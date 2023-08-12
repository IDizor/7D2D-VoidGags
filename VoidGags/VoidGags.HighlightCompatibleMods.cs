using HarmonyLib;
using UnityEngine;
using static XUiC_ItemStack;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_HighlightCompatibleMods(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), "SetInfo"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemStack stack) => XUiC_ItemInfoWindow_SetInfo.Prefix(stack))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemInfoWindow), "ShowEmptyInfo"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => XUiC_ItemInfoWindow_ShowEmptyInfo.Prefix())));

            harmony.Patch(AccessTools.Method(typeof(XUiC_InfoWindow), "OnVisibilityChanged"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_InfoWindow_OnVisibilityChanged.APostfix p) => XUiC_InfoWindow_OnVisibilityChanged.Postfix(p.__instance, p._isVisible))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), "updateLockTypeIcon"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStack_updateLockTypeIcon.APrefix p) => XUiC_ItemStack_updateLockTypeIcon.Prefix(p.__instance, p.___lockType, ref p.___lockSprite, p.___lockTypeIcon))));

            LogPatchApplied(nameof(Settings.HighlightCompatibleMods));
        }

        /// <summary>
        /// Tracks the selected item using item info window.
        /// </summary>
        public class XUiC_ItemInfoWindow_SetInfo
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
        public class XUiC_ItemInfoWindow_ShowEmptyInfo
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
        public class XUiC_InfoWindow_OnVisibilityChanged
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
        public class XUiC_ItemStack_updateLockTypeIcon
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
    }
}
