using HarmonyLib;
using static VoidGags.VoidGags.SprintJunkie;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SprintJunkie()
        {
            LogApplyingPatch(nameof(Settings.SprintJunkie));

            Harmony.Patch(AccessTools.PropertySetter(typeof(EntityAlive), nameof(EntityAlive.MovementRunning)),
                postfix: new HarmonyMethod(EntityAlive_MovementRunning.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(vp_FPController), nameof(vp_FPController.UpdateThrottleWalk)),
                prefix: new HarmonyMethod(vp_FPController_UpdateThrottleWalk.Prefix),
                postfix: new HarmonyMethod(vp_FPController_UpdateThrottleWalk.Postfix));
        }

        public static class SprintJunkie
        {
            public struct SpeedState
            {
                public float backSpeed;
                public float sideSpeed;

                public SpeedState(float backSpeed, float sideSpeed)
                {
                    this.backSpeed = backSpeed;
                    this.sideSpeed = sideSpeed;
                }
            }

            public static bool RegularRunAvailable(EntityPlayerLocal player)
            {
                return player.AttachedToEntity is null
                    && !player.IsCrouching
                    && !player.isSwimming
                    && !player.isLadderAttached
                    && !player.IsDead();
            }

            /// <summary>
            /// Allow to use running state for all directions.
            /// </summary>
            public static class EntityAlive_MovementRunning
            {
                public static void Postfix(EntityAlive __instance)
                {
                    if (__instance is EntityPlayerLocal player
                        && player.movementInput != null
                        && player.movementInput.running
                        && !player.bMovementRunning
                        && RegularRunAvailable(player))
                    {
                        if (player.movementInput.moveStrafe != 0f || player.movementInput.moveForward != 0f)
                        {
                            player.bMovementRunning = player.PerkParkour().Level > 0;
                        }
                    }
                }
            }

            /// <summary>
            /// Adjust back speed and side speed.
            /// </summary>
            public static class vp_FPController_UpdateThrottleWalk
            {
                public static void Prefix(vp_FPController __instance, ref SpeedState? __state)
                {
                    if (IsDedicatedServer) return;

                    if (__instance.localPlayer.MovementRunning && RegularRunAvailable(__instance.localPlayer))
                    {
                        if (__instance.MotorSidewaysSpeed < __instance.MotorBackwardsSpeed)
                        {
                            var parkour = __instance.localPlayer.PerkParkour();
                            if (parkour.Level > 0)
                            {
                                __state = new(__instance.MotorBackwardsSpeed, __instance.MotorSidewaysSpeed);
                                var diff = __instance.MotorBackwardsSpeed - __instance.MotorSidewaysSpeed;
                                var playerGoBackwards = __instance.localPlayer.moveDirection.z < 0f;

                                __instance.MotorSidewaysSpeed += diff * ((float)parkour.Level / parkour.ProgressionClass.MaxLevel);

                                if (playerGoBackwards)
                                {
                                    __instance.MotorBackwardsSpeed = __instance.MotorSidewaysSpeed;
                                }
                            }
                        }
                    }
                }

                public static void Postfix(vp_FPController __instance, SpeedState? __state)
                {
                    if (__state.HasValue)
                    {
                        __instance.MotorBackwardsSpeed = __state.Value.backSpeed;
                        __instance.MotorSidewaysSpeed = __state.Value.sideSpeed;
                    }
                }
            }
        }
    }
}
