using HarmonyLib;
using InControl;
using static VoidGags.VoidGags.JumpControl;

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
                postfix: new HarmonyMethod(vp_FPController_UpdateJumpForceWalk.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.MoveByInput)),
                prefix: new HarmonyMethod(EntityPlayerLocal_MoveByInput.Prefix, priority: Priority.VeryHigh));
        }

        public static class JumpControl
        {
            /// <summary>
            /// Allows to control jump height.
            /// Release jump button quickly to make lower jumps.
            /// </summary>
            public static class vp_FPController_UpdateJumpForceWalk
            {
                public static bool JumpReset = false;

                public static void Postfix(vp_FPController __instance)
                {
                    if (!JumpReset &&
                        __instance.Player.Jump.Active &&
                        !__instance.m_Grounded &&
                        !__instance.localPlayer.inputWasJump &&
                        !__instance.localPlayer.IsSwimming() &&
                        __instance.localPlayer.PerkParkour().Level > 0 &&
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

            /// <summary>
            /// Suppress immediate jump from a grabbed ladder if the jump key was not released since the previous jump.
            /// </summary>
            public static class EntityPlayerLocal_MoveByInput
            {
                static bool wasLadderAttached = false;
                static bool suppressJump = false;

                public static void Prefix(EntityPlayerLocal __instance)
                {
                    if (__instance.movementInput.jump)
                    {
                        if (__instance.isLadderAttached && !wasLadderAttached)
                        {
                            suppressJump = true;
                        }
                        if (suppressJump)
                        {
                            __instance.movementInput.jump = false;
                        }
                    }
                    else
                    {
                        suppressJump = false;
                    }
                    wasLadderAttached = __instance.isLadderAttached;
                }
            }
        }
    }
}
