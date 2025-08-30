using System;
using HarmonyLib;
using static VoidGags.VoidGags.VehicleRefuelTimer;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_VehicleRefuelTimer()
        {
            LogApplyingPatch(nameof(Settings.VehicleRefuelTimer));

            Harmony.Patch(AccessTools.Method(typeof(EntityVehicle), nameof(EntityVehicle.AddFuelFromInventory)),
                prefix: new HarmonyMethod(EntityVehicle_AddFuelFromInventory.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(EntityVehicle), nameof(EntityVehicle.takeFuel)),
                prefix: new HarmonyMethod(EntityVehicle_takeFuel.Prefix),
                postfix: new HarmonyMethod(EntityVehicle_takeFuel.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(Vehicle), nameof(Vehicle.AddFuel)),
                prefix: new HarmonyMethod(Vehicle_AddFuel.Prefix));
        }

        public static class VehicleRefuelTimer
        {
            public const float RefuelingTime = 2f;
            public static EntityPlayerLocal PlayerEntity = null;
            public static float FuelTaken = 0f;
            public static bool SkipAddFuel = false;

            public static void Clear()
            {
                PlayerEntity = null;
                FuelTaken = 0f;
            }

            public static void DisplayVehicleFuel(Vehicle vehicle, float duration = 1f)
            {
                var fuelPercent = vehicle is null ? 0f : vehicle.GetFuelPercent() * 100f;
                GameManager.ShowSubtitle(LocalPlayerUI.primaryUI.xui, speaker: "",
                    $"{Localization.Get("xuiFuel")}: {fuelPercent:0}%                               ",
                    duration, centerAlign: true);
            }

            /// <summary>
            /// Keep player entity for further use and as a flag.
            /// </summary>
            public static class EntityVehicle_AddFuelFromInventory
            {
                public static void Prefix(EntityVehicle __instance, EntityAlive entity)
                {
                    if (IsDedicatedServer) return;

                    // keep player
                    PlayerEntity = entity as EntityPlayerLocal;
                    DisplayVehicleFuel(__instance.vehicle);
                }
            }

            /// <summary>
            /// Limit fuel to take and keep taken value for further use.
            /// </summary>
            public static class EntityVehicle_takeFuel
            {
                public static void Prefix(EntityVehicle __instance, float __result, ref int count)
                {
                    if (IsDedicatedServer) return;

                    if (PlayerEntity != null)
                    {
                        // limit one refuel to 500 units
                        count = Math.Min(count, 500);
                    }
                }

                public static void Postfix(float __result)
                {
                    if (IsDedicatedServer) return;

                    // keep fuel taken
                    FuelTaken = __result;
                }
            }

            /// <summary>
            /// Add timer and cycle refueling.
            /// </summary>
            public static class Vehicle_AddFuel
            {
                public static bool Prefix(Vehicle __instance, float _fuelLevel)
                {
                    if (IsDedicatedServer) return true;

                    if (SkipAddFuel)
                    {
                        SkipAddFuel = false;
                        return true;
                    }

                    if (PlayerEntity != null &&
                        FuelTaken > 0f)
                    {
                        Helper.UiTimerAction(RefuelingTime, action: () =>
                        {
                            SkipAddFuel = true;
                            __instance.AddFuel(_fuelLevel);

                            if (__instance?.GetFuelPercent() < 1f && __instance.entity != null &&
                                PlayerEntity != null)
                            {
                                // add next fuel portion automatically
                                __instance.entity.AddFuelFromInventory(PlayerEntity);
                            }
                            else
                            {
                                // stop refuel
                                Clear();
                                DisplayVehicleFuel(__instance);
                            }
                        }, cancelAction: () =>
                        {
                            // give gas back to player
                            if (PlayerEntity != null && FuelTaken > 0f)
                            {
                                var gas = new ItemStack(new ItemValue(ItemClass.GetItem("ammoGasCan").type), (int)FuelTaken);
                                PlayerEntity.GiveItem(gas);
                                Clear();
                            }
                        });
                        return false;
                    }

                    // stop refuel
                    Clear();
                    DisplayVehicleFuel(__instance);
                    return true;
                }
            }
        }
    }
}
