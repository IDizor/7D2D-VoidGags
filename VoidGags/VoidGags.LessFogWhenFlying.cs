using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.LessFogWhenFlying;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_LessFogWhenFlying()
        {
            LogApplyingPatch(nameof(Settings.LessFogWhenFlying));

            Harmony.Patch(AccessTools.Method(typeof(SkyManager), nameof(SkyManager.SetFogDensity)),
                prefix: new HarmonyMethod(SkyManager_SetFogDensity.Prefix));
        }

        public static class LessFogWhenFlying
        {
            const float minDensity = 0.127f;
            const float maxDensity = 0.30f;
            const float range = maxDensity - minDensity;
            const float bestVisionHeight = 100f;

            /// <summary>
            /// Less fog when flying at high altitude for better vision.
            /// </summary>
            public static class SkyManager_SetFogDensity
            {
                public static void Prefix(ref float density)
                {
                    var world = GameManager.Instance.World;
                    var player = world.GetPrimaryPlayer();
                    if (player != null)
                    {
                        if ((player.AttachedToEntity != null && player.AttachedToEntity is EntityVehicle) || player.IsGodMode.Value)
                        {
                            var terrainHeight = world.GetTerrainHeight((int)player.position.x, (int)player.position.z);
                            var playerAltitude = Mathf.Max(0, (int)player.position.y - terrainHeight);
                            var sub = Mathf.Min(range, range / bestVisionHeight * playerAltitude);
                            density = Mathf.Min(density, maxDensity - sub);
                        }
                    }
                }
            }
        }
    }
}
