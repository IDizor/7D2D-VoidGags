using System.Reflection;
using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_StealthOnLadders(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), "MoveByInput"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityPlayerLocal_MoveByInput.AParams p) => EntityPlayerLocal_MoveByInput.Prefix(p.__instance, ref p.___isLadderAttached, out p.__state))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityPlayerLocal_MoveByInput.AParams p) => EntityPlayerLocal_MoveByInput.Postfix(p.__instance, ref p.___isLadderAttached, p.__state))));

            harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), "GetSpeedModifier"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityPlayerLocal_GetSpeedModifier.APostfix p) => EntityPlayerLocal_GetSpeedModifier.Postfix(p.__instance, ref p.__result, p.___isLadderAttached))));

            harmony.Patch(AccessTools.Method(typeof(PlayerStealth), "CalcVolume"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((PlayerStealth_CalcVolume.APostfix p) => PlayerStealth_CalcVolume.Postfix(ref p.__result, p.___player))));

            LogPatchApplied(nameof(Settings.StealthOnLadders));
        }

        /// <summary>
        /// Keep crouching state on the ladder.
        /// </summary>
        public class EntityPlayerLocal_MoveByInput
        {
            public struct AParams
            {
                public EntityPlayerLocal __instance;
                public bool ___isLadderAttached;
                public bool __state;
            }

            public static void Prefix(EntityPlayerLocal __instance, ref bool ___isLadderAttached, out bool __state)
            {
                __state = ___isLadderAttached;
                if (__instance.AttachedToEntity == null)
                {
                    ___isLadderAttached = false;
                }
            }

            public static void Postfix(EntityPlayerLocal __instance, ref bool ___isLadderAttached, bool __state)
            {
                if (__instance.AttachedToEntity == null)
                    ___isLadderAttached = __state;
            }
        }

        /// <summary>
        /// Makes crouching on a ladder slower.
        /// </summary>
        public class EntityPlayerLocal_GetSpeedModifier
        {
            public struct APostfix
            {
                public EntityPlayerLocal __instance;
                public float __result;
                public bool ___isLadderAttached;
            }

            public static void Postfix(EntityPlayerLocal __instance, ref float __result, bool ___isLadderAttached)
            {
                if (__instance.IsCrouching && ___isLadderAttached)
                {
                    __result *= 0.5f;
                }
            }
        }

        /// <summary>
        /// Makes less noice when crouching on a ladder.
        /// </summary>
        public class PlayerStealth_CalcVolume
        {
            static readonly FieldInfo isLadderAttached = AccessTools.Field(typeof(EntityPlayerLocal), "isLadderAttached");

            public struct APostfix
            {
                public float __result;
                public EntityPlayer ___player;
            }

            public static void Postfix(ref float __result, EntityPlayer ___player)
            {
                if (__result > 0 && ___player.IsCrouching && ___player is EntityPlayerLocal player)
                {
                    if ((bool)isLadderAttached.GetValue(player))
                    {
                        __result *= 0.5f;
                    }
                }
            }
        }
    }
}
