using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using static VoidGags.VoidGags;
using static World;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public static Mod ModInstance;
        public static string ModFolder;
        public static string FeaturesFolderPath;
        public static bool IsServer;
        private static List<string> AdditionalXmlPatches = new();
        private static List<Action> OnGameLoadedActions = new();
        
        /// <summary>
        /// Mod initialization.
        /// </summary>
        /// <param name="_modInstance"></param>
        public void InitMod(Mod _modInstance)
        {
            ModInstance = _modInstance;
            Debug.Log("Loading mod: " + GetType().ToString());
            ModFolder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(VoidGags)).Location);
            FeaturesFolderPath = Path.Combine(ModFolder, "Features");

            if (!Directory.Exists(FeaturesFolderPath))
            {
                LogModException("\"Features\" folder not found. Please reinstall the mod.");
                return;
            }

            var harmony = new Harmony(GetType().ToString());

            CheckUndeadLegacy();
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (Settings.SkipNewsScreen) ApplyPatches_SkipNewsScreen(harmony);
            if (Settings.CraftingQueueRightClickToMove) ApplyPatches_CraftingQueueMove(harmony);
            if (Settings.ExperienceRewardByMaxHP) ApplyPatches_ExperienceByMaxHP(harmony);
            if (Settings.HelmetLightByDefault) ApplyPatches_HelmetLightFirst(harmony);
            if (Settings.PickupDamagedItems) ApplyPatches_PickupDamagedBlock(harmony);
            if (Settings.FastRepair) ApplyPatches_FastRepair(harmony);
            if (Settings.RepairingHasTopPriority) ApplyPatches_RepairingPriority(harmony);
            if (Settings.LockedSlotsSystem) ApplyPatches_LockedSlotsSystem(harmony);
            if (Settings.ScrapTimeAndSalvageOperations) ApplyPatches_ScrapTime(harmony);
            if (Settings.PreventConsoleErrorSpam) ApplyPatches_PreventConsoleErrorSpam(harmony);
            if (Settings.ArrowsBoltsDistraction) ApplyPatches_ArrowsBoltsDistraction(harmony);
            if (Settings.GrenadesDistraction) ApplyPatches_GrenadesDistraction(harmony);
            if (Settings.ExplosionAttractionFix) ApplyPatches_ExplosionAttractionFix(harmony);
            if (Settings.DigThroughTheGrass) ApplyPatches_DigThroughTheGrass(harmony);
            if (Settings.LessFogWhenFlying) ApplyPatches_LessFogWhenFlying(harmony);
            if (Settings.SocialZombies) ApplyPatches_SocialZombies(harmony);
            if (Settings.PreventDestroyOnClose) ApplyPatches_PreventDestroyOnClose(harmony);
            if (Settings.MainLootTierBonus) ApplyPatches_MainLootTierBonus(harmony);
            if (Settings.PiercingShots) ApplyPatches_PiercingShots(harmony);
            if (Settings.HighlightCompatibleMods) ApplyPatches_HighlightCompatibleMods(harmony);
            /* if () is not needed for this */ ApplyPatches_MasterWorkChance(harmony);
            if (Settings.StealthOnLadders) ApplyPatches_StealthOnLadders(harmony);
            if (Settings.ExhaustingLadders) ApplyPatches_ExhaustingLadders(harmony);
            if (Settings.PreventPillaring) ApplyPatches_PreventPillaring(harmony);
            if (Settings.UnrevealedTradeRoutesOnly) ApplyPatches_UnrevealedTradeRoutesOnly(harmony);
            if (Settings.NoScreamersFromOutside) ApplyPatches_NoScreamersFromOutside(harmony);
            if (Settings.FoodWaterBars) ApplyPatches_FoodWaterBars(harmony);
            if (Settings.GeneratorSwitchFirst) ApplyPatches_GeneratorSwitchFirst(harmony);
            if (Settings.ArrowsBoltsAutoPickUp) ApplyPatches_ArrowsBoltsAutoPickUp(harmony);

            OnGameLoadedActions.Add(() => {
                IsServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
            });
        }

        /// <summary>
        /// Adds the specified XML patches for further applying.
        /// </summary>
        public void UseXmlPatches(string patchName)
        {
            if (!Directory.Exists($"{FeaturesFolderPath}\\{patchName}\\Config"))
            {
                LogModException($"Unable to apply XML patches for '{patchName}'. Incorrect folders structure. Make sure the 'Features' folder is up to date.");
            }
            AdditionalXmlPatches.Add(patchName);
        }

        /// <summary>
        /// Method that called once the game is loaded.
        /// </summary>
        [HarmonyPatch(typeof(EntityPlayerLocal))]
        [HarmonyPatch(nameof(EntityPlayerLocal.PostInit))]
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
        /// Adds features as separate mods to apply their XML patches.
        /// </summary>
        [HarmonyPatch(typeof(ModManager))]
        [HarmonyPatch(nameof(ModManager.GetLoadedMods))]
        public class ModManager_GetLoadedMods
        {
            public static void Postfix(ref List<Mod> __result)
            {
                var caller = Helper.GetCallerMethod();
                if (caller.DeclaringType.Name.Contains("GearsSettingsManager")) // "Gears" mod compatibility fix
                {
                    return;
                }

                if (AdditionalXmlPatches.Count > 0)
                {
                    for (int i = 0; i < __result.Count; i++)
                    {
                        var mod = __result[i];
                        if (mod.Path.EndsWith(nameof(VoidGags)))
                        {
                            var updatedModsList = new List<Mod>(__result);
                            for (int j = 0; j < AdditionalXmlPatches.Count; j++)
                            {
                                var featureName = AdditionalXmlPatches[j];
                                var feature = new Mod
                                {
                                    Name = mod.Name + "." + featureName,
                                    Author = mod.Author,
                                    Version = mod.Version,
                                    VersionString = mod.VersionString,
                                    LoadState = mod.LoadState,
                                    AntiCheatCompatible = mod.AntiCheatCompatible,
                                    SkipLoadingWithAntiCheat = mod.SkipLoadingWithAntiCheat,
                                    DisplayName = mod.DisplayName + "." + featureName,
                                    FolderName = mod.FolderName + $"/Features/{featureName}",
                                    Path = $"{FeaturesFolderPath}/{featureName}",
                                };
                                updatedModsList.Insert(i + j + 1, feature);
                            }
                            __result = updatedModsList;
                            return;
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

        internal static void LogModError(string message)
        {
            Debug.LogError($"Mod {nameof(VoidGags)}: {message}");
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
