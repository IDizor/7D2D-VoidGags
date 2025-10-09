using HarmonyLib;
using VoidGags.Types;
using static VoidGags.VoidGags.MoveOnePiece;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_MoveOnePiece()
        {
            LogApplyingPatch(nameof(Settings.MoveOnePiece));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.Update)),
                prefix: new HarmonyMethod(XUiC_ItemStack_Update.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.HandleItemInspect)),
                prefix: new HarmonyMethod(XUiC_ItemStack_HandleItemInspect.Prefix));
        }

        public static class MoveOnePiece
        {
            /// <summary>
            /// Shift + RClick to move only 1 item from the stack.
            /// </summary>
            public static class XUiC_ItemStack_Update
            {
                private static DelayStorage Delay = new(0.2f, new() { { 10, 0.1f } });

                public static void Prefix(XUiC_ItemStack __instance)
                {
                    if (!__instance.WindowGroup.isShowing || __instance.IsLocked || __instance.isDragAndDrop) return;
                    if (!__instance.isOver || UICamera.hoveredObject != __instance.ViewComponent.UiTransform.gameObject || !__instance.ViewComponent.EventOnPress) return;
                    if (!__instance.StackLock && !__instance.ItemStack.IsEmpty() && InputUtils.ShiftKeyPressed)
                    {
                        var cursorController = __instance.xui.playerUI.CursorController;
                        bool rightClick = cursorController.GetMouseButton(UICamera.MouseButton.RightButton);
                        if (rightClick)
                        {
                            if (Delay.Check())
                            {
                                var moveCount = Delay.Counter <= 50 ? 1 : Delay.Counter <= 75 ? 2 : 5;
                                var count = __instance.ItemStack.count;
                                if (count <= moveCount)
                                {
                                    __instance.HandleMoveToPreferredLocation();
                                }
                                else
                                {
                                    var rest = __instance.ItemStack.Clone();
                                    rest.count -= moveCount;
                                    __instance.ItemStack.count = moveCount;
                                    __instance.HandleMoveToPreferredLocation();
                                    if (!__instance.ItemStack.IsEmpty())
                                    {
                                        rest.count += __instance.ItemStack.count;
                                    }
                                    __instance.ForceSetItemStack(rest);
                                }
                            }
                            return;
                        }
                    }
                    Delay.Reset();
                }
            }

            /// <summary>
            /// Ctrl + Click to keep one item and move the rest.
            /// </summary>
            public static class XUiC_ItemStack_HandleItemInspect
            {
                public static bool Prefix(XUiC_ItemStack __instance)
                {
                    if (!__instance.StackLock && !__instance.ItemStack.IsEmpty() && __instance.ItemStack.count > 1 && InputUtils.ControlKeyPressed)
                    {
                        var rest = __instance.ItemStack.Clone();
                        rest.count = 1;
                        __instance.ItemStack.count = __instance.ItemStack.count - 1;
                        __instance.HandleMoveToPreferredLocation();
                        if (__instance.ItemStack.IsEmpty())
                        {
                            __instance.ForceSetItemStack(rest);
                        }
                        else
                        {
                            __instance.ItemStack.count += 1;
                            __instance.HandleSlotChangeEvent();
                        }
                        return false;
                    }
                    return true;
                }
            }
        }
    }
}
