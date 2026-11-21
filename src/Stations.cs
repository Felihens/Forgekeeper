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

        // what each station is doing, written to the log when it changes (so the log says why one is left alone)
        private static readonly Dictionary<Smelter, string> State = new Dictionary<Smelter, string>();
        private static void Say(Smelter s, string name, string state)
        {
            string was;
            if (State.TryGetValue(s, out was) && was == state) return;
            State[s] = state;
            if (State.Count > 300) State.Clear();
            Plugin.Debug("[forge] " + name + " at " + s.transform.position.ToString("0") + ": " + state);
        }

        private static void Look(Player p, Smelter s)
        {
            if (s.GetComponent<Piece>() == null) return;                // built ones only
            string name = Utils.GetPrefabName(s.gameObject);
            ZNetView nv = s.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) return;
            if (!nv.IsOwner()) { Say(s, name, "left alone: another player's game runs it now (owner " + nv.GetZDO().GetOwner() + ")"); return; }
            if (InList(Plugin.SkipStations.Value, name)) { Say(s, name, "left alone: in SkipStations"); return; }
            if (!Plugin.FuelOnly.Value && (s.m_conversion == null || s.m_conversion.Count == 0)) { Say(s, name, "left alone: it only burns fuel (FuelOnlyStations is off)"); return; }
            int near, others, locked;
            List<Container> chests = Near(p, s.transform.position, out near, out others, out locked);
            if (chests.Count == 0)
            {
                Say(s, name, "no chest to use within " + Plugin.ChestRange.Value.ToString("0") + " m (" + near + " chests near: " + others + " run by another player's game, " + locked + " private or warded)");
                return;
            }
            Say(s, name, "looked after, with " + chests.Count + " chest(s)" + (others + locked > 0 ? " (" + others + " run by another game, " + locked + " private or warded, left out)" : ""));
            if (Plugin.Output.Value) StoreOutput(s, nv, chests, name);
            if (Plugin.Fuel.Value) AddFuel(s, nv, chests, name);
            if (Plugin.Input.Value) AddInput(s, nv, chests, name);
        }

        // the chests by this station your game runs and you may open, nearest first
        private static List<Container> Near(Player p, Vector3 at, out int near, out int others, out int locked)
        {
            near = others = locked = 0;
            float r = Plugin.ChestRange.Value;
            long me = p.GetPlayerID();
            List<Container> list = new List<Container>();
            foreach (Container c in Chests)
            {
                if (c == null || (c.transform.position - at).sqrMagnitude > r * r) continue;
                // built chests only: never a player, a grave, a cart, a ship or an animal's saddlebags
                if (c.m_wagon != null || c.GetComponentInParent<Ship>() != null || c.GetComponent<Piece>() == null) continue;
                if (c.GetComponent<TombStone>() != null || c.GetComponentInParent<Character>() != null) continue;
                near++;
                ZNetView nv = c.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid() || !nv.IsOwner()) { others++; continue; }   // opened or run by another player's game
                if ((AccessM != null && !(bool)AccessM.Invoke(c, new object[] { me })) || !PrivateArea.CheckAccess(c.transform.position, 0f, false, false)) { locked++; continue; }
                if (c.GetInventory() == null) continue;
                list.Add(c);
            }
            list.Sort((a, b) => (a.transform.position - at).sqrMagnitude.CompareTo((b.transform.position - at).sqrMagnitude));
            return list;
        }

        // ------------------------------------------------------------ checking each step (user, 2026-11-17)

        // Every step is checked right after it is done: the chests lost what was taken, the station got what was put
        // in, the chests got what was stored. A step that didn't add up is written as a PROBLEM (always, even with
        // DetailedLog off), and what the station didn't take goes back into the chests.
        private static void Check(string name, bool ok, string what)
        {
            if (ok) Plugin.Debug("[forge][check] OK  " + name + ": " + what);
            else Plugin.Log.LogWarning("[forge][check] PROBLEM  " + name + ": " + what);
        }

        // ------------------------------------------------------------ in

        private static void AddFuel(Smelter s, ZNetView nv, List<Container> chests, string name)
        {
            if (s.m_fuelItem == null || s.m_maxFuel <= 0) return;
            float fuel = (float)FuelM.Invoke(s, null);
            int room = Mathf.FloorToInt(s.m_maxFuel - fuel);
            if (room <= 0) return;
            string item = s.m_fuelItem.gameObject.name;
            int got = Take(chests, item, room, name);
            if (got <= 0) return;
            for (int i = 0; i < got; i++) nv.InvokeRPC("RPC_AddFuel", new object[0]);
            float now = (float)FuelM.Invoke(s, null);
            int went = Mathf.RoundToInt(now - fuel);
            Check(name, went == got, got + " " + item + " as fuel: fuel " + fuel.ToString("0.#") + " -> " + now.ToString("0.#") + " (max " + s.m_maxFuel + ")");
            if (went < got) GiveBack(chests, s.m_fuelItem.gameObject, got - Mathf.Max(0, went), name);
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
                int before = (int)QueueSizeM.Invoke(s, null);
                int got = Take(chests, item, room, name);
                if (got <= 0) continue;
                for (int i = 0; i < got; i++) nv.InvokeRPC("RPC_AddOre", new object[] { item, false });
                int after = (int)QueueSizeM.Invoke(s, null);
                Check(name, after - before == got, got + " " + item + " put in: queue " + before + " -> " + after + " (holds " + s.m_maxOre + ")");
                if (after - before < got) GiveBack(chests, c.m_from.gameObject, got - Mathf.Max(0, after - before), name);
                room -= got;
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

        // what the station didn't take goes back into the chests
        private static void GiveBack(List<Container> chests, GameObject prefab, int n, string name)
        {
            int back = Store(chests, prefab, n, name);
            Check(name, back == n, "gave back " + back + " of " + n + " " + prefab.name + " the station didn't take");
        }

        // takes up to max of an item out of the chests (nearest first), leaving KeepInChests; how many it took
        private static int Take(List<Container> chests, string prefab, int max, string name)
        {
            int had = Count(chests, prefab);
            int keep = Keep(prefab);
            if (keep > 0) max = Mathf.Min(max, had - keep);
            if (max <= 0 || had <= 0) return 0;   // none in the chests: nothing to take (and nothing to log)
            int taken = Remove(chests, prefab, max);
            int left = Count(chests, prefab);
            Check(name, had - left == taken, "took " + taken + " " + prefab + " from the chests: " + had + " -> " + left + (keep > 0 ? " (keeps " + keep + ")" : ""));
            return had - left == taken ? taken : Mathf.Max(0, had - left);
        }

        private static int Remove(List<Container> chests, string prefab, int max)
        {
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
                int stored = Store(chests, conv.m_to.gameObject, amount, name);
                if (stored <= 0) return;
                if (stored >= amount) { z.Set(ZDOVars.s_spawnOre, ""); z.Set(ZDOVars.s_spawnAmount, 0); }
                else z.Set(ZDOVars.s_spawnAmount, amount - stored);
                int stays = z.GetInt(ZDOVars.s_spawnAmount, 0);
                Check(name, stays == amount - stored, "its stack of " + amount + " " + conv.m_to.gameObject.name + ": " + stored + " into the chests, " + stays + " left in the station");
                return;
            }
            // else it drops each piece at its output: pick those up into the chests
            if (s.m_outputPoint == null || s.m_conversion == null) return;
            HashSet<string> made = new HashSet<string>();
            foreach (Smelter.ItemConversion c in s.m_conversion) if (c != null && c.m_to != null) made.Add(c.m_to.gameObject.name);
            // the station throws each piece out of its output and it rolls (the kiln's coal most of all), so look
            // well around the output, not only right at it (2 m missed rolled coal, 2026-11-18)
            HashSet<ItemDrop> seen = new HashSet<ItemDrop>();
            foreach (Collider col in Physics.OverlapSphere(s.m_outputPoint.position, PickupRadius, LayerMask.GetMask("item"), QueryTriggerInteraction.Collide))
            {
                ItemDrop d = col != null ? col.GetComponentInParent<ItemDrop>() : null;
                if (d == null || d.m_itemData == null || !seen.Add(d)) continue;
                string item = Utils.GetPrefabName(d.gameObject);
                if (!made.Contains(item)) continue;
                ZNetView dv = d.GetComponent<ZNetView>();
                if (dv == null || !dv.IsValid()) continue;
                if (!dv.IsOwner()) { Plugin.Debug("[forge] " + name + ": " + d.m_itemData.m_stack + " " + item + " on the ground left alone, another player's game runs it"); continue; }
                GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(item) : null;
                int stack = d.m_itemData.m_stack;
                if (prefab == null || stack <= 0) continue;
                int stored = Store(chests, prefab, stack, name);   // as much as the chests take, spread over them
                if (stored <= 0) { Say(s, name, "chests full: " + stack + " " + item + " stays on the ground"); continue; }
                if (stored >= stack)
                {
                    ZNetScene.instance.Destroy(d.gameObject);
                    Check(name, !dv.IsValid() || dv.GetZDO() == null, "picked up " + stack + " " + item + " from the ground into the chests" + (dv.IsValid() && dv.GetZDO() != null ? ", but it is still on the ground" : ""));
                }
                else
                {
                    d.SetStack(stack - stored);   // the rest stays on the ground, nothing lost or doubled
                    Check(name, d.m_itemData.m_stack == stack - stored, "picked up " + stored + " of " + stack + " " + item + " from the ground (chests full), " + d.m_itemData.m_stack + " left there");
                }
            }
        }

        private const float PickupRadius = 6f;

        // puts amount of an item into the chests: first those that already have some, then any; how many went in
        private static int Store(List<Container> chests, GameObject prefab, int amount, string name)
        {
            ItemDrop d = prefab.GetComponent<ItemDrop>();
            if (d == null) return 0;
            int had = Count(chests, prefab.name);
            int stored = Put(chests, prefab, d, amount);
            int now = Count(chests, prefab.name);
            Check(name, now - had == stored, "stored " + stored + " of " + amount + " " + prefab.name + " in the chests: " + had + " -> " + now);
            return now - had == stored ? stored : Mathf.Max(0, now - had);
        }

        private static int Put(List<Container> chests, GameObject prefab, ItemDrop d, int amount)
        {
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
