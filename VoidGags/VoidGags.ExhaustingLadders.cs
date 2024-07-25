using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ExhaustingLadders(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(EffectManager), "GetValue"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((EffectManager_GetValue.APrefix p) => EffectManager_GetValue.Prefix(p._passiveEffect, p._entity, ref p.tags, out p.__state))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((EffectManager_GetValue.APostfix p) => EffectManager_GetValue.Postfix(ref p.__result, p.__state))));

            harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), "OnUpdateLive"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityPlayerLocal __instance) => EntityPlayerLocal_OnUpdateLive.Postfix(__instance))));

            LogPatchApplied(nameof(Settings.ExhaustingLadders));
        }

        /// <summary>
        /// Consume stamina on ladders.
        /// </summary>
        public class EffectManager_GetValue
        {
            public struct APrefix
            {
                public PassiveEffects _passiveEffect;
                public EntityAlive _entity;
                public FastTags<TagGroup.Global> tags;
                public bool __state;
            }

            public struct APostfix
            {
                public float __result;
                public bool __state;
            }

            public static void Prefix(PassiveEffects _passiveEffect, EntityAlive _entity, ref FastTags<TagGroup.Global> tags, out bool __state)
            {
                if (_passiveEffect == PassiveEffects.StaminaChangeOT && _entity != null
                    && !tags.IsEmpty && _entity is EntityPlayerLocal player)
                {
                    //Debug.LogWarning($"StaminaChangeOT = {string.Join("/", tags.GetTagNames())}");
                    if (!player.onGround && player.isLadderAttached && !player.isSwimming && !player.IsFlyMode.Value)
                    {
                        __state = true;
                        return;
                    }
                }
                __state = false;
            }

            public static void Postfix(ref float __result, bool __state)
            {
                if (__state)
                {
                    __result = Mathf.Min(__result, -2f);
                }
            }
        }

        /// <summary>
        /// Fall down from the ladder when stamina runs out.
        /// </summary>
        public class EntityPlayerLocal_OnUpdateLive
        {
            public static void Postfix(EntityPlayerLocal __instance)
            {
                var player = __instance;

                if (!player.onGround && player.isLadderAttached && !player.isSwimming && !player.IsFlyMode.Value)
                {
                    if (player.bExhausted)
                    {
                        player.canLadderAirAttach = false;
                        player.MakeAttached(false);
                        return;
                    }
                }
            }
        }
    }
}
