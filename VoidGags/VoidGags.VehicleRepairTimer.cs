using HarmonyLib;
using static VoidGags.VoidGags.VehicleRepairTimer;

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
                prefix: new HarmonyMethod(XUiM_Vehicle_RepairVehicle.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(Vehicle), nameof(Vehicle.RepairParts), [typeof(int), typeof(float)]),
                prefix: new HarmonyMethod(Vehicle_RepairParts.Prefix));
        }

        public static class VehicleRepairTimer
        {
            public const float RepairTime = 5f;
            public static XUi Ui = null;
            public static Vehicle Vehicle = null;
            public static bool SkipRepairPartsPatch = false;

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

            /// <summary>
            /// Keep parameters for further use and as a flag.
            /// </summary>
            public static class XUiM_Vehicle_RepairVehicle
            {
                public static void Prefix(XUi _xui, Vehicle vehicle)
                {
                    if (IsDedicatedServer) return;

                    // keep parameters
                    Ui = _xui;
                    Vehicle = vehicle;
                    DisplayVehicleDurability(vehicle ?? _xui.vehicle.GetVehicle());
                }
            }

            /// <summary>
            /// Add timer and cycle repairing.
            /// </summary>
            public static class Vehicle_RepairParts
            {
                public static bool Prefix(Vehicle __instance, int _add, float _percent)
                {
                    if (IsDedicatedServer) return true;

                    if (SkipRepairPartsPatch)
                    {
                        SkipRepairPartsPatch = false;
                        return true;
                    }

                    if (Ui != null)
                    {
                        Helper.UiTimerAction(RepairTime, action: () =>
                        {
                            if (__instance != null && Ui != null)
                            {
                                SkipRepairPartsPatch = true;
                                __instance.RepairParts(_add, _percent);

                                int repairAmountNeeded = __instance.GetRepairAmountNeeded();
                                if (repairAmountNeeded > 0)
                                {
                                    // do one more repair automatically
                                    XUiM_Vehicle.RepairVehicle(Ui, Vehicle);
                                }
                                else
                                {
                                    // stop repair
                                    Clear();
                                    DisplayVehicleDurability(__instance);
                                }
                            }
                        }, cancelAction: () =>
                        {
                            // give repair kit back to player
                            Helper.PlayerLocal.GiveItem(new ItemStack(new ItemValue(ItemClass.GetItem("resourceRepairKit").type), 1));
                            Clear();
                        });
                        return false;
                    }

                    // stop repair
                    Clear();
                    DisplayVehicleDurability(__instance);
                    return true;
                }
            }
        }
    }
}
