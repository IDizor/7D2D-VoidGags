using System;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using static VoidGags.VoidGags.CurtainsSmartCut;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_CurtainsSmartCut()
        {
            LogApplyingPatch(nameof(Settings.CurtainsSmartCut));

            UseXmlPatches(nameof(Settings.CurtainsSmartCut));

            Harmony.Patch(AccessTools.Method(typeof(Block), nameof(Block.OnBlockDamaged)),
                prefix: new HarmonyMethod(Block_OnBlockDamaged.Prefix));
        }

        public static class CurtainsSmartCut
        {
            public const string VerticalHangTag = "VG_verticalHang";

            /// <summary>
            /// Smart cutting for curtains, drapes and blinds: from bottom to top.
            /// </summary>
            public static class Block_OnBlockDamaged
            {
                /// <summary>
                /// Recursive OnBlockDamaged.
                /// </summary>
                public static bool Prefix(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, ItemActionAttack.AttackHitInfo _attackHitInfo, bool _bUseHarvestTool, bool _bBypassMaxDamage, int _recDepth, ref int __result)
                {
                    if (_attackHitInfo?.entityHit != null && _attackHitInfo.entityHit is EntityVehicle)
                        return true; 

                    if (!_blockValue.isair && _blockValue.Block != null && IsVerticalHang(_blockValue.Block))
                    {
                        var nextBlockPos = new Vector3i(_blockPos.x, Mathf.Max(0, _blockPos.y - 1), _blockPos.z);
                        var nextBlockValue = _world.GetBlock(nextBlockPos);
                        if (!nextBlockValue.isair && nextBlockValue.Block != null && IsVerticalHang(nextBlockValue.Block))
                        {
                            if (_attackHitInfo != null)
                            {
                                _attackHitInfo.blockBeingDamaged = nextBlockValue;
                            }

                            __result = nextBlockValue.Block.OnBlockDamaged(
                                _world: _world,
                                _clrIdx: _clrIdx,
                                _blockPos: nextBlockPos,
                                _blockValue: nextBlockValue,
                                _damagePoints: _damagePoints,
                                _entityIdThatDamaged: _entityIdThatDamaged,
                                _attackHitInfo: _attackHitInfo,
                                _bUseHarvestTool: _bUseHarvestTool,
                                _bBypassMaxDamage: _bBypassMaxDamage,
                                _recDepth: _recDepth);
                            return false;
                        }
                    }

                    return true;
                }

                public static bool IsVerticalHang(Block block)
                {
                    return block?.FilterTags != null && block.FilterTags.Any(tag => tag == VerticalHangTag);
                }
            }
        }
    }
}
