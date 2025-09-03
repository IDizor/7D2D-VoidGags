using System.Linq;
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
                postfix: new HarmonyMethod(XUiC_ItemInfoWindow_SetInfo.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.ShowEmptyInfo)),
                prefix: new HarmonyMethod(XUiC_ItemInfoWindow_ShowEmptyInfo.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_InfoWindow), nameof(XUiC_InfoWindow.OnVisibilityChanged)),
                prefix: new HarmonyMethod(XUiC_InfoWindow_OnVisibilityChanged.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.updateLockTypeIcon)),
                postfix: new HarmonyMethod(XUiC_ItemStack_updateLockTypeIcon.Postfix));

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

            public static void ClearSelectedItem()
            {
                SelectedItem = null;
                SetItemStackGridsDirty();
                RefreshRecipeLists();
            }

            /// <summary>
            /// Track selected item using the item info window.
            /// </summary>
            public static class XUiC_ItemInfoWindow_SetInfo
            {
                public static void Postfix(XUiC_ItemInfoWindow __instance, ItemStack stack)
                {
                    if (__instance.mainActionItemList?.itemActionEntries != null &&
                        __instance.mainActionItemList.itemActionEntries.Any(a => a is ItemActionEntryAssemble)) // if item is modifyable
                    {
                        SelectedItem = stack?.IsEmpty() == false ? stack.Clone() : null;
                        SetItemStackGridsDirty();
                        RefreshRecipeLists();
                    }
                    else if (SelectedItem != null)
                    {
                        ClearSelectedItem();
                    }
                }
            }

            /// <summary>
            /// Clear selected item.
            /// </summary>
            public static class XUiC_ItemInfoWindow_ShowEmptyInfo
            {
                public static void Prefix()
                {
                    if (SelectedItem != null)
                    {
                        ClearSelectedItem();
                    }
                }
            }

            /// <summary>
            /// Clear selected item.
            /// </summary>
            public static class XUiC_InfoWindow_OnVisibilityChanged
            {
                public static void Prefix(XUiC_InfoWindow __instance, bool _isVisible)
                {
                    if (!_isVisible && __instance is XUiC_ItemInfoWindow)
                    {
                        ClearSelectedItem();
                    }
                }
            }

            /// <summary>
            /// Update mod item icon according to selected item compatibility.
            /// </summary>
            public static class XUiC_ItemStack_updateLockTypeIcon
            {
                public static void Postfix(XUiC_ItemStack __instance)
                {
                    if (SelectedItem != null && __instance.flashLockTypeIcon == flashLockTypes.None && __instance.itemClass is ItemClassModifier itemClassModifier)
                    {
                        /// TODO: before new release compare this block with the original code from <see cref="XUiC_ItemStack.updateLockTypeIcon"/>.
                        /// Latest original code was:

                        //if (itemClass is ItemClassModifier itemClassModifier)
                        //{
                        //    lockSprite = "ui_game_symbol_assemble";
                        //    if (itemClassModifier.HasAnyTags(ItemClassModifier.CosmeticModTypes))
                        //    {
                        //        lockSprite = "ui_game_symbol_paint_bucket";
                        //    }
                        //    if (base.xui.AssembleItem.CurrentItem != null)
                        //    {
                        //        if ((itemClassModifier.InstallableTags.IsEmpty || base.xui.AssembleItem.CurrentItem.itemValue.ItemClass.HasAnyTags(itemClassModifier.InstallableTags)) && !base.xui.AssembleItem.CurrentItem.itemValue.ItemClass.HasAnyTags(itemClassModifier.DisallowedTags))
                        //        {
                        //            if (StackLocation != StackLocationTypes.Part)
                        //            {
                        //                for (int i = 0; i < base.xui.AssembleItem.CurrentItem.itemValue.Modifications.Length; i++)
                        //                {
                        //                    ItemValue itemValue = base.xui.AssembleItem.CurrentItem.itemValue.Modifications[i];
                        //                    if (!itemValue.IsEmpty() && itemValue.ItemClass.HasAnyTags(itemClassModifier.ItemTags))
                        //                    {
                        //                        flashLockTypeIcon = flashLockTypes.AlreadyEquipped;
                        //                        return;
                        //                    }
                        //                }
                        //            }
                        //            flashLockTypeIcon = flashLockTypes.Allowed;
                        //        }
                        //        else
                        //        {
                        //            setLockTypeIconColor(Color.grey);
                        //            flashLockTypeIcon = flashLockTypes.None;
                        //        }
                        //    }
                        //    else
                        //    {
                        //        setLockTypeIconColor(Color.white);
                        //        flashLockTypeIcon = flashLockTypes.None;
                        //    }
                        //}

                        /// Converted to the following code (SelectedItem should be used instead the base.xui.AssembleItem.CurrentItem) : 
                        if ((itemClassModifier.InstallableTags.IsEmpty || SelectedItem.itemValue.ItemClass.HasAnyTags(itemClassModifier.InstallableTags)) && !SelectedItem.itemValue.ItemClass.HasAnyTags(itemClassModifier.DisallowedTags))
                        {
                            if (__instance.StackLocation != StackLocationTypes.Part && SelectedItem.itemValue.Modifications != null) // added own check "Modifications != null"
                            {
                                for (int i = 0; i < SelectedItem.itemValue.Modifications.Length; i++)
                                {
                                    ItemValue itemValue = SelectedItem.itemValue.Modifications[i];
                                    if (itemValue != null && !itemValue.IsEmpty() && itemValue.ItemClass?.HasAnyTags(itemClassModifier.ItemTags) == true) // added own null checks "itemValue != null", and ItemClass"." -> "?."
                                    {
                                        __instance.flashLockTypeIcon = flashLockTypes.AlreadyEquipped;
                                        return;
                                    }
                                }
                            }
                            __instance.flashLockTypeIcon = flashLockTypes.Allowed;
                        }
                        /// End of block to compare
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
                                XUiC_ItemStack stack = __instance.itemControllers[i];

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
                                            window.SetItemStack(stack, _makeVisible: true);
                                            window.mainActionItemList.SetCraftingActionList(XUiC_ItemActionList.ItemActionListTypes.None, stack);
                                            window.mainActionItemList.AddActionListEntry(new ItemActionEntryCustom(stack, () =>
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
