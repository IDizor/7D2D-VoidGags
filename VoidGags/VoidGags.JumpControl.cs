using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_JumpControl()
        {
            LogApplyingPatch(nameof(Settings.JumpControl));

            Harmony.Patch(AccessTools.Method(typeof(vp_FPController), nameof(vp_FPController.UpdateJumpForceWalk)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((vp_FPController __instance) => vp_FPController_UpdateJumpForceWalk.Postfix(__instance))));
        }

        /// <summary>
        /// Allows to control jump height.
        /// Release jump button quickly to make lower jumps.
        /// </summary>
        public class vp_FPController_UpdateJumpForceWalk
        {
            public static bool JumpReset = false;

            public static void Postfix(vp_FPController __instance)
            {
                if (!JumpReset &&
                    __instance.Player.Jump.Active &&
                    !__instance.m_Grounded &&
                    !__instance.localPlayer.inputWasJump &&
                    !__instance.localPlayer.isLadderAttached &&
                    !__instance.localPlayer.IsSwimming() &&
                    __instance.m_MotorThrottle.y > 0f)
                {
                    __instance.m_MotorThrottle.y *= 0.5f;
                    JumpReset = true;
                    return;
                }

                if (JumpReset && __instance.m_Grounded)
                {
                    JumpReset = false;
                }
            }
        }
    }
}
