using HarmonyLib;
using static VoidGags.VoidGags.SprintModeHold;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SprintModeHold()
        {
            LogApplyingPatch(nameof(Settings.SprintModeHold));

            Harmony.Patch(AccessTools.Method(typeof(PlayerMoveController), nameof(PlayerMoveController.Update)),
                prefix: new HarmonyMethod(PlayerMoveController_Update.Prefix));
        }

        public static class SprintModeHold
        {
            /// <summary>
            /// If sprint mode is "Hold" - disable tapping autorun toggle. Hold only.
            /// </summary>
            public static class PlayerMoveController_Update
            {
                public static void Prefix(PlayerMoveController __instance)
                {
                    if (__instance.sprintMode == PlayerMoveController.cSprintModeHold)
                    {
                        __instance.runInputTime = 1f;
                    }
                }
            }
        }
    }
}
