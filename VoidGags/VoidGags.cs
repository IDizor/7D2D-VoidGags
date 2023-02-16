using System;
using System.Collections.Generic;
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
        private static Queue<Action> OnGameLoadedActions = new Queue<Action>();
        
        /// <summary>
        /// Mod initialization.
        /// </summary>
        /// <param name="_modInstance"></param>
        public void InitMod(Mod _modInstance)
        {
            Debug.Log("Loading mod: " + GetType().ToString());
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
            if (Settings.SaveLockedSlotsCount) ApplyPatches_SaveLockedSlots(harmony);
            if (Settings.ScrapTimeAndSalvageOperations) ApplyPatches_ScrapTime(harmony);
        }

        /// <summary>
        /// Method that called once the game is loaded.
        /// </summary>
        [HarmonyPatch(typeof(GameManager))]
        [HarmonyPatch("PlayerSpawnedInWorld")]
        public class GameManager_PlayerSpawnedInWorld
        {
            public static void Postfix(RespawnType _respawnReason)
            {
                if (_respawnReason == RespawnType.LoadedGame)
                {
                    while (OnGameLoadedActions.Count > 0)
                    {
                        OnGameLoadedActions.Dequeue()();
                    }
                }
            }
        }
    }
}
