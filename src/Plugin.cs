using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Forgekeeper
{
    // Forgekeeper: production stations near you (charcoal kiln, smelter, blast furnace, eitr refinery, spinning
    // wheel, windmill... every station of the game that turns one item into another) take what they need from
    // chests next to them, and what they make goes into those chests. Client-side: each player who installs it
    // has it for the stations near them.
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "felih.Forgekeeper";
        public const string NAME = "Forgekeeper";
        public const string VERSION = "0.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled, Fuel, Input, Output, DetailedLog;
        internal static ConfigEntry<float> ChestRange, StationRange, Interval;
        internal static ConfigEntry<string> NeverFeed, SkipStations, KeepInChests;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("1 - General", "Enabled", true, "Stations near you feed themselves from nearby chests and put what they make into them.");
            StationRange = Config.Bind("1 - General", "StationRange", 40f,
                new ConfigDescription("Stations within this distance of you (metres) are looked after.", new AcceptableValueRange<float>(5f, 100f)));
            ChestRange = Config.Bind("1 - General", "ChestRange", 8f,
                new ConfigDescription("How far from a station (metres) its chests may stand.", new AcceptableValueRange<float>(1f, 30f)));
            Interval = Config.Bind("1 - General", "Interval", 5f,
                new ConfigDescription("Seconds between two checks.", new AcceptableValueRange<float>(0.5f, 30f)));
            Fuel = Config.Bind("2 - What it does", "AddFuel", true, "Fuel (coal, wood, soft tissue...) is taken from the chests and put in.");
            Input = Config.Bind("2 - What it does", "AddInput", true, "What a station turns into something else (ore, scrap, wood, sap, flax, barley...) is taken from the chests and put in.");
            Output = Config.Bind("2 - What it does", "StoreOutput", true, "What the station made (ingots, coal, eitr, linen, flour...) is collected and stored in the chests instead of piling up or lying on the ground.");
            KeepInChests = Config.Bind("2 - What it does", "KeepInChests", "Wood:100,Stone:100,Flax:20,Barley:20,Sap:0,Softtissue:20",
                "Least of an item (prefab name:amount, comma separated) that always stays in the chests around a station: it only takes what is above it. " +
                "Wood:100 = the kiln makes coal only while the chests hold more than 100 wood. Items not listed (ores, scrap, coal) may be used up to the last one.");
            NeverFeed = Config.Bind("2 - What it does", "NeverFeed", "FineWood,RoundLog,ElderBark,YggdrasilWood,Blackwood",
                "Items (prefab names, comma separated) never taken from the chests to be turned into something else: so the kiln doesn't burn your fine wood or core wood. Fuel is not affected.");
            SkipStations = Config.Bind("2 - What it does", "SkipStations", "",
                "Stations (prefab names, comma separated) left alone, e.g. charcoal_kiln,windmill.");
            DetailedLog = Config.Bind("9 - Debug", "DetailedLog", true, "Writes details to the BepInEx log.");
            new Harmony(GUID).PatchAll(typeof(Plugin).Assembly);
        }

        private void Update()
        {
            if (!Enabled.Value) return;
            Stations.ListOnce();
            Stations.Update();
        }

        internal static void Debug(string s) { if (DetailedLog.Value) Log.LogInfo(s); }
    }
}
