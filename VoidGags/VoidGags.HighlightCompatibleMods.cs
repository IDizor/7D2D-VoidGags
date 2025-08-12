using HarmonyLib;
using UnityEngine;
using VoidGags.Types;
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
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemStack stack) => XUiC_ItemInfoWindow_SetInfo.Prefix(stack))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.ShowEmptyInfo)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => XUiC_ItemInfoWindow_ShowEmptyInfo.Prefix())));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_InfoWindow), nameof(XUiC_InfoWindow.OnVisibilityChanged)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_InfoWindow_OnVisibilityChanged.APostfix p) => XUiC_InfoWindow_OnVisibilityChanged.Postfix(p.__instance, p._isVisible))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.updateLockTypeIcon)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStack_updateLockTypeIcon.APrefix p) => XUiC_ItemStack_updateLockTypeIcon.Prefix(p.__instance, p.___lockType, ref p.___lockSprite, p.___lockTypeIcon))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_PartList), nameof(XUiC_PartList.SetSlots)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_PartList __instance) => XUiC_PartList_SetSlots.Postfix(__instance))));
        }

        /// <summary>
        /// Tracks the selected item using item info window.
        /// </summary>
        public static class XUiC_ItemInfoWindow_SetInfo
        {
            public static ItemClass SelectedItem = null;

            public static void Prefix(ItemStack stack)
            {
                SelectedItem = stack.IsEmpty() ? null : stack.itemValue.ItemClass;
                SetItemStackGridsDirty();
            }

            public static void SetItemStackGridsDirty()
            {
                Helper.FindControllersByType<XUiC_ItemStackGrid>().ForEach(c => c.SetAllChildrenDirty());
            }
        }

        /// <summary>
        /// Clears selected item.
        /// </summary>
        public static class XUiC_ItemInfoWindow_ShowEmptyInfo
        {
            public static void Prefix()
            {
                XUiC_ItemInfoWindow_SetInfo.SelectedItem = null;
                XUiC_ItemInfoWindow_SetInfo.SetItemStackGridsDirty();
            }
        }

        /// <summary>
        /// Clears selected item.
        /// </summary>
        public static class XUiC_InfoWindow_OnVisibilityChanged
        {
            public struct APostfix
            {
                public XUiC_InfoWindow __instance;
                public bool _isVisible;
            }

            public static void Postfix(XUiC_InfoWindow __instance, bool _isVisible)
            {
                if (!_isVisible && __instance is XUiC_ItemInfoWindow)
                {
                    XUiC_ItemInfoWindow_SetInfo.SelectedItem = null;
                }
            }
        }

        /// <summary>
        /// Highlights compatible mods gears icon.
        /// </summary>
        public static class XUiC_ItemStack_updateLockTypeIcon
        {
            public struct APrefix
            {
                public XUiC_ItemStack __instance;
                public LockTypes ___lockType;
                public string ___lockSprite;
                public XUiV_Sprite ___lockTypeIcon;
            }

            public static bool Prefix(XUiC_ItemStack __instance, LockTypes ___lockType, ref string ___lockSprite, XUiV_Sprite ___lockTypeIcon)
            {
                var selectedItem = XUiC_ItemInfoWindow_SetInfo.SelectedItem;
                if (selectedItem != null && !(__instance.Parent is XUiC_PartList))
                {
                    if (__instance.IsLocked && ___lockType != 0)
                    {
                        return true;
                    }
                    var itemClass = __instance.ItemStack?.itemValue?.ItemClass;
                    if (itemClass is ItemClassModifier itemClassModifier)
                    {
                        ___lockSprite = "ui_game_symbol_assemble";
                        if (!itemClassModifier.HasAnyTags(ItemClassModifier.CosmeticModTypes))
                        {
                            if (__instance.xui.AssembleItem.CurrentItem == null)
                            {
                                if ((itemClassModifier.InstallableTags.IsEmpty || selectedItem.HasAnyTags(itemClassModifier.InstallableTags)) && !selectedItem.HasAnyTags(itemClassModifier.DisallowedTags))
                                {
                                    if (___lockTypeIcon != null)
                                    {
                                        ___lockTypeIcon.Color = Color.Lerp(Color.green, Color.white, 0.33f);
                                    }
                                    return false;
                                }

                            }
                        }
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Greys out not allowed mod slots and makes mods clickable in info window.
        /// </summary>
        public static class XUiC_PartList_SetSlots
        {
            private static Color32? EnabledColor = null;
            private static Color32 DisabledColor = Color.clear;

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
