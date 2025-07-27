using System;
using HarmonyLib;

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
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityVehicle_AddFuelFromInventory.AParams p) => EntityVehicle_AddFuelFromInventory.Prefix(p.__instance, p.entity))));

            Harmony.Patch(AccessTools.Method(typeof(EntityVehicle), nameof(EntityVehicle.takeFuel)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityVehicle_takeFuel.AParams p) => EntityVehicle_takeFuel.Prefix(p.__instance, p.__result, ref p.count))),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((float __result) => EntityVehicle_takeFuel.Postfix(__result))));

            Harmony.Patch(AccessTools.Method(typeof(Vehicle), nameof(Vehicle.AddFuel)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vehicle_AddFuel.AParams p) => Vehicle_AddFuel.Prefix(p.__instance, p._fuelLevel))));
        }

        private static class VehicleRefuelTimer
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
        }

        /// <summary>
        /// Keep player entity for further use and as a flag.
        /// </summary>
        public class EntityVehicle_AddFuelFromInventory
        {
            public struct AParams
            {
                public EntityVehicle __instance;
                public EntityAlive entity;
            }

            public static void Prefix(EntityVehicle __instance, EntityAlive entity)
            {
                if (IsDedicatedServer) return;

                // keep player
                VehicleRefuelTimer.PlayerEntity = entity as EntityPlayerLocal;
                VehicleRefuelTimer.DisplayVehicleFuel(__instance.vehicle);
            }
        }

        /// <summary>
        /// Limit fuel to take and keep taken value for further use.
        /// </summary>
        public class EntityVehicle_takeFuel
        {
            public struct AParams
            {
                public EntityVehicle __instance;
                public float __result;
                public int count;
            }

            public static void Prefix(EntityVehicle __instance, float __result, ref int count)
            {
                if (IsDedicatedServer) return;

                if (VehicleRefuelTimer.PlayerEntity != null)
                {
                    // limit one refuel to 500 units
                    count = Math.Min(count, 500);
                }
            }

            public static void Postfix(float __result)
            {
                if (IsDedicatedServer) return;

                // keep fuel taken
                VehicleRefuelTimer.FuelTaken = __result;
            }
        }

        /// <summary>
        /// Add timer and cycle refueling.
        /// </summary>
        public class Vehicle_AddFuel
        {
            public struct AParams
            {
                public Vehicle __instance;
                public float _fuelLevel;
            }

            public static bool Prefix(Vehicle __instance, float _fuelLevel)
            {
                if (IsDedicatedServer) return true;

                if (VehicleRefuelTimer.SkipAddFuel)
                {
                    VehicleRefuelTimer.SkipAddFuel = false;
                    return true;
                }

                if (VehicleRefuelTimer.PlayerEntity != null &&
                    VehicleRefuelTimer.FuelTaken > 0f)
                {
                    Helper.UiTimerAction(VehicleRefuelTimer.RefuelingTime, action: () =>
                    {
                        VehicleRefuelTimer.SkipAddFuel = true;
                        __instance.AddFuel(_fuelLevel);

                        if (__instance?.GetFuelPercent() < 1f && __instance.entity != null &&
                            VehicleRefuelTimer.PlayerEntity != null)
                        {
                            // add next fuel portion automatically
                            __instance.entity.AddFuelFromInventory(VehicleRefuelTimer.PlayerEntity);
                        }
                        else
                        {
                            // stop refuel
                            VehicleRefuelTimer.Clear();
                            VehicleRefuelTimer.DisplayVehicleFuel(__instance);
                        }
                    }, cancelAction: () =>
                    {
                        // give gas back to player
                        if (VehicleRefuelTimer.PlayerEntity != null && VehicleRefuelTimer.FuelTaken > 0f)
                        {
                            var gas = new ItemStack(new ItemValue(ItemClass.GetItem("ammoGasCan").type), (int)VehicleRefuelTimer.FuelTaken);
                            VehicleRefuelTimer.PlayerEntity.GiveItem(gas);
                            VehicleRefuelTimer.Clear();
                        }
                    });
                    return false;
                }

                // stop refuel
                VehicleRefuelTimer.Clear();
                VehicleRefuelTimer.DisplayVehicleFuel(__instance);
                return true;
            }
        }
    }
}
