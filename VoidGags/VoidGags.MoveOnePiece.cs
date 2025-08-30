using HarmonyLib;
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

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.HandleItemInspect)),
                prefix: new HarmonyMethod(XUiC_ItemStack_HandleItemInspect.Prefix));
        }

        public static class MoveOnePiece
        {
            /// <summary>
            /// Ctrl+Click to move only 1 item from the stack.
            /// </summary>
            public static class XUiC_ItemStack_HandleItemInspect
            {
                public static bool Prefix(XUiC_ItemStack __instance)
                {
                    if (!__instance.StackLock && !__instance.ItemStack.IsEmpty() && InputUtils.ControlKeyPressed)
                    {
                        var count = __instance.ItemStack.count;
                        if (count == 1)
                        {
                            __instance.HandleMoveToPreferredLocation();
                        }
                        else
                        {
                            var rest = __instance.ItemStack.Clone();
                            __instance.ItemStack.count = 1;
                            __instance.HandleMoveToPreferredLocation();
                            if (__instance.ItemStack.IsEmpty())
                            {
                                rest.count--;
                            }
                            __instance.ForceSetItemStack(rest);
                            Helper.DeferredAction(0.001f, () => __instance.Hovered(true));
                        }
                        return false;
                    }
                    return true;
                }
            }
        }
    }
}
