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
        public static bln SaveLockedSlotsCount = true;
        public static bln ScrapTimeAndSalvageOperations = true;

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
                            f.SetValue(null, Convert.ChangeType(v, f.FieldType));
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Mod {nameof(AirDropIsNeverEmpty)}: {ex.Message}");
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
