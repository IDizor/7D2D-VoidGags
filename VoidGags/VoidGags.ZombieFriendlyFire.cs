using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UniLinq;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ZombiesFriendlyFire()
        {
            LogApplyingPatch(nameof(Settings.ZombiesFriendlyFire));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.DamageEntity)),
                transpiler: new HarmonyMethod(SymbolExtensions.GetMethodInfo((IEnumerable<CodeInstruction> instructions) => EntityAlive_DamageEntity.Transpiler(instructions))));
        }

        /// <summary>
        /// Zombies can hurt each other.
        /// </summary>
        public static class EntityAlive_DamageEntity
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var patched = false;
                var IL = new List<CodeInstruction>(instructions);

                // original code:
                // bool flag = _damageSource.GetDamageType() == EnumDamageTypes.Heat;
                // if (!flag && (bool)entityAlive && (entityFlags & entityAlive.entityFlags & EntityFlags.Zombie) != 0)
                // {
                //     return -1;
                // }

                // IL code:
                // IL_00d1: ldfld valuetype EntityFlags Entity::entityFlags
                // IL_00d6: and
                // IL_00d7: ldc.i4.2
                // IL_00d8: and
                // IL_00d9: ldc.i4.0
                // IL_00da: ble.un.s IL_00de

                for (int i = 0; i < IL.Count; i++)
                {
                    if (IL[i].opcode == OpCodes.Ldfld &&
                        IL[i + 1].opcode == OpCodes.And &&
                        IL[i + 2].opcode == OpCodes.Ldc_I4_2 &&
                        IL[i + 3].opcode == OpCodes.And &&
                        IL[i + 4].opcode == OpCodes.Ldc_I4_0 &&
                        IL[i + 5].opcode == OpCodes.Ble_Un &&
                        IL[i].ToString().Same("ldfld EntityFlags Entity::entityFlags"))
                    {
                        // make unconditional jump instead of OpCodes.Ble_Un
                        IL[i + 5].opcode = OpCodes.Br;

                        // remove not used values from stack
                        IL.Insert(i + 5, new CodeInstruction(OpCodes.Pop));
                        IL.Insert(i + 5, new CodeInstruction(OpCodes.Pop));

                        patched = true;
                        break;
                    }
                }

                if (!patched)
                {
                    LogModTranspilerFailure(nameof(Settings.ZombiesFriendlyFire));
                }

                return IL.AsEnumerable();
            }
        }
    }
}
