using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_VehicleRepairTimer()
        {
            LogApplyingPatch(nameof(Settings.VehicleRepairTimer));

            Harmony.Patch(AccessTools.Method(typeof(XUiM_Vehicle), nameof(XUiM_Vehicle.RepairVehicle)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiM_Vehicle_RepairVehicle.AParams p) => XUiM_Vehicle_RepairVehicle.Prefix(p._xui, p.vehicle))));

            Harmony.Patch(AccessTools.Method(typeof(Vehicle), nameof(Vehicle.RepairParts), [typeof(int), typeof(float)]),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vehicle_RepairParts.AParams p) => Vehicle_RepairParts.Prefix(p.__instance, p._add, p._percent))));
        }

        private static class VehicleRepairTimer
        {
            public const float RepairTime = 5f;
            public static XUi Ui = null;
            public static Vehicle Vehicle = null;
            public static bool SkipRepairParts = false;

            public static void Clear()
            {
                Ui = null;
                Vehicle = null;
            }

            public static void DisplayVehicleDurability(Vehicle vehicle, float duration = 5f)
            {
                var max = vehicle.GetMaxHealth();
                var health = vehicle.entity.Health;
                GameManager.ShowSubtitle(LocalPlayerUI.primaryUI.xui, speaker: "",
                    $"{Localization.Get("xuiDurability")}: {health} / {max}                               ",
                    duration, centerAlign: true);
            }
        }

        /// <summary>
        /// Keep parameters for further use and as a flag.
        /// </summary>
        public class XUiM_Vehicle_RepairVehicle
        {
            public struct AParams
            {
                public XUi _xui;
                public Vehicle vehicle;
            }

            public static void Prefix(XUi _xui, Vehicle vehicle)
            {
                if (IsDedicatedServer) return;

                // keep parameters
                VehicleRepairTimer.Ui = _xui;
                VehicleRepairTimer.Vehicle = vehicle;
                VehicleRepairTimer.DisplayVehicleDurability(vehicle ?? _xui.vehicle.GetVehicle());
            }
        }

        /// <summary>
        /// Add timer and cycle repairing.
        /// </summary>
        public class Vehicle_RepairParts
        {
            public struct AParams
            {
                public Vehicle __instance;
                public int _add;
                public float _percent;
            }

            public static bool Prefix(Vehicle __instance, int _add, float _percent)
            {
                if (IsDedicatedServer) return true;

                if (VehicleRepairTimer.SkipRepairParts)
                {
                    VehicleRepairTimer.SkipRepairParts = false;
                    return true;
                }

                if (VehicleRepairTimer.Ui != null)
                {
                    Helper.UiTimerAction(VehicleRepairTimer.RepairTime, action: () =>
                    {
                        if (__instance != null && VehicleRepairTimer.Ui != null)
                        {
                            VehicleRepairTimer.SkipRepairParts = true;
                            __instance.RepairParts(_add, _percent);

                            int repairAmountNeeded = __instance.GetRepairAmountNeeded();
                            if (repairAmountNeeded > 0)
                            {
                                // do one more repair automatically
                                XUiM_Vehicle.RepairVehicle(VehicleRepairTimer.Ui, VehicleRepairTimer.Vehicle);
                            }
                            else
                            {
                                // stop repair
                                VehicleRepairTimer.Clear();
                                VehicleRepairTimer.DisplayVehicleDurability(__instance);
                            }
                        }
                    }, cancelAction: () =>
                    {
                        // give repair kit back to player
                        Helper.PlayerLocal.GiveItem(new ItemStack(new ItemValue(ItemClass.GetItem("resourceRepairKit").type), 1));
                        VehicleRepairTimer.Clear();
                    });
                    return false;
                }

                // stop repair
                VehicleRepairTimer.Clear();
                VehicleRepairTimer.DisplayVehicleDurability(__instance);
                return true;
            }
        }
    }
}
