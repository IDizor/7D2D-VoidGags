using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PreventPillaring(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(RenderDisplacedCube), "update0"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((RenderDisplacedCube_update0.APrefix p) => RenderDisplacedCube_update0.Prefix(p._focusBlockPos, p._player))));

            harmony.Patch(AccessTools.Method(typeof(Block), "CanPlaceBlockAt"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((bool __result) => Block_CanPlaceBlockAt.Prefix(ref __result))));

            LogPatchApplied(nameof(Settings.PreventPillaring));
        }

        /// <summary>
        /// Keeps allow-flag for further check when placing blocks.
        /// </summary>
        public class RenderDisplacedCube_update0
        {
            public static bool Allowed = true;
            
            public struct APrefix
            {
                public Vector3i _focusBlockPos;
                public EntityAlive _player;
            }

            public static void Prefix(Vector3i _focusBlockPos, EntityAlive _player)
            {
                Allowed = _player.IsGodMode.Value
                    || _player.onGround
                    || _player.IsInWater()
                    || _focusBlockPos.y >= _player.position.y
                    || _player is EntityPlayerLocal && ((EntityPlayerLocal)_player).isLadderAttached;
            }
        }

        /// <summary>
        /// Prevents pillaring - jumping and placing a block underneath you.
        /// </summary>
        public class Block_CanPlaceBlockAt
        {
            public static bool Prefix(ref bool __result)
            {
                var caller = Helper.GetCallerMethod();
                if (caller.Name.Contains(":update0(") || (caller.Name == nameof(BlockToolSelection.ExecuteUseAction) && caller.DeclaringType.Name == nameof(BlockToolSelection)))
                {
                    if (!RenderDisplacedCube_update0.Allowed)
                    {
                        __result = false;
                        return false;
                    }
                }
                /*else
                {
                    var ps = Helper.GetCallStackPath(5).Replace(" <-- ", "~").Split('~');
                    UnityEngine.Debug.LogError($"!CanPlaceBlockAt, {caller.DeclaringType.Name}.{caller.Name}");
                    foreach (var p in ps)
                    {
                        UnityEngine.Debug.LogWarning($"{p}");
                    }
                }*/
                return true;
            }
        }
    }
}
