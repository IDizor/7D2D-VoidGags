using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using bln = System.Boolean;
using flt = System.Single;

namespace VoidGags
{
    internal static class Settings
    {
        public static bln AirDropIsNeverEmpty = true;
        public static bln CraftingQueueRightClickToMove = true;
        public static bln ExperienceRewardByMaxHP = true;
        public static flt ExperienceRewardByMaxHP_Multiplier = 1.0f;
        public static bln HelmetLightByDefault = true;
        public static bln PickupDamagedItems = true;
        public static int PickupDamagedItems_Percentage = 80;
        public static bln MouseWheelClickFastRepair = true;
        public static bln RepairHasTopPriority = true;
        public static bln LockedSlotsSystem = true;
        public static bln LockedSlotsSystem_AutoSpreadButton = true;
        public static flt AutoSpreadLootRadius = 10f;
        public static bln ScrapTimeAndSalvageOperations = true;
        public static bln PreventConsoleErrorSpam = false;
        public static bln ArrowsBoltsDistraction = true;
        public static bln RocksGrenadesDistraction = true;
        public static bln ExplosionAttractionFix = true;
        public static bln ScrapDrinksToEmptyJars = true;
        public static bln DigThroughTheGrass = true;
        public static bln LessFogWhenFlying = true;
        public static bln SocialZombies = false;
        public static bln PreventDestroyOnClose = true;
        public static int PreventDestroyOnClose_KeyCode = (int)KeyCode.LeftShift;
        public static bln MainLootTierBonus = false; // hidden feature, disabled by default
        public static bln PiercingShots = true;
        public static bln HighlightCompatibleMods = true;
        public static flt MasterWorkChance = 10f;
        public static bln StealthOnLadders = true;
        public static bln PreventPillaring = true;
        public static bln UnrevealedTradeRoutesOnly = true;
        public static bln NoScreamersFromOutside = true;

        static Settings()
        {
            // load settings from json file
            var path = Path.ChangeExtension(Assembly.GetAssembly(typeof(VoidGags)).Location, "config");
            if (File.Exists(path))
            {
                try
                {
                    var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path));
                    typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.Public).ToList().ForEach(f =>
                    {
                        if (settings.TryGetValue(f.Name, out object v))
                        {
                            f.SetValue(null, Convert.ChangeType(v, f.FieldType));
                        }
                    });
                }
                catch
                {
                    VoidGags.LogModException("Failed to parse config file.");
                }
            }
            else
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(typeof(Settings)
                    .GetFields(BindingFlags.Static | BindingFlags.Public)
                    .ToDictionary(f => f.Name, f => f.GetValue(null)), Formatting.Indented));
            }
        }
    }
}
