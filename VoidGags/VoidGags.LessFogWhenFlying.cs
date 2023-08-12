using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_LessFogWhenFlying(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(SkyManager), "SetFogDensity"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((float density) => SkyManager_SetFogDensity.Prefix(ref density))));

            LogPatchApplied(nameof(Settings.LessFogWhenFlying));
        }

        /// <summary>
        /// Less fog when flying at high altitude for better vision.
        /// </summary>
        public class SkyManager_SetFogDensity
        {
            const float minDensity = 0.13f;
            const float maxDensity = 0.3f;
            const float range = maxDensity - minDensity;
            const float bestVisionHeight = 100f;

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
