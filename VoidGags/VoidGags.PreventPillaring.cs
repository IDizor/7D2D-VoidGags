using HarmonyLib;
using static VoidGags.VoidGags.PreventPillaring;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PreventPillaring()
        {
            LogApplyingPatch(nameof(Settings.PreventPillaring));

            Harmony.Patch(AccessTools.Method(typeof(RenderDisplacedCube), nameof(RenderDisplacedCube.update0)),
                prefix: new HarmonyMethod(RenderDisplacedCube_update0.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(Block), nameof(Block.CanPlaceBlockAt)),
                prefix: new HarmonyMethod(Block_CanPlaceBlockAt.Prefix));
        }

        public static class PreventPillaring
        {
            public static bool PlacingAllowed = true;

            /// <summary>
            /// Set allow-flag for further check when placing blocks.
            /// </summary>
            public static class RenderDisplacedCube_update0
            {
                public static void Prefix(Vector3i _focusBlockPos, EntityAlive _player)
                {
                    PlacingAllowed = _player.IsGodMode.Value
                        || _player.onGround
                        || _player.IsInWater()
                        || _focusBlockPos.y >= _player.position.y
                        || _player is EntityPlayerLocal && ((EntityPlayerLocal)_player).isLadderAttached
                        || IsLandClaimBlockArea();

                    bool IsLandClaimBlockArea()
                    {
                        var playerData = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(_player.entityId);
                        var ownership = _player.world.GetLandClaimOwner(_focusBlockPos, playerData);

                        return ownership == EnumLandClaimOwner.Self || ownership == EnumLandClaimOwner.Ally;
                    }
                }
            }

            /// <summary>
            /// Prevents pillaring - jumping and placing a block underneath you.
            /// </summary>
            public static class Block_CanPlaceBlockAt
            {
                public static bool Prefix(ref bool __result)
                {
                    var caller = Helper.GetCallerMethod();
                    if (caller.Name.Contains(":update0(") || (caller.DeclaringType == typeof(BlockToolSelection) && caller.Name == nameof(BlockToolSelection.ExecuteUseAction)))
                    {
                        if (!PlacingAllowed)
                        {
                            __result = false;
                            return false;
                        }
                    }
                    //else
                    //{
                    //    var ps = Helper.GetCallStackPath(5).Replace(" <-- ", "~").Split('~');
                    //    UnityEngine.Debug.LogError($"!CanPlaceBlockAt, {caller.DeclaringType.Name}.{caller.Name}");
                    //    foreach (var p in ps)
                    //    {
                    //        UnityEngine.Debug.LogWarning($"{p}");
                    //    }
                    //}
                    return true;
                }
            }
        }
    }
}
