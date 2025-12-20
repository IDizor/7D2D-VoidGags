using HarmonyLib;
using UnityEngine;
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
        }

        public static class BugFixes
        {
            public static float MeleeSwapTimestamp = 0f;

            /// <summary>
            /// Fix melee attack when changing holding item.
            /// Track swap time during melee attack.
            /// </summary>
            public static class PlayerMoveController_swapItem
            {
                public static void Prefix(PlayerMoveController __instance)
                {
                    if (IsDedicatedServer) return;

                    var inventory = __instance.entityPlayerLocal.inventory;
                    for (int i = 0; i < 3; i++)
                    {
                        var itemAction = inventory.holdingItem?.Actions[i];
                        if (itemAction != null && itemAction is ItemActionDynamicMelee)
                        {
                            var itemActionData = inventory.holdingItemData?.actionData[i];
                            if (itemActionData != null && itemAction.IsActionRunning(itemActionData))
                            {
                                MeleeSwapTimestamp = Time.time;
                                return;
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
            /// Fix melee attack when changing holding item.
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

                    return true;
                }
            }
        }
    }
}
