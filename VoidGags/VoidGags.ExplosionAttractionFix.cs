using HarmonyLib;
using UnityEngine;
using VoidGags.NetPackages;

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
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((AIDirector_OnSoundPlayedAtPosition.APrefix p) =>
                AIDirector_OnSoundPlayedAtPosition.Prefix(ref p._entityThatCausedSound, p._position, p.__instance))));

            harmony.Patch(AccessTools.Method(typeof(GameManager), "explode"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => GameManager_explode.Prefix())),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => GameManager_explode.Postfix())));

            LogPatchApplied(nameof(Settings.ExplosionAttractionFix));
        }

        /// <summary>
        /// Makes zombies to check explosion location, and not the player location.
        /// </summary>
        public class AIDirector_OnSoundPlayedAtPosition
        {
            public struct APrefix
            {
                public int _entityThatCausedSound;
                public Vector3 _position;
                public AIDirector __instance;
            }

            public static void Prefix(ref int _entityThatCausedSound, Vector3 _position, AIDirector __instance)
            {
                if (GameManager_explode.IsExplosion)
                {
                    _entityThatCausedSound = -1;
                    var wakeRadius = 15f;
                    var distractionRadius = 50f;
                    var distractionTargets = Helper.GetEntities<EntityEnemy>(_position, distractionRadius);
                    var targetsToWakeUp = Helper.GetEntities<EntityEnemy>(_position, wakeRadius);

                    foreach (var entityEnemy in distractionTargets)
                    {
                        if (entityEnemy.distraction != null && entityEnemy.distraction.position != _position)
                        {
                            continue;
                        }

                        if (entityEnemy.IsSleeping && targetsToWakeUp.Contains(entityEnemy))
                        {
                            entityEnemy.ConditionalTriggerSleeperWakeUp();
                        }

                        if (!entityEnemy.IsSleeping)
                        {
                            int ticks = entityEnemy.CalcInvestigateTicks((int)(30f + __instance.random.RandomFloat * 30f) * 20, null);
                            Vector2 vector = __instance.random.RandomInsideUnitCircle * 7f; // circle * radius
                            var randomizedPos = _position + new Vector3(vector.x, 0f, vector.y);
                            entityEnemy.SetInvestigatePosition(randomizedPos, ticks);
                            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToClientsOrServer(NetPackageManager.GetPackage<NetPackageSetInvestigatePos>().Setup(entityEnemy.entityId, randomizedPos, ticks));
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
