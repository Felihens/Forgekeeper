using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Forgekeeper
{
    // Every few seconds, for each station near you that YOUR game runs (the game lets one player's game run each
    // station and each chest; only that game may change them, else items get lost or doubled):
    //  - its finished goods go into its chests (from the station itself when it keeps them in a stack, else the
    //    pieces it dropped at its output);
    //  - fuel from the chests tops it up;
    //  - what it turns into something else fills its queue.
    // Items go into the station the game's own way (the same calls as a player putting them in), so the station
    // really gets them, and only from chests your game runs, that you may open (private chests, wards).
    [HarmonyPatch]
    internal static class Stations
    {
        private static readonly List<Smelter> All = new List<Smelter>();
        private static readonly List<Container> Chests = new List<Container>();
        private static float _next;

        private static readonly MethodInfo QueueSizeM = AccessTools.Method(typeof(Smelter), "GetQueueSize");
        private static readonly MethodInfo FuelM = AccessTools.Method(typeof(Smelter), "GetFuel");
        private static readonly MethodInfo AccessM = AccessTools.Method(typeof(Container), "CheckAccess", new[] { typeof(long) });
        private static readonly MethodInfo ConversionM = AccessTools.Method(typeof(Smelter), "GetItemConversion", new[] { typeof(string) });

        [HarmonyPatch(typeof(Smelter), "Awake"), HarmonyPostfix]
        private static void SeenStation(Smelter __instance) { All.Add(__instance); }

        [HarmonyPatch(typeof(Container), "Awake"), HarmonyPostfix]
        private static void SeenChest(Container __instance) { Chests.Add(__instance); }

        internal static void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + Plugin.Interval.Value;
            Player p = Player.m_localPlayer;
            if (p == null || p.IsDead() || QueueSizeM == null || FuelM == null) return;
            Chests.RemoveAll(c => c == null);
            float range = Plugin.StationRange.Value;
            Vector3 me = p.transform.position;
            for (int i = All.Count - 1; i >= 0; i--)
            {
                Smelter s = All[i];
                if (s == null) { All.RemoveAt(i); continue; }
                if ((s.transform.position - me).sqrMagnitude > range * range) continue;
                try { Look(p, s); }
                catch (System.Exception e) { Plugin.Log.LogWarning("[forge] " + s.name + ": " + e.Message); }
            }
        }

        private static void Look(Player p, Smelter s)
        {
            ZNetView nv = s.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid() || !nv.IsOwner()) return;   // another player's game runs it
            if (s.GetComponent<Piece>() == null) return;                // built ones only
            string name = Utils.GetPrefabName(s.gameObject);
            if (InList(Plugin.SkipStations.Value, name)) return;
            List<Container> chests = Near(p, s.transform.position);
            if (chests.Count == 0) return;
            if (Plugin.Output.Value) StoreOutput(s, nv, chests, name);
            if (Plugin.Fuel.Value) AddFuel(s, nv, chests, name);
            if (Plugin.Input.Value) AddInput(s, nv, chests, name);
        }

        // the chests by this station your game runs and you may open, nearest first
        private static List<Container> Near(Player p, Vector3 at)
        {
            float r = Plugin.ChestRange.Value;
            long me = p.GetPlayerID();
            List<Container> list = new List<Container>();
            foreach (Container c in Chests)
            {
                if (c == null || (c.transform.position - at).sqrMagnitude > r * r) continue;
                // built chests only: never a player, a grave, a cart, a ship or an animal's saddlebags
                if (c.m_wagon != null || c.GetComponentInParent<Ship>() != null || c.GetComponent<Piece>() == null) continue;
                if (c.GetComponent<TombStone>() != null || c.GetComponentInParent<Character>() != null) continue;
                ZNetView nv = c.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid() || !nv.IsOwner()) continue;   // opened or run by another player's game
                if ((AccessM != null && !(bool)AccessM.Invoke(c, new object[] { me })) || !PrivateArea.CheckAccess(c.transform.position, 0f, false, false)) continue;
                if (c.GetInventory() == null) continue;
                list.Add(c);
            }
            list.Sort((a, b) => (a.transform.position - at).sqrMagnitude.CompareTo((b.transform.position - at).sqrMagnitude));
            return list;
        }

        // ------------------------------------------------------------ in

        private static void AddFuel(Smelter s, ZNetView nv, List<Container> chests, string name)
        {
            if (s.m_fuelItem == null || s.m_maxFuel <= 0) return;
            float fuel = (float)FuelM.Invoke(s, null);
            int room = Mathf.FloorToInt(s.m_maxFuel - fuel);
            if (room <= 0) return;
            string item = s.m_fuelItem.gameObject.name;
            int got = Take(chests, item, room);
            for (int i = 0; i < got; i++) nv.InvokeRPC("RPC_AddFuel", new object[0]);
            if (got > 0) Plugin.Debug("[forge] " + name + ": " + got + " " + item + " as fuel");
        }

        private static void AddInput(Smelter s, ZNetView nv, List<Container> chests, string name)
        {
            if (s.m_conversion == null || s.m_maxOre <= 0) return;
            int room = s.m_maxOre - (int)QueueSizeM.Invoke(s, null);
            foreach (Smelter.ItemConversion c in s.m_conversion)
            {
                if (room <= 0) return;
                if (c == null || c.m_from == null) continue;
                string item = c.m_from.gameObject.name;
                if (InList(Plugin.NeverFeed.Value, item)) continue;
                int got = Take(chests, item, room);
                for (int i = 0; i < got; i++) nv.InvokeRPC("RPC_AddOre", new object[] { item, false });
                room -= got;
                if (got > 0) Plugin.Debug("[forge] " + name + ": " + got + " " + item + " put in");
            }
        }

        // the least of an item that stays in the chests (KeepInChests)
        private static int Keep(string prefab)
        {
            string list = Plugin.KeepInChests.Value;
            if (string.IsNullOrEmpty(list)) return 0;
            foreach (string e in list.Split(','))
            {
                string[] kv = e.Split(':');
                int n;
                if (kv.Length == 2 && string.Equals(kv[0].Trim(), prefab, System.StringComparison.OrdinalIgnoreCase) && int.TryParse(kv[1].Trim(), out n)) return Mathf.Max(0, n);
            }
            return 0;
        }

        private static int Count(List<Container> chests, string prefab)
        {
            int n = 0;
            foreach (Container c in chests)
                foreach (ItemDrop.ItemData it in c.GetInventory().GetAllItems())
                    if (it != null && it.m_dropPrefab != null && it.m_dropPrefab.name == prefab) n += it.m_stack;
            return n;
        }

        // takes up to max of an item out of the chests (nearest first), leaving KeepInChests; how many it took
        private static int Take(List<Container> chests, string prefab, int max)
        {
            int keep = Keep(prefab);
            if (keep > 0) max = Mathf.Min(max, Count(chests, prefab) - keep);
            if (max <= 0) return 0;
            int taken = 0;
            foreach (Container c in chests)
            {
                if (taken >= max) break;
                Inventory inv = c.GetInventory();
                foreach (ItemDrop.ItemData it in new List<ItemDrop.ItemData>(inv.GetAllItems()))
                {
                    if (taken >= max) break;
                    if (it == null || it.m_dropPrefab == null || it.m_dropPrefab.name != prefab || it.m_stack <= 0) continue;
                    int n = Mathf.Min(it.m_stack, max - taken);
                    if (inv.RemoveItem(it, n)) taken += n;
                }
            }
            return taken;
        }

        // ------------------------------------------------------------ out

        private static void StoreOutput(Smelter s, ZNetView nv, List<Container> chests, string name)
        {
            if (s.m_spawnStack)
            {
                // the station keeps what it made in a stack until it's emptied: straight from there into the chests
                ZDO z = nv.GetZDO();
                string ore = z.GetString(ZDOVars.s_spawnOre, "");
                int amount = z.GetInt(ZDOVars.s_spawnAmount, 0);
                if (amount <= 0 || ore == "" || ConversionM == null) return;
                Smelter.ItemConversion conv = ConversionM.Invoke(s, new object[] { ore }) as Smelter.ItemConversion;
                if (conv == null || conv.m_to == null) return;
                int stored = Store(chests, conv.m_to.gameObject, amount);
                if (stored <= 0) return;
                if (stored >= amount) { z.Set(ZDOVars.s_spawnOre, ""); z.Set(ZDOVars.s_spawnAmount, 0); }
                else z.Set(ZDOVars.s_spawnAmount, amount - stored);
                Plugin.Debug("[forge] " + name + ": " + stored + " " + conv.m_to.gameObject.name + " into the chests");
                return;
            }
            // else it drops each piece at its output: pick those up into the chests
            if (s.m_outputPoint == null || s.m_conversion == null) return;
            HashSet<string> made = new HashSet<string>();
            foreach (Smelter.ItemConversion c in s.m_conversion) if (c != null && c.m_to != null) made.Add(c.m_to.gameObject.name);
            foreach (Collider col in Physics.OverlapSphere(s.m_outputPoint.position, 2f, LayerMask.GetMask("item"), QueryTriggerInteraction.Collide))
            {
                ItemDrop d = col != null ? col.GetComponentInParent<ItemDrop>() : null;
                if (d == null || d.m_itemData == null) continue;
                ZNetView dv = d.GetComponent<ZNetView>();
                if (dv == null || !dv.IsValid() || !dv.IsOwner()) continue;
                string item = Utils.GetPrefabName(d.gameObject);
                if (!made.Contains(item)) continue;
                GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(item) : null;
                int stack = d.m_itemData.m_stack;
                if (prefab == null || stack <= 0 || !Fits(chests, prefab, stack)) continue;   // only whole pieces, so nothing is lost
                if (Store(chests, prefab, stack) != stack) continue;
                ZNetScene.instance.Destroy(d.gameObject);
                Plugin.Debug("[forge] " + name + ": picked up " + stack + " " + item + " into the chests");
            }
        }

        private static bool Fits(List<Container> chests, GameObject prefab, int amount)
        {
            foreach (Container c in chests) if (c.GetInventory().CanAddItem(prefab, amount)) return true;
            return false;
        }

        // puts amount of an item into the chests: first those that already have some, then any; how many went in
        private static int Store(List<Container> chests, GameObject prefab, int amount)
        {
            ItemDrop d = prefab.GetComponent<ItemDrop>();
            if (d == null) return 0;
            string shared = d.m_itemData.m_shared.m_name;
            int left = amount;
            for (int pass = 0; pass < 2 && left > 0; pass++)
                foreach (Container c in chests)
                {
                    if (left <= 0) break;
                    Inventory inv = c.GetInventory();
                    if (pass == 0 && inv.CountItems(shared, -1, true) == 0) continue;
                    int n = left;
                    while (n > 0 && !inv.CanAddItem(prefab, n)) n--;
                    if (n > 0 && inv.AddItem(prefab, n)) left -= n;
                }
            return amount - left;
        }

        // Once a game: every station the game has, what it burns and what it turns into what, so it's known what
        // later stations take (written to the log).
        private static bool _listed;
        internal static void ListOnce()
        {
            if (_listed || ZNetScene.instance == null || Player.m_localPlayer == null) return;
            _listed = true;
            foreach (GameObject g in ZNetScene.instance.m_prefabs)
            {
                Smelter s = g != null ? g.GetComponent<Smelter>() : null;
                if (s == null) continue;
                List<string> conv = new List<string>();
                if (s.m_conversion != null)
                    foreach (Smelter.ItemConversion c in s.m_conversion)
                        if (c != null && c.m_from != null && c.m_to != null) conv.Add(c.m_from.gameObject.name + ">" + c.m_to.gameObject.name);
                Plugin.Log.LogInfo("[forge] station " + g.name + ": fuel " + (s.m_fuelItem != null ? s.m_fuelItem.gameObject.name + " (max " + s.m_maxFuel + ", " + s.m_fuelPerProduct + " per item)" : "none")
                    + ", holds " + s.m_maxOre + ", " + s.m_secPerProduct + " s each, " + (s.m_spawnStack ? "keeps a stack" : "drops each") + ": " + string.Join(", ", conv.ToArray()));
            }
        }

        private static bool InList(string list, string name)
        {
            if (string.IsNullOrEmpty(list)) return false;
            foreach (string e in list.Split(','))
                if (string.Equals(e.Trim(), name, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
