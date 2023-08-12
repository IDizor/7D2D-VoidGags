using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public static string ModFolder;
        public static string FeaturesFolder;
        public static bool IsServer;
        private static List<string> AdditionalXmlPatches = new List<string>();
        private static List<Action> OnGameLoadedActions = new List<Action>();
        
        /// <summary>
        /// Mod initialization.
        /// </summary>
        /// <param name="_modInstance"></param>
        public void InitMod(Mod _modInstance)
        {
            Debug.Log("Loading mod: " + GetType().ToString());
            ModFolder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(VoidGags)).Location);
            FeaturesFolder = Path.Combine(ModFolder, "Features");

            if (!Directory.Exists(FeaturesFolder))
            {
                LogModException("\"Features\" folder not found. Please reinstall the mod.");
                return;
            }

            var harmony = new Harmony(GetType().ToString());

            CheckUndeadLegacy();
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (Settings.AirDropIsNeverEmpty) ApplyPatches_AirDropIsNeverEmpty(harmony);
            if (Settings.CraftingQueueRightClickToMove) ApplyPatches_CraftingQueueMove(harmony);
            if (Settings.ExperienceRewardByMaxHP) ApplyPatches_ExperienceByMaxHP(harmony);
            if (Settings.HelmetLightByDefault) ApplyPatches_HelmetLightFirst(harmony);
            if (Settings.PickupDamagedItems) ApplyPatches_PickupDamagedBlock(harmony);
            if (Settings.MouseWheelClickFastRepair) ApplyPatches_RepairByWheelClick(harmony);
            if (Settings.RepairHasTopPriority) ApplyPatches_RepairPriority(harmony);
            if (Settings.LockedSlotsSystem) ApplyPatches_LockedSlotsSystem(harmony);
            if (Settings.ScrapTimeAndSalvageOperations) ApplyPatches_ScrapTime(harmony);
            if (Settings.PreventConsoleErrorSpam) ApplyPatches_PreventConsoleErrorSpam(harmony);
            if (Settings.ArrowsBoltsDistraction) ApplyPatches_ArrowsBoltsDistraction(harmony);
            if (Settings.RocksGrenadesDistraction) ApplyPatches_RocksGrenadesDistraction(harmony);
            if (Settings.ExplosionAttractionFix) ApplyPatches_ExplosionAttractionFix(harmony);
            //if (Settings.ScrapDrinksToEmptyJars) ApplyPatches_ScrapDrinksToEmptyJars(harmony); // Obsolete (used in A20)
            if (Settings.DigThroughTheGrass) ApplyPatches_DigThroughTheGrass(harmony);
            if (Settings.LessFogWhenFlying) ApplyPatches_LessFogWhenFlying(harmony);
            if (Settings.SocialZombies) ApplyPatches_SocialZombies(harmony);
            if (Settings.PreventDestroyOnClose) ApplyPatches_PreventDestroyOnClose(harmony);
            if (Settings.MainLootTierBonus) ApplyPatches_MainLootTierBonus(harmony);
            if (Settings.PiercingShots) ApplyPatches_PiercingShots(harmony);
            if (Settings.HighlightCompatibleMods) ApplyPatches_HighlightCompatibleMods(harmony);
            /* if () is not needed for this */ ApplyPatches_MasterWorkChance(harmony);
            if (Settings.StealthOnLadders) ApplyPatches_StealthOnLadders(harmony);
            if (Settings.PreventPillaring) ApplyPatches_PreventPillaring(harmony);
            if (Settings.UnrevealedTradeRoutesOnly) ApplyPatches_UnrevealedTradeRoutesOnly(harmony);
            if (Settings.NoScreamersFromOutside) ApplyPatches_NoScreamersFromOutside(harmony);

            OnGameLoadedActions.Add(() => {
                IsServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
            });
        }

        /// <summary>
        /// Adds the specified XML patch for further applying.
        /// </summary>
        /// <param name="patchName">Folder name with XML files.</param>
        public void UseXmlPatches(string patchName)
        {
            if (!Directory.Exists($"{FeaturesFolder}\\{patchName}"))
            {
                LogModException($"Missing XML patch folder '{patchName}'.");
            }
            AdditionalXmlPatches.Add(patchName);
        }

        /// <summary>
        /// Method that called once the game is loaded.
        /// </summary>
        [HarmonyPatch(typeof(EntityPlayerLocal))]
        [HarmonyPatch("PostInit")]
        public class EntityPlayerLocal_PostInit
        {
            public static void Postfix()
            {
                foreach (var action in OnGameLoadedActions)
                {
                    action();
                }
            }
        }

        /// <summary>
        /// Applies feature-specific additional XML patches.
        /// </summary>
        [HarmonyPatch(typeof(WorldStaticData))]
        [HarmonyPatch("cacheSingleXml")]
        public class WorldStaticData_cacheSingleXml
        {
            public static void Prefix(object _loadInfo, XmlFile _origXml)
            {
                if (AdditionalXmlPatches.Count == 0)
                {
                    return;
                }

                var cachingXmlName = (string)AccessTools.Field(_loadInfo.GetType(), "XmlName").GetValue(_loadInfo);

                if (!string.IsNullOrEmpty(cachingXmlName))
                {
                    foreach (var patchName in AdditionalXmlPatches)
                    {
                        var configPath = $"{FeaturesFolder}\\{patchName}\\{cachingXmlName.Replace('/','\\')}.xml";
                        var configDir = $"{FeaturesFolder}\\{patchName}";
                        if (File.Exists(configPath))
                        {
                            try
                            {
                                var patchXml = new XmlFile(configDir, cachingXmlName, _loadAsync: false, _throwExc: true);
                                XmlPatcher.PatchXml(_origXml, patchXml, patchName);
                            }
                            catch (Exception ex)
                            {
                                LogModException($"Failed to apply XML patch '{patchName}' for the file '{cachingXmlName}.xml'. {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        internal static void LogPatchApplied(string patchName)
        {
            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {patchName}");
        }

        internal static void LogModException(string message)
        {
            Debug.LogException(new Exception($"Mod {nameof(VoidGags)}: {message}"));
        }

        internal static void LogModWarning(string message)
        {
            Debug.LogWarning($"Mod {nameof(VoidGags)}: {message}");
        }

        internal static void LogModInfo(string message)
        {
            Debug.Log($"Mod {nameof(VoidGags)}: {message}");
        }
    }
}
