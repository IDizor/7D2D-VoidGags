using System.Collections.Generic;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using VoidGags.Types;
using static VoidGags.VoidGags.RoadRash;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_RoadRash()
        {
            LogApplyingPatch(nameof(Settings.RoadRash));
            
            const int paramsCount = 9;

            if (Settings.RoadRash_Drive.Length != paramsCount)
            {
                LogException($"Invalid '{nameof(Settings.RoadRash_Drive)}' array length. It should have {paramsCount} elements.");
                return;
            }

            if (Settings.RoadRash_Walk.Length != paramsCount)
            {
                LogException($"Invalid '{nameof(Settings.RoadRash_Walk)}' array length. It should have {paramsCount} elements.");
                return;
            }

            DriveAsphalt = Mathf.Clamp(Settings.RoadRash_Drive[0], MinLimit, MaxLimit);
            DriveGravel = Mathf.Clamp(Settings.RoadRash_Drive[1], MinLimit, MaxLimit);
            DriveWooden = Mathf.Clamp(Settings.RoadRash_Drive[2], MinLimit, MaxLimit);
            DriveGround = Mathf.Clamp(Settings.RoadRash_Drive[3], MinLimit, MaxLimit);
            DriveSand = Mathf.Clamp(Settings.RoadRash_Drive[4], MinLimit, MaxLimit);
            DriveSnow = Mathf.Clamp(Settings.RoadRash_Drive[5], MinLimit, MaxLimit);
            DriveDestroyed = Mathf.Clamp(Settings.RoadRash_Drive[6], MinLimit, MaxLimit);
            DriveBushes = Mathf.Clamp(Settings.RoadRash_Drive[7], MinLimit, MaxLimit);
            DriveOther = Mathf.Clamp(Settings.RoadRash_Drive[8], MinLimit, MaxLimit);

            WalkAsphalt = Mathf.Clamp(Settings.RoadRash_Walk[0], MinLimit, MaxLimit);
            WalkGravel = Mathf.Clamp(Settings.RoadRash_Walk[1], MinLimit, MaxLimit);
            WalkWooden = Mathf.Clamp(Settings.RoadRash_Walk[2], MinLimit, MaxLimit);
            WalkGround = Mathf.Clamp(Settings.RoadRash_Walk[3], MinLimit, MaxLimit);
            WalkSand = Mathf.Clamp(Settings.RoadRash_Walk[4], MinLimit, MaxLimit);
            WalkSnow = Mathf.Clamp(Settings.RoadRash_Walk[5], MinLimit, MaxLimit);
            WalkDestroyed = Mathf.Clamp(Settings.RoadRash_Walk[6], MinLimit, MaxLimit);
            WalkBushes = Mathf.Clamp(Settings.RoadRash_Walk[7], MinLimit, MaxLimit);
            WalkOther = Mathf.Clamp(Settings.RoadRash_Walk[8], MinLimit, MaxLimit);

            if (DriveAsphalt != Settings.RoadRash_Drive[0]) LogClampWarning(nameof(DriveAsphalt));
            if (DriveGravel != Settings.RoadRash_Drive[1]) LogClampWarning(nameof(DriveGravel));
            if (DriveWooden != Settings.RoadRash_Drive[2]) LogClampWarning(nameof(DriveWooden));
            if (DriveGround != Settings.RoadRash_Drive[3]) LogClampWarning(nameof(DriveGround));
            if (DriveSand != Settings.RoadRash_Drive[4]) LogClampWarning(nameof(DriveSand));
            if (DriveSnow != Settings.RoadRash_Drive[5]) LogClampWarning(nameof(DriveSnow));
            if (DriveDestroyed != Settings.RoadRash_Drive[6]) LogClampWarning(nameof(DriveDestroyed));
            if (DriveBushes != Settings.RoadRash_Drive[7]) LogClampWarning(nameof(DriveBushes));
            if (DriveOther != Settings.RoadRash_Drive[8]) LogClampWarning(nameof(DriveOther));

            if (WalkAsphalt != Settings.RoadRash_Walk[0]) LogClampWarning(nameof(WalkAsphalt));
            if (WalkGravel != Settings.RoadRash_Walk[1]) LogClampWarning(nameof(WalkGravel));
            if (WalkWooden != Settings.RoadRash_Walk[2]) LogClampWarning(nameof(WalkWooden));
            if (WalkGround != Settings.RoadRash_Walk[3]) LogClampWarning(nameof(WalkGround));
            if (WalkSand != Settings.RoadRash_Walk[4]) LogClampWarning(nameof(WalkSand));
            if (WalkSnow != Settings.RoadRash_Walk[5]) LogClampWarning(nameof(WalkSnow));
            if (WalkDestroyed != Settings.RoadRash_Walk[6]) LogClampWarning(nameof(WalkDestroyed));
            if (WalkBushes != Settings.RoadRash_Walk[7]) LogClampWarning(nameof(WalkBushes));
            if (WalkOther != Settings.RoadRash_Walk[8]) LogClampWarning(nameof(WalkOther));

            static void LogClampWarning(string name) => LogWarning($"{name} speed modifier is clamped to fit valid range [{MinLimit:0.00} to {MaxLimit:0.00}].");

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.Update)),
                prefix: new HarmonyMethod(EntityAlive_Update.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(Vehicle), nameof(Vehicle.CalcEffects)),
                postfix: new HarmonyMethod(Vehicle_CalcEffects.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.GetSpeedModifier)),
                postfix: new HarmonyMethod(EntityPlayerLocal_GetSpeedModifier.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.GetSpeedModifier)),
                postfix: new HarmonyMethod(EntityAlive_GetSpeedModifier.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityVehicle), nameof(EntityVehicle.SetWheelsForces)),
                postfix: new HarmonyMethod(EntityVehicle_SetWheelsForces.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.Detach)),
                prefix: new HarmonyMethod(EntityPlayerLocal_Detach.Prefix),
                postfix: new HarmonyMethod(EntityPlayerLocal_Detach.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityVehicle), nameof(EntityVehicle.PhysicsFixedUpdate)),
                prefix: new HarmonyMethod(EntityVehicle_PhysicsFixedUpdate.Prefix),
                postfix: new HarmonyMethod(EntityVehicle_PhysicsFixedUpdate.Postfix));
        }

        public static class RoadRash
        {
            public static float MaxLimit = 2f;
            public static float MinLimit = 0.01f;

            public static float DriveAsphalt = 1f;
            public static float DriveGravel = 1f;
            public static float DriveWooden = 1f;
            public static float DriveGround = 1f;
            public static float DriveSand = 1f;
            public static float DriveSnow = 1f;
            public static float DriveDestroyed = 1f;
            public static float DriveBushes = 1f;
            public static float DriveOther = 1f;

            public static float WalkAsphalt = 1f;
            public static float WalkGravel = 1f;
            public static float WalkWooden = 1f;
            public static float WalkGround = 1f;
            public static float WalkSand = 1f;
            public static float WalkSnow = 1f;
            public static float WalkDestroyed = 1f;
            public static float WalkBushes = 1f;
            public static float WalkOther = 1f;

            public static Dictionary<int, float> EntityModifiers = [];
            public static Dictionary<int, float> VehicleInclines = [];

            public static bool IsBushBlock(Block block)
            {
                var isBush = !block.IsDecoration && !block.IsCollideMovement && block.IsPlant();
                return isBush || block.Is("plantedBlueberry3") || block.Is("plantedYucca3");
            }

            public static bool IsBigVehicle(Vehicle vehicle)
            {
                return vehicle.FindPart("headlight") is VPHeadlight vpHeadlight && (bool)vpHeadlight.GetTransform();
            }

            public static float NerfSlowness(float modifier)
            {
                if (modifier < 1f)
                {
                    return 1f - ((1f - modifier) / 2f);
                }
                return modifier;
            }

            public static void ManualUpdateEffectVelocityMaxPer(Vehicle vehicle, float modifier) // Based on Vehicle.CalcEffects()
            {
                var driver = vehicle.entity.AttachedMainEntity as EntityAlive;
                vehicle.EffectVelocityMaxPer = EffectManager.GetValue(PassiveEffects.VehicleVelocityMaxPer, vehicle.itemValue, 1f, driver, null, vehicle.entity.EntityTags);
                vehicle.EffectVelocityMaxPer *= modifier;
            }

            public static void SlowDownVehicle(Vehicle vehicle)
            {
                var maxVelocity = vehicle.EffectVelocityMaxPer * (vehicle.IsTurbo
                    ? vehicle.VelocityMaxTurboForward
                    : vehicle.VelocityMaxForward);

                if (vehicle.CurrentVelocity.magnitude > maxVelocity)
                {
                    vehicle.CurrentVelocity *= 0.85f;
                    vehicle.entity.vehicleRB.velocity *= 0.85f;
                    vehicle.entity.lastRemoteData.Velocity = vehicle.CurrentVelocity;
                    vehicle.entity.currentRemoteData.Velocity = vehicle.CurrentVelocity;
                }
            }

            /// <summary>
            /// Calc max speed modifier based on current surface.
            /// </summary>
            public static class EntityAlive_Update
            {
                private static DelayStorage<int> EntityDelays = new(0.2f);

                public static void Prefix(EntityAlive __instance)
                {
                    if (!EntityDelays.Check(__instance.entityId)) return;
                    if (__instance.blockValueStandingOn.isair) return;

                    var world = __instance.world;
                    var standingOn = __instance.blockValueStandingOn.Block;
                    var above = world.GetBlock(__instance.blockPosStandingOn + Vector3i.up).Block;
                    var below = world.GetBlock(__instance.blockPosStandingOn + Vector3i.down).Block;
                    if (standingOn != null && above != null && below != null)
                    {
                        //if (__instance is EntityPlayerLocal)
                        //{
                        //    LogModWarning($"Above is {above.blockName}. IsDecoration {above.IsDecoration}, IsCollideMovement {above.IsCollideMovement}, IsPlant {above.IsPlant()},");
                        //    LogModWarning($"Surface is {standingOn.blockName}. IsDecoration {standingOn.IsDecoration}, IsCollideMovement {standingOn.IsCollideMovement}, IsPlant {standingOn.IsPlant()},");
                        //    LogModWarning($"Below is {below.blockName}. IsDecoration {below.IsDecoration}, IsCollideMovement {below.IsCollideMovement}, IsPlant {below.IsPlant()},");
                        //}

                        var vehicle = __instance.AttachedToEntity as EntityVehicle;
                        var isVehicle = vehicle != null;
                        var bushesModifier = isVehicle ? DriveBushes : WalkBushes;
                        var isJump = __instance.bJumping;
                        var isBush = bushesModifier != 1f && !isJump && (IsBushBlock(standingOn) || IsBushBlock(above));
                        var isBigVehicle = isVehicle && IsBigVehicle(vehicle.vehicle);
                        var entityId = isVehicle ? vehicle.entityId : __instance.entityId;

                        var surface = standingOn;
                        if (surface.IsPlant() && !surface.IsCollideMovement)
                        {
                            surface = below;
                        }

                        var isGround = false;
                        var isWood = false;
                        var isSand = false;
                        var isSnow = false;
                        var isDestroyed = false;
                        var isGravel = false;
                        var isAsphalt = false;

                        if (surface.Is("terrDirt") || surface.Is("terrForestGround") || surface.Is("terrTopSoil") || surface.Is("terrBurntForestGround")) isGround = true;
                        else if (surface.Is("woodShapes") || surface.Is("frameShapes")) isWood = true;
                        else if (surface.Is("terrSand") || surface.Is("terrDesertGround")) isSand = true;
                        else if (surface.Is("terrSnow")) isSnow = true;
                        else if (surface.Is("terrDestroyed")) isDestroyed = true;
                        else if (surface.Is("terrGravel")) isGravel = true;
                        else if (surface.Is("terrAsphalt") || surface.Is("terrConcrete") || surface.Is("concreteShapes")) isAsphalt = true;

                        var prevModifier = EntityModifiers.ContainsKey(entityId) ? EntityModifiers[entityId] : 1f;
                        var modifier = isVehicle ? DriveOther : WalkOther;
                        if (isGround) modifier = isVehicle ? DriveGround : WalkGround;
                        else if (isWood) modifier = isVehicle ? DriveWooden : WalkWooden;
                        else if (isSand) modifier = isBigVehicle ? NerfSlowness(DriveSand) : isVehicle ? DriveSand : WalkSand;
                        else if (isSnow) modifier = isBigVehicle ? NerfSlowness(DriveSnow) : isVehicle ? DriveSnow : WalkSnow;
                        else if (isDestroyed) modifier = isVehicle ? DriveDestroyed : WalkDestroyed;
                        else if (isGravel) modifier = isVehicle ? DriveGravel : WalkGravel;
                        else if (isAsphalt) modifier = isVehicle ? DriveAsphalt : WalkAsphalt;

                        // check bushes if walking or driving small vehicle
                        if (!isBigVehicle && !isJump && bushesModifier != 1f)
                        {
                            var movingSpeed = isVehicle
                                ? vehicle.vehicle.CurrentVelocity.magnitude
                                : __instance.GetMovingSpeed();
                            if (!isBush && movingSpeed > 0.5f)
                            {
                                var moveDirection = __instance.GetMovingDirection();
                                var nextBlockPos = Helper.GetBlockPosInDirection(__instance.position + Vector3.up, moveDirection, 0.8f);
                                if (nextBlockPos.x != __instance.blockPosStandingOn.x ||
                                    nextBlockPos.z != __instance.blockPosStandingOn.z)
                                {
                                    isBush = IsBushBlock(world.GetBlock(nextBlockPos).Block);
                                    isBush |= IsBushBlock(world.GetBlock(nextBlockPos + Vector3i.down).Block);
                                }
                            }
                            if (isBush)
                            {
                                EntityModifiers[entityId] = Mathf.Clamp(modifier * bushesModifier, MinLimit, MaxLimit);
                                if (isVehicle) // additional code to force slowdown for vehicle
                                {
                                    ManualUpdateEffectVelocityMaxPer(vehicle.vehicle, EntityModifiers[entityId]);
                                    var maxBushesSpeed = vehicle.vehicle.VelocityMaxForward * DriveBushes;
                                    if (movingSpeed > maxBushesSpeed)
                                    {
                                        var limiter = maxBushesSpeed / movingSpeed;
                                        vehicle.vehicle.CurrentVelocity *= limiter;
                                        vehicle.vehicleRB.velocity *= limiter;
                                        vehicle.lastRemoteData.Velocity = vehicle.vehicle.CurrentVelocity;
                                        vehicle.currentRemoteData.Velocity = vehicle.vehicle.CurrentVelocity;
                                    }
                                }
                                return;
                            }
                        }

                        // apply forward incline for vehicle
                        if (isVehicle && !isBush && VehicleInclines.ContainsKey(entityId))
                        {
                            var incline = VehicleInclines[entityId];
                            if (incline < 0f && vehicle.vehicle.HasEnginePart())
                            {
                                incline /= 3f; // motorized vehicles goes uphill more easily
                            }
                            var inclineModifier = Mathf.Max(0.2f, (incline * 0.1f) + 1f);
                            modifier *= inclineModifier;
                        }

                        // apply modifier
                        if (modifier > prevModifier)
                        {
                            EntityModifiers[entityId] = Mathf.Clamp(prevModifier + 0.2f, MinLimit, modifier);
                            if (isVehicle) ManualUpdateEffectVelocityMaxPer(vehicle.vehicle, EntityModifiers[entityId]);
                        }
                        else if (modifier < prevModifier)
                        {
                            EntityModifiers[entityId] = Mathf.Clamp(prevModifier - 0.2f, modifier, MaxLimit);
                            if (isVehicle)
                            {
                                ManualUpdateEffectVelocityMaxPer(vehicle.vehicle, EntityModifiers[entityId]);
                                SlowDownVehicle(vehicle.vehicle);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Apply modifier to vehicle max speed.
            /// </summary>
            public static class Vehicle_CalcEffects
            {
                public static void Postfix(Vehicle __instance)
                {
                    if (__instance.entity != null && EntityModifiers.ContainsKey(__instance.entity.entityId))
                    {
                        __instance.EffectVelocityMaxPer *= EntityModifiers[__instance.entity.entityId];
                    }
                }
            }

            /// <summary>
            /// Apply modifier to player max speed.
            /// </summary>
            public static class EntityPlayerLocal_GetSpeedModifier
            {
                public static void Postfix(EntityPlayerLocal __instance, ref float __result)
                {
                    if (EntityModifiers.ContainsKey(__instance.entityId))
                    {
                        __result *= EntityModifiers[__instance.entityId];
                    }
                }
            }

            /// <summary>
            /// Apply modifier to others max speed.
            /// </summary>
            public static class EntityAlive_GetSpeedModifier
            {
                public static void Postfix(EntityAlive __instance, ref float __result)
                {
                    if (EntityModifiers.ContainsKey(__instance.entityId))
                    {
                        __result *= EntityModifiers[__instance.entityId];
                    }
                }
            }

            /// <summary>
            /// Update vehicle wheels side friction.
            /// </summary>
            public static class EntityVehicle_SetWheelsForces
            {
                public static void Postfix(EntityVehicle __instance)
                {
                    if (EntityModifiers.TryGetValue(__instance.entityId, out float modifier))
                    {
                        if (modifier > 1f)
                        {
                            for (int i = 0; i < __instance.wheels.Length; i++)
                            {
                                var wheel = __instance.wheels[i];
                                wheel.sideFriction.stiffness *= modifier;
                                wheel.sideFriction.stiffness += 1f;
                                wheel.wheelC.sidewaysFriction = wheel.sideFriction;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Slow down wheels on detach vehicle.
            /// </summary>
            public static class EntityPlayerLocal_Detach
            {
                public static void Prefix(EntityPlayerLocal __instance, out EntityVehicle __state)
                {
                    __state = __instance.AttachedToEntity as EntityVehicle;
                }

                public static void Postfix(EntityVehicle __state)
                {
                    if (__state != null)
                    {
                        foreach (var wheel in __state.wheels)
                        {
                            wheel.wheelC.rotationSpeed *= 0.1f;
                        }
                    }
                }
            }

            /// <summary>
            /// Improve momentum and respect vehicle forward incline.
            /// </summary>
            public static class EntityVehicle_PhysicsFixedUpdate
            {
                public struct VehicleCache(float velScale, float angVelScale, float velocityMaxForward)
                {
                    public float VelScale = velScale;
                    public float AngVelScale = angVelScale;
                    public float VelocityMaxForward = velocityMaxForward;
                };

                public static void Prefix(EntityVehicle __instance, out VehicleCache __state)
                {
                    __state = new(__instance.vehicle.AirDragVelScale, __instance.vehicle.AirDragAngVelScale, __instance.vehicle.VelocityMaxForward);
                    if (__instance.wheels.All(w => !w.isGrounded)) return;

                    var incline = __instance.transform.eulerAngles.x.Normalize180();
                    if (__instance.vehicle.CurrentForwardVelocity < 0) incline *= -1;
                    incline = Mathf.Clamp(incline, -45f, 45f); // clamp to 45° deviation
                    //if (__instance.HasDriver) LogWarningNoSpam($"Incline = {incline:0.0000}", 0.5f, true);
                    VehicleInclines[__instance.EntityId] = incline; // positive incline° means vehicle goes downhill
                    var inclineModifier = incline * 0.0004f;
                    __instance.vehicle.AirDragVelScale = 0.99998f + inclineModifier;
                    __instance.vehicle.AirDragAngVelScale = 1f + inclineModifier;

                    // update VelocityMaxForward
                    if (__instance.vehicle.CurrentForwardVelocity > 0.01f && __instance.vehicle.VelocityMaxForward < __instance.vehicle.VelocityMaxTurboForward)
                    {
                        if (__instance.movementInput?.moveForward == 0f) // if move forward button not pressed
                        {
                            __instance.vehicle.VelocityMaxForward = __instance.vehicle.VelocityMaxTurboForward;
                        }
                        else
                        {
                            __instance.vehicle.VelocityMaxForward = Mathf.Lerp(
                                __instance.vehicle.VelocityMaxForward,
                                __instance.vehicle.VelocityMaxTurboForward,
                                Mathf.Clamp(incline / 2f, 0f, 1f)); // 0° is MaxForward, 2+° is MaxTurboForward
                        }
                    }
                }

                public static void Postfix(EntityVehicle __instance, VehicleCache __state)
                {
                    __instance.vehicle.AirDragVelScale = __state.VelScale;
                    __instance.vehicle.AirDragAngVelScale = __state.AngVelScale;
                    __instance.vehicle.VelocityMaxForward = __state.VelocityMaxForward;
                }
            }
        }
    }
}
