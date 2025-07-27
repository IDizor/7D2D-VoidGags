using HarmonyLib;
using UnityEngine;
using static ItemActionRanged;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PiercingShots()
        {
            LogApplyingPatch(nameof(Settings.PiercingShots));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionRanged), nameof(ItemActionRanged.fireShot)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemActionRanged_fireShot.APrefix p) => ItemActionRanged_fireShot.Prefix(p.__instance, p._shotIdx, p._actionData, p.___hitmaskOverride))));

            Harmony.Patch(AccessTools.Method(typeof(ProjectileMoveScript), nameof(ProjectileMoveScript.checkCollision)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((ProjectileMoveScript_checkCollision.APrefix p) => ProjectileMoveScript_checkCollision.Prefix(p.__instance, p.___isOnIdealPos, p.___idealPosition, p.___previousPosition, p.___hitMask, p.___radius, p.___firingEntity))));
        }

        /// <summary>
        /// Bullets can break weak blocks and hit objects behind.
        /// </summary>
        public class ItemActionRanged_fireShot
        {
            public struct APrefix
            {
                public ItemActionRanged __instance;
                public int _shotIdx;
                public ItemActionDataRanged _actionData;
                public int ___hitmaskOverride;
            }

            public static void Prefix(ItemActionRanged __instance, int _shotIdx, ItemActionDataRanged _actionData, int ___hitmaskOverride)
            {
                var world = _actionData.invData.world;
                EntityAlive holdingEntity = _actionData.invData.holdingEntity;
                ItemValue itemValue = _actionData.invData.itemValue;
                float range = __instance.GetRange(_actionData);
                Ray lookRay = holdingEntity.GetLookRay();
                lookRay.direction = __instance.getDirectionOffset(_actionData, lookRay.direction, _shotIdx);
                int hitMask = ((___hitmaskOverride == 0) ? 8 : ___hitmaskOverride);
                
                var repeat = false;
                var destroyedBlockPos = Vector3i.zero;
                do
                {
                    repeat = false;
                    if (Voxel.Raycast(world, lookRay, range, -538750997, hitMask, 0f))
                    {
                        if (GameUtils.IsBlockOrTerrain(Voxel.voxelRayHitInfo.tag))
                        {
                            var block = ItemActionAttack.GetBlockHit(world, Voxel.voxelRayHitInfo);
                            if (!block.isair && block.Block != null && block.GetCurrentHP() <= 10 && destroyedBlockPos != Voxel.voxelRayHitInfo.hit.blockPos
                                && !Helper.IsTraderArea(Voxel.voxelRayHitInfo.hit.blockPos))
                            {
                                if (Settings.ArrowsBoltsDistraction)
                                {
                                    var hitInfo = Voxel.voxelRayHitInfo.Clone();
                                    var shotStartPos = holdingEntity.position;
                                    Helper.DeferredAction(0.1f, () =>
                                    {
                                        ItemActionAttack_Hit.ProcessBlockHitAttraction(hitInfo, block, shotStartPos);
                                    });
                                }
                                block.Block.DamageBlock(world, 0, Voxel.voxelRayHitInfo.hit.blockPos, block, block.Block.MaxDamage, -1);
                                destroyedBlockPos = Voxel.voxelRayHitInfo.hit.blockPos;
                                repeat = true;
                            }
                        }
                    }
                } while (repeat);
            }
        }

        /// <summary>
        /// Arrows and bolts can break weak blocks and hit objects behind.
        /// </summary>
        public class ProjectileMoveScript_checkCollision
        {
            static bool skip = false;
            //static readonly List<string> loggedProjectiles = new List<string>();

            public struct APrefix
            {
                public ProjectileMoveScript __instance;
                public bool ___isOnIdealPos;
                public Vector3 ___idealPosition;
                public Vector3 ___previousPosition;
                public int ___hitMask;
                public float ___radius;
                public Entity ___firingEntity;
            }

            public static bool Prefix(ProjectileMoveScript __instance, bool ___isOnIdealPos, Vector3 ___idealPosition, Vector3 ___previousPosition, int ___hitMask, float ___radius, Entity ___firingEntity)
            {
                if (skip)
                {
                    return false;
                }
                if (__instance.itemValueLauncher == null || __instance.itemValueLauncher.ItemClass == null || !__instance.itemValueLauncher.ItemClass.ItemTags.Test_AnySet(ItemActionAttack_Hit.PerkArcheryTag))
                {
                    return true;
                }
                if (__instance.stateTime > 1f)
                {
                    return true;
                }
                var world = GameManager.Instance.World;
                Vector3 vector = ((!___isOnIdealPos) ? ___idealPosition : (__instance.transform.position + Origin.position));
                Vector3 vector2 = vector - ___previousPosition;
                float magnitude = vector2.magnitude;
                if (magnitude < 0.04f)
                {
                    return false;
                }
                /*
                var s = $"[{__instance.GetInstanceID()}] Weapon: {__instance.itemValueLauncher?.ItemClass.Name}, Projectile: {__instance.itemProjectile?.Name}, ProjectileTime: {projectileTime:0.00}, Magnitude: {magnitude:0.00}";
                if (!loggedProjectiles.Contains(s))
                {
                    loggedProjectiles.Add(s);
                    Debug.LogWarning(s);
                }
                */
                
                Ray ray = new Ray(___previousPosition, vector2.normalized);

                var repeat = false;
                var destroyedBlockPos = Vector3i.zero;
                var hitPos = Vector3.zero;
                do
                {
                    repeat = false;
                    bool num = Voxel.Raycast(world, ray, magnitude, -538750981, ___hitMask, ___radius);
                    if (num && GameUtils.IsBlockOrTerrain(Voxel.voxelRayHitInfo.tag) && destroyedBlockPos != Voxel.voxelRayHitInfo.hit.blockPos && ___firingEntity != null && !___firingEntity.isEntityRemote)
                    {
                        var block = ItemActionAttack.GetBlockHit(world, Voxel.voxelRayHitInfo);
                        var projectileName = __instance.itemProjectile?.Name;
                        if (!block.isair && block.Block != null && block.GetCurrentHP() <= 5 && projectileName != null && !projectileName.EndsWith("Stone") && !Helper.IsTraderArea(Voxel.voxelRayHitInfo.hit.blockPos))
                        {
                            //Debug.LogWarning($"destroyedBlockPos {Voxel.voxelRayHitInfo.hit.blockPos}, hit = {Voxel.voxelRayHitInfo.hit.pos}, collider.transform.tag = '{Voxel.phyxRaycastHit.collider?.transform?.tag}'");
                            if (Settings.ArrowsBoltsDistraction)
                            {
                                var hitInfo = Voxel.voxelRayHitInfo.Clone();
                                var shotStartPos = ___firingEntity == null ? Vector3.zero : ___firingEntity.position;
                                Helper.DeferredAction(0.1f, () =>
                                {
                                    ItemActionAttack_Hit.ProcessBlockHitAttraction(hitInfo, block, shotStartPos);
                                });
                            }
                            block.Block.DamageBlock(world, 0, Voxel.voxelRayHitInfo.hit.blockPos, block, block.Block.MaxDamage, -1);
                            destroyedBlockPos = Voxel.voxelRayHitInfo.hit.blockPos;
                            hitPos = Voxel.voxelRayHitInfo.hit.pos;
                            repeat = true;
                        }
                    }
                } while (repeat);

                if (destroyedBlockPos != Vector3i.zero)
                {
                    skip = true;
                    Helper.DoWhen(Unskip, () => {
                        bool success = Voxel.Raycast(world, ray, magnitude, -538750981, ___hitMask, ___radius);
                        return !success || hitPos != Voxel.voxelRayHitInfo.hit.pos;
                    }, 0.02f, 0.3f, failureAction: Unskip);
                    return false;
                }

                return true;

                void Unskip()
                {
                    Helper.DeferredAction(0.05f, () => skip = false);
                }
            }
        }
    }
}
