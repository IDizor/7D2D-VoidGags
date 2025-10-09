using System.Collections.Generic;
using System.Linq;
using Audio;
using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.EnqueueCraftWhenNoFuel;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_EnqueueCraftWhenNoFuel()
        {
            LogApplyingPatch(nameof(Settings.EnqueueCraftWhenNoFuel));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationFuelGrid), nameof(XUiC_WorkstationFuelGrid.HasRequirement)),
                postfix: new HarmonyMethod(XUiC_WorkstationFuelGrid_HasRequirement.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationFuelGrid), nameof(XUiC_WorkstationFuelGrid.TurnOn)),
                postfix: new HarmonyMethod(XUiC_WorkstationFuelGrid_TurnOn.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationWindowGroup), nameof(XUiC_WorkstationWindowGroup.OnClose)),
                prefix: new HarmonyMethod(XUiC_WorkstationWindowGroup_OnClose.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(Manager), nameof(Manager.BroadcastPlayByLocalPlayer)),
                prefix: new HarmonyMethod(Manager_BroadcastPlayByLocalPlayer.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationWindowGroup), nameof(XUiC_WorkstationWindowGroup.Update)),
                postfix: new HarmonyMethod(XUiC_WorkstationWindowGroup_Update.Postfix));
        }

        public static class EnqueueCraftWhenNoFuel
        {
            public static bool SuppressWorkstationCloseSound = false;

            /// <summary>
            /// Allow to enqueue item to craft when no fuel in the workstation.
            /// </summary>
            public static class XUiC_WorkstationFuelGrid_HasRequirement
            {
                public static void Postfix(XUiC_WorkstationFuelGrid __instance, ref bool __result)
                {
                    if (!__result && !__instance.HasFuelAndCanStart())
                    {
                        var caller = Helper.GetCallerMethod();
                        __result = caller.DeclaringType != __instance.GetType();
                    }
                }
            }

            /// <summary>
            /// Immediate turn-off the workstation when no fuel.
            /// </summary>
            public static class XUiC_WorkstationFuelGrid_TurnOn
            {
                public static void Postfix(XUiC_WorkstationFuelGrid __instance)
                {
                    if (__instance.isOn && !__instance.HasFuelAndCanStart())
                    {
                        __instance.TurnOff();
                    }
                }
            }

            /// <summary>
            /// Display warning when leaving workstation turned off.
            /// </summary>
            public static class XUiC_WorkstationWindowGroup_OnClose
            {
                public static void Prefix(XUiC_WorkstationWindowGroup __instance)
                {
                    SuppressWorkstationCloseSound = false;

                    // if it has fuel window
                    if (__instance.fuelWindow != null)
                    {
                        var warning = !__instance.fuelWindow.isOn;
                        warning &= __instance.craftingQueue?.queueItems.Any(i =>
                            ((XUiC_RecipeStack)i).recipe != null && ((XUiC_RecipeStack)i).recipeCount > 0) == true;

                        if (warning)
                        {
                            SuppressWorkstationCloseSound = true;
                            Helper.DeferredAction(0.1f, () => SuppressWorkstationCloseSound = false);
                            XUiC_PopupToolTip.QueueTooltip(Helper.PlayerLocal.PlayerUI.xui, $"{Localization.Get(__instance.workstation)} : {Localization.Get("TwitchAction_EmptyFuel")} / {Localization.Get("goDisabled")}",
                                _args: null, _alertSound: "keystone_build_warning", _eventHandler: null, _showImmediately: false, _pinTooltip: false);
                        }
                    }
                }
            }

            /// <summary>
            /// Suppress workstation close sound.
            /// </summary>
            public static class Manager_BroadcastPlayByLocalPlayer
            {
                public static bool Prefix()
                {
                    if (SuppressWorkstationCloseSound)
                    {
                        var caller = Helper.GetCallerMethod();
                        return caller.DeclaringType != typeof(XUiC_WorkstationWindowGroup);
                    }

                    return true;
                }
            }

            /// <summary>
            /// Change color for fuel burn time counter.
            /// </summary>
            public static class XUiC_WorkstationWindowGroup_Update
            {
                public static void Postfix(XUiC_WorkstationWindowGroup __instance)
                {
                    // synchronize updating totalBurnTimeLeft and totalCraftTime
                    if (__instance.openTEUpdateTime < 0.5f)
                        return;

                    // if it has fuel windows and crafting queue
                    if (__instance.fuelWindow != null && __instance.craftingQueue != null && __instance.burnTimeLeft != null && __instance.WorkstationData != null)
                    {
                        var craftTimes = new List<double>();
                        double totalBurnTimeLeft = Mathf.Max(__instance.WorkstationData.GetTotalBurnTimeLeft() - 0.5f, 0f); // game set totalBurnTimeLeft greater by 0.5 by some reason
                        var queue = __instance.craftingQueue.GetRecipesToCraft();

                        //foreach (var it in queue)
                        //{
                        //    if (it.recipe?.count > 0) // log to check times synchronization
                        //        LogModWarningNoSpam($"totalBurnTimeLeft = {totalBurnTimeLeft:0.0000}, totalCraftTimeLeft = {it.totalCraftTimeLeft:0.0000}");
                        //}

                        double totalCraftTime = queue.Where(i => i.recipe?.count > 0).Sum(i => i.totalCraftTimeLeft);
                        craftTimes.Add(totalCraftTime);

                        // if it has input window (like forge)
                        if (__instance.inputWindow != null)
                        {
                            var workstationEntity = __instance.WorkstationData.tileEntity;

                            for (int i = 0; i < __instance.inputWindow.WorkstationData.TileEntity.Input.Length && i < __instance.inputWindow.itemControllers.Length; i++)
                            {
                                var itemClass = workstationEntity.input[i]?.itemValue?.ItemClass;
                                if (itemClass?.MadeOfMaterial.ForgeCategory == null)
                                {
                                    continue;
                                }
                                ItemClass materialClass = ItemClass.GetItemClass("unit_" + itemClass.MadeOfMaterial.ForgeCategory);
                                if (materialClass == null || materialClass.MadeOfMaterial.ForgeCategory == null)
                                {
                                    continue;
                                }

                                // calculate total melt time for input cell
                                var inputCount = workstationEntity.input[i].count;
                                if (inputCount > 0)
                                {
                                    var meltTime = 0d;
                                    var unitMeltTime = (float)itemClass.GetWeight() * ((itemClass.MeltTimePerUnit > 0f) ? itemClass.MeltTimePerUnit : 1f);
                                    if (workstationEntity.isModuleUsed[0])
                                    {
                                        for (int k = 0; k < workstationEntity.tools.Length; k++)
                                        {
                                            float _perc_value = 1f;
                                            workstationEntity.tools[k].itemValue.ModifyValue(null, null, PassiveEffects.CraftingSmeltTime,
                                                ref unitMeltTime, ref _perc_value, FastTags<TagGroup.Global>.Parse(itemClass.Name));
                                            unitMeltTime *= _perc_value;
                                        }
                                    }

                                    var slotMeltTimeLeft = Mathf.Max(__instance.inputWindow.WorkstationData.TileEntity.GetTimerForSlot(i), 0f);

                                    meltTime += (double)unitMeltTime * (inputCount - 1);
                                    meltTime += slotMeltTimeLeft > 0f ? slotMeltTimeLeft : unitMeltTime;
                                    if (meltTime > 0d)
                                    {
                                        //LogModWarningNoSpam($"totalBurnTimeLeft = {totalBurnTimeLeft:0.0000}, meltTime = {meltTime+0.5d:0.0000}");
                                        craftTimes.Add(meltTime + 0.5d); // have to add 0.5 sec for correct behavior
                                    }
                                }
                            }
                        }

                        if (craftTimes.Sum() < 0.0001d)
                        {
                            __instance.burnTimeLeft.Color = Color.white;
                        }
                        else if (totalBurnTimeLeft < craftTimes.Max())
                        {
                            __instance.burnTimeLeft.Color = Color.red;
                        }
                        else
                        {
                            __instance.burnTimeLeft.Color = Color.green;
                        }
                    }
                }
            }
        }
    }
}
