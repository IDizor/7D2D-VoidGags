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

            Harmony.Patch(AccessTools.PropertyGetter(typeof(XUiM_AssembleItem), nameof(XUiM_AssembleItem.CurrentItem)),
                postfix: new HarmonyMethod(XUiM_AssembleItem_CurrentItem_Getter.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.SetInfo)),
                postfix: new HarmonyMethod(XUiC_ItemInfoWindow_SetInfo.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.ShowEmptyInfo)),
                prefix: new HarmonyMethod(XUiC_ItemInfoWindow_ShowEmptyInfo.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_InfoWindow), nameof(XUiC_InfoWindow.OnVisibilityChanged)),
                postfix: new HarmonyMethod(XUiC_InfoWindow_OnVisibilityChanged.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.updateLockTypeIcon)),
                prefix: new HarmonyMethod(XUiC_ItemStack_updateLockTypeIcon.Prefix),
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
            public static bool IsUpdateIconMethod = false;

            public static void SetItemStackGridsDirty()
            {
                Helper.FindControllersByType<XUiC_ItemStackGrid>().ForEach(c => c.SetAllChildrenDirty());
            }

            public static void RefreshRecipeLists()
            {
                Helper.FindControllersByType<XUiC_RecipeList>().ForEach(c =>
                {
                    c.IsDirty = true;
                    c.pageChanged = true;
                });
            }

            /// <summary>
            /// Track selected item using item info window.
            /// </summary>
            public static class XUiC_ItemInfoWindow_SetInfo
            {
                public static void Postfix(XUiC_ItemInfoWindow __instance, ItemStack stack)
                {
                    // if item is modifyable
                    if (__instance.mainActionItemList?.itemActionEntries != null &&
                        __instance.mainActionItemList.itemActionEntries.Any(a => a is ItemActionEntryAssemble))
                    {
                        SelectedItem = stack.IsEmpty() ? null : stack;
                        SetItemStackGridsDirty();
                        RefreshRecipeLists();
                    }
                    else if (SelectedItem != null)
                    {
                        SelectedItem = null;
                        SetItemStackGridsDirty();
                        RefreshRecipeLists();
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
                        SelectedItem = null;
                        SetItemStackGridsDirty();
                        RefreshRecipeLists();
                    }
                }
            }

            /// <summary>
            /// Clear selected item.
            /// </summary>
            public static class XUiC_InfoWindow_OnVisibilityChanged
            {
                public static void Postfix(XUiC_InfoWindow __instance, bool _isVisible)
                {
                    if (!_isVisible && __instance is XUiC_ItemInfoWindow)
                    {
                        SelectedItem = null;
                    }
                }
            }

            /// <summary>
            /// Substitutes assembling item if empty.
            /// </summary>
            public static class XUiM_AssembleItem_CurrentItem_Getter
            {
                public static void Postfix(ref ItemStack __result)
                {
                    if (IsUpdateIconMethod &&
                        SelectedItem != null &&
                        (__result == null || __result.IsEmpty()))
                    {
                        __result = SelectedItem;
                    }
                }
            }

            /// <summary>
            /// Track update icon method.
            /// </summary>
            public static class XUiC_ItemStack_updateLockTypeIcon
            {
                public static void Prefix()
                {
                    IsUpdateIconMethod = true;
                }

                public static void Postfix()
                {
                    IsUpdateIconMethod = false;
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
                        if (__instance.Recipe?.GetOutputItemClass() is ItemClassModifier itemClass)
                        {
                            var stack = new XUiC_ItemStack();
                            stack.itemStack = new(new(__instance.Recipe.itemValueType), 0);
                            stack.xui = __instance.xui;
                            stack.updateLockTypeIcon();
                            if (stack.flashLockTypeIcon == flashLockTypes.Allowed || stack.flashLockTypeIcon == flashLockTypes.AlreadyEquipped)
                            {
                                var color = stack.flashLockTypeIcon == flashLockTypes.Allowed
                                    ? stack.modAllowedColor
                                    : stack.modAlreadyEquippedColor;
                                __instance.lblName.Color = Color.Lerp(__instance.lblName.Color, color, 0.4f);
                                __instance.IsDirty = true;
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
