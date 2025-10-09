using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.ExhaustingLadders;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ExhaustingLadders()
        {
            LogApplyingPatch(nameof(Settings.ExhaustingLadders));

            Harmony.Patch(AccessTools.Method(typeof(EffectManager), nameof(EffectManager.GetValue)),
                prefix: new HarmonyMethod(EffectManager_GetValue.Prefix),
                postfix: new HarmonyMethod(EffectManager_GetValue.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.OnUpdateLive)),
                postfix: new HarmonyMethod(EntityPlayerLocal_OnUpdateLive.Postfix));
        }

        public static class ExhaustingLadders
        {
            /// <summary>
            /// Consume stamina on ladders.
            /// </summary>
            public static class EffectManager_GetValue
            {
                public static void Prefix(PassiveEffects _passiveEffect, EntityAlive _entity, ref FastTags<TagGroup.Global> tags, out bool __state)
                {
                    if (_passiveEffect == PassiveEffects.StaminaChangeOT && _entity != null
                        && !tags.IsEmpty && _entity is EntityPlayerLocal player)
                    {
                        //Debug.LogWarning($"StaminaChangeOT = {string.Join("/", tags.GetTagNames())}");
                        if (!player.onGround && player.isLadderAttached && !player.isSwimming && !player.IsInWater() && !player.IsFlyMode.Value)
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
            public static class EntityPlayerLocal_OnUpdateLive
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
}
