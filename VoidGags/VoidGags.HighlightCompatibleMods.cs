using HarmonyLib;
using UnityEngine;
using VoidGags.Types;
using static VoidGags.VoidGags.HighlightCompatibleMods;
using static XUiC_ItemStack;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_HighlightCompatibleMods()
        {
            LogApplyingPatch(nameof(Settings.HighlightCompatibleMods));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.SetInfo)),
                prefix: new HarmonyMethod(XUiC_ItemInfoWindow_SetInfo.Prefix),
                postfix: new HarmonyMethod(XUiC_ItemInfoWindow_SetInfo.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.ShowEmptyInfo)),
                prefix: new HarmonyMethod(XUiC_ItemInfoWindow_ShowEmptyInfo.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_InfoWindow), nameof(XUiC_InfoWindow.OnVisibilityChanged), [typeof(XUiController), typeof(bool), typeof(bool)]),
                prefix: new HarmonyMethod(XUiC_InfoWindow_OnVisibilityChanged.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.updateLockTypeIcon)),
                prefix: new HarmonyMethod(XUiC_ItemStack_updateLockTypeIcon.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_RecipeEntry), nameof(XUiC_RecipeEntry.SetRecipeAndHasIngredients)),
                postfix: new HarmonyMethod(XUiC_RecipeEntry_SetRecipeAndHasIngredients.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_PartList), nameof(XUiC_PartList.SetSlots)),
                prefix: new HarmonyMethod(XUiC_PartList_SetSlots.Prefix),
                postfix: new HarmonyMethod(XUiC_PartList_SetSlots.Postfix));
        }

        public static class HighlightCompatibleMods
        {
            public static ItemStack SelectedItem = null;
            
            public static void SetItemStackGridsDirty()
            {
                Helper.FindControllersByType<XUiC_ItemStackGrid>().ForEach(c =>
                {
                    if (c.GetParentWindow().IsVisible)
                        c.SetAllChildrenDirty();
                });
            }

            public static void RefreshRecipeLists()
            {
                Helper.FindControllersByType<XUiC_RecipeList>().ForEach(c =>
                {
                    if (c.GetParentWindow().IsVisible)
                    {
                        c.IsDirty = true;
                        c.pageChanged = true;
                    }
                });
            }

            public static bool IsAssembleWindowActive()
            {
                var windowManager = Helper.PlayerLocal?.playerUI?.windowManager;
                return windowManager?.IsWindowOpen(XUiC_AssembleWindowGroup.ID) == true;
            }

            public static void ClearSelectedItem()
            {
                //LogWarning($"Selected item cleared {SelectedItem?.itemValue?.ItemClass.Name}");
                SelectedItem = null;
                SetItemStackGridsDirty();
                RefreshRecipeLists();
            }

            /// <summary>
            /// Track selected item using the item info window.
            /// </summary>
            public static class XUiC_ItemInfoWindow_SetInfo
            {
                public static void Prefix(XUiC_ItemInfoWindow __instance)
                {
                    // Clear AssembleItem.CurrentItem to have a correct action list in the item info window.
                    if (SelectedItem != null &&
                        __instance.xui.AssembleItem != null &&
                        __instance.xui.AssembleItem.CurrentItem == SelectedItem &&
                        !IsAssembleWindowActive())
                    {
                        __instance.xui.AssembleItem.CurrentItem = null;
                    }
                }

                public static void Postfix(XUiC_ItemInfoWindow __instance, ItemStack stack)
                {
                    if (stack?.itemValue?.HasModSlots == true)
                    {
                        if (!IsAssembleWindowActive())
                        {
                            SelectedItem = stack.IsEmpty() == false ? stack.Clone() : null;
                            //if (SelectedItem != null) LogWarning($"Selected item is set to {SelectedItem.itemValue?.ItemClass.Name}");
                            SetItemStackGridsDirty();
                            RefreshRecipeLists();
                        }
                    }
                    else if (SelectedItem != null)
                    {
                        if (!IsAssembleWindowActive() && SelectedItem == __instance.xui.AssembleItem.CurrentItem)
                        {
                            __instance.xui.AssembleItem.CurrentItem = null;
                        }
                        ClearSelectedItem();
                    }
                }
            }

            /// <summary>
            /// Clear selected item.
            /// </summary>
            public static class XUiC_ItemInfoWindow_ShowEmptyInfo
            {
                public static void Prefix(XUiC_ItemInfoWindow __instance)
                {
                    if (SelectedItem != null)
                    {
                        if (!IsAssembleWindowActive() && SelectedItem == __instance.xui.AssembleItem.CurrentItem)
                        {
                            __instance.xui.AssembleItem.CurrentItem = null;
                        }
                        ClearSelectedItem();
                    }
                }
            }

            /// <summary>
            /// Clear selected item.
            /// </summary>
            public static class XUiC_InfoWindow_OnVisibilityChanged
            {
                public static void Prefix(XUiC_InfoWindow __instance, bool _visibleInScene)
                {
                    if (SelectedItem != null && !_visibleInScene && __instance is XUiC_ItemInfoWindow)
                    {
                        if (!IsAssembleWindowActive() && SelectedItem == __instance.xui.AssembleItem.CurrentItem)
                        {
                            __instance.xui.AssembleItem.CurrentItem = null;
                        }
                        ClearSelectedItem();
                    }
                }
            }

            /// <summary>
            /// Update mod item icon according to selected item compatibility.
            /// </summary>
            public static class XUiC_ItemStack_updateLockTypeIcon
            {
                public static void Prefix(XUiC_ItemStack __instance)
                {
                    if (SelectedItem != null &&
                        __instance.xui.AssembleItem != null &&
                        __instance.xui.AssembleItem.CurrentItem != SelectedItem &&
                        !IsAssembleWindowActive())
                    {
                        __instance.xui.AssembleItem.CurrentItem = SelectedItem;
                    }
                }
            }

            /// <summary>
            /// Colorize compatible mods in recipe list.
            /// </summary>
            public static class XUiC_RecipeEntry_SetRecipeAndHasIngredients
            {
                public static void Postfix(XUiC_RecipeEntry __instance)
                {
                    if (SelectedItem != null && __instance.lblName != null)
                    {
                        if (__instance.Recipe?.GetOutputItemClass() is ItemClassModifier)
                        {
                            var stack = new XUiC_ItemStack();
                            stack.itemStack = new(new(__instance.Recipe.itemValueType), 1);
                            stack.xui = __instance.xui;
                            stack.updateLockTypeIcon();
                            if (stack.flashLockTypeIcon == flashLockTypes.Allowed || stack.flashLockTypeIcon == flashLockTypes.AlreadyEquipped)
                            {
                                var color = stack.flashLockTypeIcon == flashLockTypes.Allowed
                                    ? stack.modAllowedColor
                                    : stack.modAlreadyEquippedColor;
                                __instance.lblName.Color = Color.Lerp(__instance.lblName.Color, color, 0.4f);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Greys out not allowed mod slots and makes mods clickable in info window.
            /// </summary>
            public static class XUiC_PartList_SetSlots
            {
                private static FastTags<TagGroup.Global> Dye = FastTags<TagGroup.Global>.Parse("dye");
                private static Color32? EnabledColor = null;
                private static Color32 DisabledColor = Color.clear;

                public static void Prefix(XUiC_PartList __instance, ref int startIndex)
                {
                    if (startIndex == 1 && __instance.itemControllers?.Length > 0)
                    {
                        var stack = __instance.itemControllers[0];
                        if (stack.itemClass?.HasAllTags(Dye) == true)
                        {
                            startIndex = 0;
                        }
                    }
                }

                public static void Postfix(XUiC_PartList __instance)
                {
                    if (__instance.GetParentWindow().Controller is XUiC_ItemInfoWindow window)
                    {
                        if (!window.itemStack.IsEmpty())
                        {
                            var modsLimit = window.itemStack.itemValue.Modifications?.Length ?? 0;
                            for (int i = 0; i < __instance.itemControllers.Length; i++)
                            {
                                var stack = __instance.itemControllers[i];

                                // store initial color
                                if (EnabledColor is null)
                                {
                                    EnabledColor = stack.backgroundColor;
                                    DisabledColor = Color32.Lerp(EnabledColor.Value, new Color32(0, 0, 0, EnabledColor.Value.a), 0.85f);
                                }

                                if (!stack.ItemStack.IsEmpty())
                                {
                                    // make stack clickable
                                    stack.ViewComponent.EventOnPress = true;
                                    stack.OnPress += (_, _) =>
                                    {
                                        if (!stack.ItemStack.IsEmpty() && stack.itemClass != null)
                                        {
                                            var initialItemStack = window.selectedItemStack;
                                            var mod = new XUiC_ItemStack() { xui = stack.xui };
                                            mod.setItemStack(stack.ItemStack);
                                            window.SetItemStack(mod, _makeVisible: true);
                                            window.mainActionItemList.SetCraftingActionList(XUiC_ItemActionList.ItemActionListTypes.None, mod);
                                            window.mainActionItemList.AddActionListEntry(new ItemActionEntryCustom(mod, () =>
                                            {
                                                window.SetItemStack(initialItemStack, _makeVisible: true);
                                            }));
                                        }
                                    };
                                }

                                // update available mod stacks color
                                stack.backgroundColor = i < modsLimit ? EnabledColor.Value : DisabledColor;
                            }
                        }
                    }
                }
            }
        }
    }
}
