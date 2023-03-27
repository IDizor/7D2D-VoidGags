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
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemActionAttack_Hit_Params p) =>
                ItemActionAttack_Hit.Postfix(p.hitInfo, p._damageType, p._attackDetails, p.damagingItemValue))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.ArrowsBoltsDistraction)}");
        }

        private struct ItemActionAttack_Hit_Params
        {
            public WorldRayHitInfo hitInfo;
            public EnumDamageTypes _damageType;
            public AttackHitInfo _attackDetails;
            public ItemValue damagingItemValue;
        }

        /// <summary>
        /// Arrows and bolts make noise and attract enemies. Can wake sleeping zombies.
        /// </summary>
        public class ItemActionAttack_Hit
        {
            public static FastTags PerkArcheryTag = FastTags.Parse("perkArchery");

            public static void Postfix(WorldRayHitInfo hitInfo, EnumDamageTypes _damageType, AttackHitInfo _attackDetails, ItemValue damagingItemValue)
            {
                if (_damageType == EnumDamageTypes.Piercing && damagingItemValue != null && damagingItemValue.ItemClass != null
                    && damagingItemValue.ItemClass.ItemTags.Test_AnySet(PerkArcheryTag))
                {
                    var material = _attackDetails.blockBeingDamaged.Block?.Properties.GetString("Material");
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
                            distractionRadius = 2f;
                        }
                        else if (material.StartsWith("mfurniture"))
                        {
                            distractionRadius = 7f;
                        }
                        else if (material.StartsWith("mglass"))
                        {
                            distractionRadius = 15f;
                        }

                        var distractionStrength = 20f;
                        var distractionTargets = Helper.GetEntities<EntityEnemy>(hitInfo.hit.pos, distractionRadius);
                        var lastBlockPos = hitInfo.lastBlockPos.ToVector3Center();
                        var hitPos = hitInfo.hit.pos;

                        Helper.DeferredAction(delayMs: 333, () =>
                        {
                            foreach (var entityEnemy in distractionTargets)
                            {
                                if (entityEnemy != null && entityEnemy.distraction == null && !entityEnemy.IsDead())
                                {
                                    var occlusion = Helper.CalculateNoiseOcclusion(lastBlockPos, entityEnemy.position, 0.03f);
                                    occlusion *= Mathf.Sqrt(distractionRadius / 10f); // adjust occlusion depending on radius/material
                                    if (occlusion >= 0.9f)
                                    {
                                        entityEnemy.ConditionalTriggerSleeperWakeUp();
                                    }
                                    if (occlusion >= 0.3f)
                                    {
                                        float num = entityEnemy.distractionResistance - distractionStrength;
                                        if (num <= 0f || num < GameManager.Instance.World.GetGameRandom().RandomFloat * 100f)
                                        {
                                            entityEnemy.SetInvestigatePosition(lastBlockPos, 1000);
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
