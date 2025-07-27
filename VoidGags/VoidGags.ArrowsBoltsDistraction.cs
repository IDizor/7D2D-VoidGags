using HarmonyLib;
using UnityEngine;
using VoidGags.NetPackages;
using static ItemActionAttack;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ArrowsBoltsDistraction()
        {
            LogApplyingPatch(nameof(Settings.ArrowsBoltsDistraction));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionAttack), nameof(ItemActionAttack.Hit)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemActionAttack_Hit.APostfix p) => ItemActionAttack_Hit.Postfix(p.hitInfo, p._attackerEntityId, p._damageType, p._attackDetails, p.damagingItemValue))));
        }

        /// <summary>
        /// Arrows and bolts make noise and attract enemies. Can wake sleeping zombies.
        /// </summary>
        public class ItemActionAttack_Hit
        {
            public static FastTags<TagGroup.Global> PerkArcheryTag = FastTags<TagGroup.Global>.Parse("perkArchery");

            public struct APostfix
            {
                public WorldRayHitInfo hitInfo;
                public int _attackerEntityId;
                public EnumDamageTypes _damageType;
                public AttackHitInfo _attackDetails;
                public ItemValue damagingItemValue;
            }

            public static void Postfix(WorldRayHitInfo hitInfo, int _attackerEntityId, EnumDamageTypes _damageType, AttackHitInfo _attackDetails, ItemValue damagingItemValue)
            {
                if (_damageType == EnumDamageTypes.Piercing && damagingItemValue != null && damagingItemValue.ItemClass != null && damagingItemValue.ItemClass.ItemTags.Test_AnySet(PerkArcheryTag))
                {
                    EntityAlive entityAlive = GameManager.Instance.World.GetEntity(_attackerEntityId) as EntityAlive;
                    var shotStartPos = entityAlive == null ? Vector3.zero : entityAlive.position;
                    ProcessBlockHitAttraction(hitInfo, _attackDetails.blockBeingDamaged, shotStartPos);
                }
            }

            public static void ProcessBlockHitAttraction(WorldRayHitInfo hitInfo, BlockValue damagedBlock, Vector3 shotStartPos)
            {
                if (!damagedBlock.isair)
                {
                    var material = damagedBlock.Block?.Properties.GetString("Material");
                    if (material != null)
                    {
                        var distractionRadius = 10f;

                        material = material.ToLower();
                        if (material.StartsWith("mair"))
                        {
                            return;
                        }
                        else if (material.StartsWith("mwood"))
                        {
                            distractionRadius = 10f;
                        }
                        else if (material.StartsWith("mmetal"))
                        {
                            distractionRadius = 12f;
                        }
                        else if (material.StartsWith("msteel"))
                        {
                            distractionRadius = 12f;
                        }
                        else if (material.StartsWith("mconcrete"))
                        {
                            distractionRadius = 8f;
                        }
                        else if (material.StartsWith("mcloth"))
                        {
                            distractionRadius = 5f;
                        }
                        else if (material.StartsWith("mfurniture"))
                        {
                            distractionRadius = 7f;
                        }
                        else if (material.StartsWith("mglass"))
                        {
                            distractionRadius = 20f;
                        }

                        var world = GameManager.Instance.World;
                        var random = world.GetGameRandom();
                        var distractionTargets = Helper.GetEntities<EntityEnemy>(hitInfo.hit.pos, distractionRadius);
                        var lastBlockPos = hitInfo.lastBlockPos.ToVector3Center();
                        var hitPos = hitInfo.hit.pos;

                        //Debug.LogError($"distractionTargets = {distractionTargets.Count}");
                        if (distractionTargets.Count > 0)
                        {
                            Helper.DeferredAction(0.333f, () =>
                            {
                                foreach (var enemy in distractionTargets)
                                {
                                    //Debug.LogWarning($"{enemy.EntityClass.entityClassName} [{(lastBlockPos - enemy.position).magnitude:0.00}] : {enemy != null}, {enemy.distraction == null}, {!enemy.IsDead()}, {!enemy.InvestigatesMoreDistantPos(lastBlockPos)}");
                                    if (enemy != null && enemy.distraction == null && !enemy.IsDead() && !enemy.InvestigatesMoreDistantPos(lastBlockPos))
                                    {
                                        var noiceOcclusion = Helper.CalculateNoiseOcclusion(lastBlockPos, enemy.position, 0.027f);
                                        var occlusion = noiceOcclusion * Mathf.Pow(distractionRadius / 10f, 0.2f); // apply material/radius adjustment
                                        //Debug.LogWarning($"occlusion : {noiceOcclusion:0.000} -> {occlusion:0.000}, distractionRadius = {distractionRadius:0.00}");
                                        var wasSleeping = enemy.IsSleeping;
                                        if (occlusion >= 0.87f && enemy.IsSleeping)
                                        {
                                            //Debug.LogError($"ConditionalTriggerSleeperWakeUp() : {enemy.EntityName}");
                                            enemy.ConditionalTriggerSleeperWakeUp();
                                        }
                                        if (!enemy.IsSleeping && (wasSleeping || occlusion >= 0.3f))
                                        {
                                            var investigatePos = hitPos;
                                            if (wasSleeping && shotStartPos != Vector3.zero)
                                            {
                                                investigatePos = shotStartPos;
                                                if ((shotStartPos - hitPos).magnitude > 3f)
                                                {
                                                    investigatePos = Vector3.Lerp(hitPos, shotStartPos, 0.2f + random.RandomFloat / 4f);
                                                    //Debug.LogWarning($"InvestigatePosition lerped: {Helper.WorldPosToCompasText(new Vector3i(investigatePos))}");
                                                }
                                            }
                                            enemy.SetInvestigatePosition(investigatePos, 600, isAlert: true);
                                            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToClientsOrServer(NetPackageManager.GetPackage<NetPackageSetInvestigatePos>().Setup(enemy.entityId, investigatePos, 600));
                                            //Debug.LogWarning($"SetInvestigatePosition() {enemy.EntityName} : {Helper.WorldPosToCompasText(new Vector3i(enemy.position))} --> {Helper.WorldPosToCompasText(new Vector3i(investigatePos))}");
                                        }
                                    }
                                }
                                distractionTargets.Clear();
                            });
                        }
                    }
                }
            }
        }
    }
}
