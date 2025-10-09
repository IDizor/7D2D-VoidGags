using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using bln = System.Boolean;
using fla = System.Single[];
using flt = System.Single;
using str = System.String;

namespace VoidGags
{
    internal static class Settings
    {
        public static bln SkipNewsScreen = false;
        public static bln CraftingQueueRightClickToMove = true;
        public static bln ExperienceRewardByMaxHP = true;
        public static flt ExperienceRewardByMaxHP_Multiplier = 1.0f;
        public static bln HelmetLightByDefault = true;
        public static bln PickupDamagedItems = true;
        public static int PickupDamagedItems_Percentage = 80;
        public static bln FastRepair = true;
        public static str FastRepair_HotKey = nameof(KeyCode.Mouse2);
        public static bln RepairingHasTopPriority = true;
        public static bln HighlightLockedSlots = true;
        public static bln AutoSpreadLoot = true;
        public static flt AutoSpreadLoot_Radius = 10f;
        public static bln ScrapTimeAndSalvageOperations = true;
        public static bln PreventConsoleErrorSpam = false;
        public static bln ArrowsBoltsDistraction = true;
        public static bln GrenadesDistraction = true;
        public static bln ExplosionAttractionFix = true;
        public static bln DigThroughTheGrass = true;
        public static bln LessFogWhenFlying = true;
        public static bln SocialZombies = false;
        public static bln PreventDestroyOnClose = true;
        public static bln MainLootTierBonus = false; // hidden feature, disabled by default
        public static bln PiercingShots = true;
        public static bln HighlightCompatibleMods = true;
        public static flt MasterWorkChance = 10f;
        public static int MasterWorkChance_MaxQuality = 5;
        public static bln StealthOnLadders = true;
        public static bln ExhaustingLadders = true;
        public static bln PreventPillaring = true;
        public static bln UnrevealedTradeRoutesOnly = true;
        public static bln NoScreamersFromOutside = true;
        public static bln FoodWaterBars = true;
        public static bln GeneratorSwitchFirst = true;
        public static bln ArrowsBoltsAutoPickUp = true;
        public static bln EnqueueCraftWhenNoFuel = true;
        public static int OddNightSoundsVolume = 30;
        public static bln VehicleRefuelTimer = true;
        public static bln VehicleRepairTimer = true;
        public static bln ExplosionMining = true;
        public static bln SprintJunkie = true;
        public static bln JumpControl = true;
        public static bln VisibleScriptedSleepers = false;
        public static bln ZombiesFriendlyFire = true;
        public static flt ZombiesStumbleChance = 1.0f;
        public static bln DamageModifier = false; // hidden feature, disabled by default
        public static flt DamageModifier_Gun = 1f;
        public static bln MoveOnePiece = true;
        public static bln ClickableMarkers = true;
        public static bln TradersPlayerReputation = true;
        public static bln TradersBiomeQuests = true;
        public static bln RoadRash = true;
        public static fla RoadRash_Drive = [1.5f, 1.25f, 1.25f, 0.9f, 0.7f, 0.8f, 0.7f, 0.1f, 1f]; // [Asphalt, Gravel, Wooden, Ground, Sand, Snow, Destroyed, Bushes, Other]
        public static fla RoadRash_Walk = [1.2f, 1.2f, 1.0f, 0.9f, 0.7f, 0.8f, 0.8f, 0.3f, 1f]; // [Asphalt, Gravel, Wooden, Ground, Sand, Snow, Destroyed, Bushes, Other]
        public static bln RhinoTouch = true;

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
                            if (f.FieldType.IsArray)
                                f.SetValue(null, ((JArray)v).ToObject(f.FieldType));
                            else
                                f.SetValue(null, Convert.ChangeType(v, f.FieldType));
                        }
                    });
                }
                catch (Exception ex)
                {
                    VoidGags.LogModException("Failed to parse config file: " + ex.Message);
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
