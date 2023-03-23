﻿using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ExplosionAttractionFix(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(AIDirector), "OnSoundPlayedAtPosition"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((AIDirector_OnSoundPlayedAtPosition_Params p) =>
                AIDirector_OnSoundPlayedAtPosition.Prefix(ref p._entityThatCausedSound, p._position, p.__instance))));

            harmony.Patch(AccessTools.Method(typeof(GameManager), "explode"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => GameManager_explode.Prefix())),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => GameManager_explode.Postfix())));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.ExplosionAttractionFix)}");
        }

        private struct AIDirector_OnSoundPlayedAtPosition_Params
        {
            public int _entityThatCausedSound;
            public Vector3 _position;
            public AIDirector __instance;
        }

        /// <summary>
        /// Makes zombies to check explosion location, and not the player location.
        /// </summary>
        public class AIDirector_OnSoundPlayedAtPosition
        {
            public static void Prefix(ref int _entityThatCausedSound, Vector3 _position, AIDirector __instance)
            {
                if (GameManager_explode.IsExplosion)
                {
                    _entityThatCausedSound = -1;
                    var wakeRadius = 15f;
                    var distractionRadius = 50f;
                    var distractionStrength = 100f;
                    var distractionTargets = Helper.GetEntities<EntityEnemy>(_position, distractionRadius);
                    var targetsToWakeUp = Helper.GetEntities<EntityEnemy>(_position, wakeRadius);

                    foreach (var entityEnemy in distractionTargets)
                    {
                        if (entityEnemy.distraction != null)
                        {
                            continue;
                        }

                        if (targetsToWakeUp.Contains(entityEnemy))
                        {
                            entityEnemy.ConditionalTriggerSleeperWakeUp();
                        }

                        if (!entityEnemy.IsSleeping)
                        {
                            float num = entityEnemy.distractionResistance - distractionStrength;
                            if (num <= 0f || num < __instance.random.RandomFloat * 100f)
                            {
                                int ticks = entityEnemy.CalcInvestigateTicks((int)(30f + __instance.random.RandomFloat * 30f) * 20, null);
                                Vector2 vector = __instance.random.RandomInsideUnitCircle * 7f; // circle * radius
                                var randomizedPos = _position + new Vector3(vector.x, 0f, vector.y);
                                entityEnemy.SetInvestigatePosition(randomizedPos, ticks);
                            }
                        }
                    }
                    distractionTargets.Clear();
                    targetsToWakeUp.Clear();
                }
            }
        }

        /// <summary>
        /// Tracks explosion moment.
        /// </summary>
        public class GameManager_explode
        {
            public static bool IsExplosion = false;

            public static void Prefix()
            {
                IsExplosion = true;
            }

            public static void Postfix()
            {
                IsExplosion = false;
            }
        }
    }
}