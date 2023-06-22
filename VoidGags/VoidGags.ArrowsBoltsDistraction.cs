using HarmonyLib;
using UnityEngine;
using static ItemActionAttack;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ArrowsBoltsDistraction(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ItemActionAttack), "Hit"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemActionAttack_Hit.APostfix p) =>
                ItemActionAttack_Hit.Postfix(p.hitInfo, p._damageType, p._attackDetails, p.damagingItemValue))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.ArrowsBoltsDistraction)}");
        }

        /// <summary>
        /// Arrows and bolts make noise and attract enemies. Can wake sleeping zombies.
        /// </summary>
        public class ItemActionAttack_Hit
        {
            public static FastTags PerkArcheryTag = FastTags.Parse("perkArchery");

            public struct APostfix
            {
                public WorldRayHitInfo hitInfo;
                public EnumDamageTypes _damageType;
                public AttackHitInfo _attackDetails;
                public ItemValue damagingItemValue;
            }

            public static void Postfix(WorldRayHitInfo hitInfo, EnumDamageTypes _damageType, AttackHitInfo _attackDetails, ItemValue damagingItemValue)
            {
                if (_damageType == EnumDamageTypes.Piercing && damagingItemValue != null && damagingItemValue.ItemClass != null && damagingItemValue.ItemClass.ItemTags.Test_AnySet(PerkArcheryTag))
                {
                    ProcessBlockHitAttraction(GameManager.Instance.World, hitInfo, _attackDetails.blockBeingDamaged);
                }
            }

            public static void ProcessBlockHitAttraction(World world, WorldRayHitInfo hitInfo, BlockValue damagedBlock)
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

                        var distractionStrength = 80f;
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
                                        if (occlusion >= 0.87f && enemy.IsSleeping)
                                        {
                                            enemy.ConditionalTriggerSleeperWakeUp();
                                        }
                                        if (occlusion >= 0.3f && !enemy.IsSleeping)
                                        {
                                            float num = enemy.distractionResistance - distractionStrength;
                                            if (num <= 0f || num < world.GetGameRandom().RandomFloat * 100f)
                                            {
                                                enemy.SetInvestigatePosition(lastBlockPos, 1000);
                                                //Debug.LogWarning($"InvestigatePosition set.");
                                            }
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
