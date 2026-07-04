using HarmonyLib;
using UnityEngine;
using static ItemActionDynamicMelee;
using static VoidGags.VoidGags.BugFixes;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_BugFixes()
        {
            Harmony.Patch(AccessTools.Method(typeof(PlayerMoveController), nameof(PlayerMoveController.swapItem)),
                prefix: new HarmonyMethod(PlayerMoveController_swapItem.Prefix, priority: Priority.VeryHigh));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionDynamicMelee), nameof(ItemActionDynamicMelee.Raycast)),
                prefix: new HarmonyMethod(ItemActionDynamicMelee_Raycast.Prefix, priority: Priority.VeryHigh));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_LootWindowGroup), nameof(XUiC_LootWindowGroup.openTimerClosedManually)),
                postfix: new HarmonyMethod(XUiC_LootWindowGroup_openTimerClosedManually.Postfix, priority: Priority.VeryHigh));
        }

        /// <summary>
        /// Vanilla bug fixes.
        /// </summary>
        public static class BugFixes
        {
            public static float MeleeSwapTimestamp = 0f;
            public static float PrevMeleeHitTimestamp = 0f;

            /// <summary>
            /// Track swap time during melee attack.
            /// To fix exploit when changing holding weapon during a melee attack.
            /// </summary>
            public static class PlayerMoveController_swapItem
            {
                public static void Prefix(PlayerMoveController __instance)
                {
                    if (IsDedicatedServer) return;

                    if (Time.time - PrevMeleeHitTimestamp > 1f)
                    {
                        var inventory = __instance.entityPlayerLocal.inventory;
                        for (int i = 0; i < 3; i++)
                        {
                            var itemAction = inventory.holdingItem?.Actions[i];
                            if (itemAction != null && itemAction is ItemActionDynamicMelee meleeAction)
                            {
                                var itemActionData = inventory.holdingItemData?.actionData[i] as ItemActionDynamicMeleeData;
                                if (itemActionData != null && itemActionData.Attacking)
                                {
                                    if (itemActionData != null && itemAction.IsActionRunning(itemActionData))
                                    {
                                        MeleeSwapTimestamp = Time.time;
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    if (MeleeSwapTimestamp > 0f && Time.time - MeleeSwapTimestamp < 1.5f)
                    {
                        MeleeSwapTimestamp = 0f;
                    }
                }
            }

            /// <summary>
            /// Fix for exploit when changing holding weapon during a melee attack.
            /// </summary>
            public static class ItemActionDynamicMelee_Raycast
            {
                public static bool Prefix(ref bool __result)
                {
                    if (IsDedicatedServer) return true;

                    if (MeleeSwapTimestamp > 0f && Time.time - MeleeSwapTimestamp < 1.5f)
                    {
                        MeleeSwapTimestamp = 0f;
                        __result = false;
                        return false;
                    }

                    PrevMeleeHitTimestamp = Time.time;
                    return true;
                }
            }

            /// <summary>
            /// Untouch container and remove loot if looting timer was closed manually.
            /// </summary>
            public static class XUiC_LootWindowGroup_openTimerClosedManually
            {
                public static void Postfix(TimerEventData _data)
                {
                    var loot = (((string, ITileEntityLootable))_data.Data).Item2;
                    loot.SetEmpty();
                    loot.bTouched = false;
                    loot.bWasTouched = false;
                    loot.worldTimeTouched = 0;
                    loot.SetModified(); // update block for all players in coop
                }
            }
        }
    }
}
