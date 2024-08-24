using HarmonyLib;
using UniLinq;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_GeneratorSwitchFirst(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(BlockPowerSource), nameof(BlockPowerSource.GetBlockActivationCommands)), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((BlockActivationCommand[] __result) =>
                BlockPowerSource_GetBlockActivationCommands.Postfix(ref __result))));

            LogPatchApplied(nameof(Settings.GeneratorSwitchFirst));
        }

        /// <summary>
        /// Press E to turn on/off power generators.
        /// </summary>
        public class BlockPowerSource_GetBlockActivationCommands
        {
            public static void Postfix(ref BlockActivationCommand[] __result)
            {
                if (__result != null)
                {
                    var list = __result.ToList();
                    var i = list.FindIndex(i => i.text.Same("light"));
                    if (i > 0 && list[i].enabled)
                    {
                        (list[i], list[0]) = (list[0], list[i]);
                        __result = list.ToArray();
                    }
                }
            }
        }
    }
}
