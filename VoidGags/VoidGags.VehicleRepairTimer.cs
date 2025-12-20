using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Audio;
using HarmonyLib;
using UnityEngine;
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
                reverse: new HarmonyMethod(Vehicle_RepairParts.RepairParts),
                prefix: new HarmonyMethod(Vehicle_RepairParts.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(Manager), nameof(Manager.PlayInsidePlayerHead), [typeof(string), typeof(int), typeof(float), typeof(bool), typeof(bool)]),
                prefix: new HarmonyMethod(Manager_PlayInsidePlayerHead.Prefix));
        }

        public static class VehicleRepairTimer
        {
            public const float RepairTime = 5f;
            public static XUi Ui = null;
            public static Vehicle Vehicle = null;
            public static bool IsRepairing = false;

            public struct SoundData(string name, float duration)
            {
                public string SoundName = name;
                public float Duration = duration;
            }

            public static SoundData[] RepairSounds =
            [
                new("twitch_repair_all", 2.5f), // for big vehicles only
                new("wrench_harvest", 1.1f),
                new("wrench_harvest", 1.1f),
                new("craft_repair_item", 1.2f),
                new("craft_repair_item", 1.2f),
                new("wrench_place", 0.5f),
                new("repairkits_place", 0.7f),
                new("twitch_repair", 2.0f),
                new("twitch_repair", 2.0f),
                new("ratchet", 1.0f),
                new("ratchet_grab", 0.5f),
                new("wrench_grab", 0.7f),
                new("metalhitmetal", 0.8f),
                new("metalhitmetal", 0.8f),
                new("metalhitmetal", 0.8f),
                new("metalhitmetal", 0.8f),
            ];

            public static void Clear()
            {
                Ui = null;
                Vehicle = null;
                IsRepairing = false;
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
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void RepairParts(Vehicle __instance, int _add, float _percent)
                {
                    throw new NotImplementedException();
                }

                public static bool Prefix(Vehicle __instance, int _add, float _percent)
                {
                    if (IsDedicatedServer) return true;

                    if (Ui != null)
                    {
                        if (!IsRepairing)
                        {
                            IsRepairing = true;
                            var isBigVehicle = RoadRash.IsBigVehicle(__instance);
                            GameManager.Instance.StartCoroutine(PlayRepairSounds(__instance.entity.position, isBigVehicle));
                        }

                        Helper.UiTimerAction(RepairTime, action: () =>
                        {
                            if (__instance != null && Ui != null)
                            {
                                RepairParts(__instance, _add, _percent);

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
                            else
                            {
                                Clear();
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

            /// <summary>
            /// Skip default simple repair sound.
            /// </summary>
            public static class Manager_PlayInsidePlayerHead
            {
                public static bool Prefix(string soundGroupName)
                {
                    return !IsRepairing || !soundGroupName.Same("craft_complete_item");
                }
            }

            public static IEnumerator PlayRepairSounds(Vector3 position, bool isBigVehicle)
            {
                var random = GameManager.Instance.World.GetGameRandom();
                while (IsRepairing)
                {
                    var i = random.Next(isBigVehicle ? 0 : 1, RepairSounds.Length);
                    Manager.Play(position, RepairSounds[i].SoundName);
                    yield return new WaitForSeconds(RepairSounds[i].Duration);
                }
            }
        }
    }
}
