using HarmonyLib;
using static VoidGags.VoidGags.SpeedIndicator;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SpeedIndicator()
        {
            LogApplyingPatch(nameof(Settings.SpeedIndicator));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_CompassWindow), nameof(XUiC_CompassWindow.GetBindingValueInternal)),
                prefix: new HarmonyMethod(XUiC_CompassWindow_GetBindingValueInternal.Prefix));

            UseXmlPatches(nameof(Settings.SpeedIndicator));
        }

        public static class SpeedIndicator
        {
            /// <summary>
            /// Display moving speed (m/s) below the compass.
            /// </summary>
            public static class XUiC_CompassWindow_GetBindingValueInternal
            {
                public static bool Prefix(XUiC_CompassWindow __instance, ref string value, string bindingName, ref bool __result)
                {
                    if (bindingName == "movingspeed")
                    {
                        value = "";
                        if (__instance.localPlayer != null)
                        {
                            var vehicle = __instance.localPlayer.AttachedToEntity as EntityVehicle;
                            var movingSpeed = vehicle == null
                                ? __instance.localPlayer.GetMovingSpeed()
                                : vehicle.vehicle.CurrentVelocity.magnitude;
                            value = movingSpeed.ToString("0.00");
                        }
                        __result = true;
                        return false;
                    }
                    return true;
                }
            }
        }
    }
}
