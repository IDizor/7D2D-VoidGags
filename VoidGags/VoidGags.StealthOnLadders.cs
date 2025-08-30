using HarmonyLib;
using static VoidGags.VoidGags.StealthOnLadders;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_StealthOnLadders()
        {
            LogApplyingPatch(nameof(Settings.StealthOnLadders));

            Harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.MoveByInput)),
                prefix: new HarmonyMethod(EntityPlayerLocal_MoveByInput.Prefix),
                postfix: new HarmonyMethod(EntityPlayerLocal_MoveByInput.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.GetSpeedModifier)),
                postfix: new HarmonyMethod(EntityPlayerLocal_GetSpeedModifier.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(PlayerStealth), nameof(PlayerStealth.CalcVolume)),
                postfix: new HarmonyMethod(PlayerStealth_CalcVolume.Postfix));
        }

        public static class StealthOnLadders
        {
            /// <summary>
            /// Keep crouching state on the ladder.
            /// </summary>
            public static class EntityPlayerLocal_MoveByInput
            {
                static bool isCrouchingJump;
                static bool isCrouchingLocked;

                public static void Prefix(EntityPlayerLocal __instance, out bool __state)
                {
                    __state = __instance.isLadderAttached; // remember the ladder attached state
                    isCrouchingJump = __instance.IsCrouching && (__instance.Jumping || __instance.movementInput.jump);
                    isCrouchingLocked = __instance.CrouchingLocked;

                    if (__instance.AttachedToEntity == null && !__instance.movementInput.jump)
                    {
                        // set isLadderAttached to false during the original method execution
                        __instance.isLadderAttached = false;
                    }
                }

                public static void Postfix(EntityPlayerLocal __instance, bool __state)
                {
                    if (__instance.AttachedToEntity == null)
                    {
                        // restore the ladder attached state
                        __instance.isLadderAttached = __state;
                    }

                    if (isCrouchingJump)
                    {
                        __instance.CrouchingLocked = isCrouchingLocked;
                        __instance.Crouching = true;
                    }
                }
            }

            /// <summary>
            /// Makes crouching on a ladder slower.
            /// </summary>
            public static class EntityPlayerLocal_GetSpeedModifier
            {
                public static void Postfix(EntityPlayerLocal __instance, ref float __result, bool ___isLadderAttached)
                {
                    if (__instance.IsCrouching && ___isLadderAttached)
                    {
                        __result *= 0.5f;
                    }
                }
            }

            /// <summary>
            /// Makes less noice when crouching on a ladder.
            /// </summary>
            public static class PlayerStealth_CalcVolume
            {
                public static void Postfix(ref float __result, EntityPlayer ___player)
                {
                    if (__result > 0 && ___player.IsCrouching && ___player is EntityPlayerLocal player)
                    {
                        if (player.isLadderAttached)
                        {
                            __result *= 0.25f;
                        }
                    }
                }
            }
        }
    }
}
