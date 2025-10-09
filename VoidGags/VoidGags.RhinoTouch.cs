using HarmonyLib;
using static Block;
using static ItemActionAttack;
using static VoidGags.VoidGags.RhinoTouch;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_RhinoTouch()
        {
            LogApplyingPatch(nameof(Settings.RhinoTouch));

            Harmony.Patch(AccessTools.Method(typeof(Block), nameof(Block.OnBlockDamaged), [typeof(WorldBase), typeof(int), typeof(Vector3i), typeof(BlockValue), typeof(int), typeof(int), typeof(AttackHitInfo), typeof(bool), typeof(bool), typeof(int)]),
                prefix: new HarmonyMethod(Block_OnBlockDamaged.Prefix, Priority.First));

            Harmony.Patch(AccessTools.Method(typeof(Block), nameof(Block.OnBlockDestroyedBy)),
                postfix: new HarmonyMethod(Block_OnBlockDestroyedBy.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionAttack), nameof(ItemActionAttack.Hit)),
                prefix: new HarmonyMethod(ItemActionAttack_Hit.Prefix));
        }

        public static class RhinoTouch
        {
            public static Vector3i? BumpedBlockPos = null;
            public static void ClearBumpedBlockPos() => BumpedBlockPos = null;

            /// <summary>
            /// Suppress block damage if bumped off.
            /// </summary>
            public static class Block_OnBlockDamaged
            {
                public static bool Prefix(Vector3i _blockPos, BlockValue _blockValue, ref int __result)
                {
                    if (BumpedBlockPos != null && BumpedBlockPos.Value == _blockPos)
                    {
                        __result = _blockValue.damage;
                        return false;
                    }
                    return true;
                }
            }

            /// <summary>
            /// Push block if destroyed by vehicle.
            /// </summary>
            public static class Block_OnBlockDestroyedBy
            {
                public static void Postfix(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, int _entityId, ref DestroyedResult __result)
                {
                    if (__result != DestroyedResult.Keep && !_blockValue.Block.IsPlant())
                    {
                        var entity = _world.GetEntity(_entityId);
                        if (entity is EntityVehicle)
                        {
                            __result = DestroyedResult.Keep;
                            (_world as World)?.AddFallingBlock(_blockPos);
                        }
                    }
                }
            }

            /// <summary>
            /// Push block if approximate kinetic energy is enough.
            /// </summary>
            public static class ItemActionAttack_Hit
            {
                public static bool Prefix(WorldRayHitInfo hitInfo, int _attackerEntityId, float _blockDamage, AttackHitInfo _attackDetails)
                {
                    var world = GameManager.Instance.World;

                    if (hitInfo.hit.blockValue.isair ||
                        hitInfo.hit.blockValue.Block.shape.IsTerrain() ||
                        hitInfo.hit.blockValue.Block.IsPlant() ||
                        world.IsWithinTraderArea(hitInfo.hit.blockPos) ||
                        hitInfo.hit.blockValue.Block.Is("resourceRock"))
                    {
                        return true;
                    }

                    var entity = world.GetEntity(_attackerEntityId);
                    if (entity is EntityVehicle)
                    {
                        var speed = entity.GetVelocityPerSecond().magnitude;
                        var mass = entity.EntityClass.MassKg;
                        var blockMass = hitInfo.hit.blockValue.Block.MaxDamage;
                        var kineticEnergy = speed * mass * 20f;
                        //LogModWarning($"Vehicle kinetic energy = {kineticEnergy:0.00}. Block mass = {blockMass} ({hitInfo.hit.blockValue.Block.blockName}).");
                        if (kineticEnergy > blockMass)
                        {
                            BumpedBlockPos = hitInfo.hit.blockPos;
                            world.AddFallingBlock(hitInfo.hit.blockPos);
                            _attackDetails.bBlockHit = false;
                            Helper.DeferredAction(0.02f, ClearBumpedBlockPos);
                            return false;
                        }

                        // Lets keep a note how to add impulse to bumped block:
                        //Faller = (EntityFallingBlock)_entity;
                        //if (gfjdfgjnfgd.Blocks.TryGetValue(new(Faller.position), out Vector3 velocity))
                        //{
                        //    var vector = velocity * 400f;
                        //    Vector3 axis = Vector3.Cross(vector.normalized, Vector3.up);
                        //    vector = Quaternion.AngleAxis(10f, axis) * vector;
                        //    Faller.rigidBody.AddForce(vector, ForceMode.Impulse);
                        //}
                    }
                    return true;
                }
            }
        }
    }
}
