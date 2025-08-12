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
        public Harmony Harmony = null;

        public static Mod ModInstance;
        public static string ModFolder;
        public static string FeaturesFolderPath;
        public static bool IsServer;
        private static List<string> AdditionalXmlPatches = new();
        private static List<Action> OnGameLoadedActions = new();

        public static bool IsDedicatedServer => GameManager.IsDedicatedServer;

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

            Harmony = new Harmony(GetType().ToString());

            CheckUndeadLegacy();
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (Settings.SkipNewsScreen) SafePatch(ApplyPatches_SkipNewsScreen);
            if (Settings.CraftingQueueRightClickToMove) SafePatch(ApplyPatches_CraftingQueueMove);
            if (Settings.ExperienceRewardByMaxHP) SafePatch(ApplyPatches_ExperienceByMaxHP);
            if (Settings.HelmetLightByDefault) SafePatch(ApplyPatches_HelmetLightFirst);
            if (Settings.PickupDamagedItems) SafePatch(ApplyPatches_PickupDamagedBlock);
            if (Settings.FastRepair) SafePatch(ApplyPatches_FastRepair);
            if (Settings.RepairingHasTopPriority) SafePatch(ApplyPatches_RepairingPriority);
            if (Settings.HighlightLockedSlots) SafePatch(ApplyPatches_HighlightLockedSlots);
            if (Settings.AutoSpreadLoot) SafePatch(ApplyPatches_AutoSpreadLoot);
            if (Settings.ScrapTimeAndSalvageOperations) SafePatch(ApplyPatches_ScrapTime);
            if (Settings.PreventConsoleErrorSpam) SafePatch(ApplyPatches_PreventConsoleErrorSpam);
            if (Settings.ArrowsBoltsDistraction) SafePatch(ApplyPatches_ArrowsBoltsDistraction);
            if (Settings.GrenadesDistraction) SafePatch(ApplyPatches_GrenadesDistraction);
            if (Settings.ExplosionAttractionFix) SafePatch(ApplyPatches_ExplosionAttractionFix);
            if (Settings.DigThroughTheGrass) SafePatch(ApplyPatches_DigThroughTheGrass);
            if (Settings.LessFogWhenFlying) SafePatch(ApplyPatches_LessFogWhenFlying);
            if (Settings.SocialZombies) SafePatch(ApplyPatches_SocialZombies);
            if (Settings.PreventDestroyOnClose) SafePatch(ApplyPatches_PreventDestroyOnClose);
            if (Settings.MainLootTierBonus) SafePatch(ApplyPatches_MainLootTierBonus);
            if (Settings.PiercingShots) SafePatch(ApplyPatches_PiercingShots);
            if (Settings.HighlightCompatibleMods) SafePatch(ApplyPatches_HighlightCompatibleMods);
            /* if () is not needed for this */ SafePatch(ApplyPatches_MasterWorkChance);
            if (Settings.StealthOnLadders) SafePatch(ApplyPatches_StealthOnLadders);
            if (Settings.ExhaustingLadders) SafePatch(ApplyPatches_ExhaustingLadders);
            if (Settings.PreventPillaring) SafePatch(ApplyPatches_PreventPillaring);
            if (Settings.UnrevealedTradeRoutesOnly) SafePatch(ApplyPatches_UnrevealedTradeRoutesOnly);
            if (Settings.NoScreamersFromOutside) SafePatch(ApplyPatches_NoScreamersFromOutside);
            if (Settings.FoodWaterBars) SafePatch(ApplyPatches_FoodWaterBars);
            if (Settings.GeneratorSwitchFirst) SafePatch(ApplyPatches_GeneratorSwitchFirst);
            if (Settings.ArrowsBoltsAutoPickUp) SafePatch(ApplyPatches_ArrowsBoltsAutoPickUp);
            if (Settings.EnqueueCraftWhenNoFuel) SafePatch(ApplyPatches_EnqueueCraftWhenNoFuel);
            if (Settings.OddNightSoundsVolume != 100) SafePatch(ApplyPatches_OddNightSoundsVolume);
            if (Settings.VehicleRefuelTimer) SafePatch(ApplyPatches_VehicleRefuelTimer);
            if (Settings.VehicleRepairTimer) SafePatch(ApplyPatches_VehicleRepairTimer);
            if (Settings.ExplosionMining) SafePatch(ApplyPatches_ExplosionMining);
            if (Settings.SprintJunkie) SafePatch(ApplyPatches_SprintJunkie);
            if (Settings.JumpControl) SafePatch(ApplyPatches_JumpControl);
            if (Settings.VisibleScriptedSleepers) SafePatch(ApplyPatches_VisibleScriptedSleepers);
            if (Settings.ZombiesFriendlyFire) SafePatch(ApplyPatches_ZombiesFriendlyFire);
            if (Settings.ZombiesStumbleChance > 0f) SafePatch(ApplyPatches_ZombiesStumbleChance);
            if (Settings.DamageModifier) SafePatch(ApplyPatches_DamageModifier);

            OnGameLoadedActions.Add(() => {
                IsServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
            });
        }

        public static void SafePatch(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                LogModException("The patch was not applied properly.", e);
            }
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

        internal static void LogApplyingPatch(string patchName)
        {
            Debug.Log($"Mod {nameof(VoidGags)}: Applying patch {patchName}...");
        }

        internal static void LogModException(string message, Exception inner = null)
        {
            Debug.LogException(new Exception($"Mod {nameof(VoidGags)}: {message}", innerException: inner));
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

        internal static void LogModTranspilerFailure(string patchName)
        {
            LogModException($"Failed to apply transpiler patch for feature: {patchName}.");
        }

        private static float spLogError = 0f;
        private static string msgError = null;
        internal static void LogModErrorNoSpam(string message, float spamProtection = 0.1f)
        {
            if (Time.time - spLogError > spamProtection && msgError != message)
            {
                msgError = message;
                spLogError = Time.time;
                LogModError(message);
            }
        }

        private static float spLogWarning = 0f;
        private static string msgWarning = null;
        internal static void LogModWarningNoSpam(string message, float spamProtection = 0.1f)
        {
            if (Time.time - spLogWarning > spamProtection && msgWarning != message)
            {
                msgWarning = message;
                spLogWarning = Time.time;
                LogModWarning(message);
            }
        }

        private static float spLogInfo = 0f;
        private static string msgInfo = null;
        internal static void LogModInfoNoSpam(string message, float spamProtection = 0.1f)
        {
            if (Time.time - spLogInfo > spamProtection && msgInfo != message)
            {
                msgInfo = message;
                spLogInfo = Time.time;
                LogModInfo(message);
            }
        }
    }
}
