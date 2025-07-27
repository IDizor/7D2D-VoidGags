using System;
using System.Linq;
using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PickupDamagedBlock()
        {
            LogApplyingPatch(nameof(Settings.PickupDamagedItems));

            if (Settings.PickupDamagedItems_Percentage < 100 && Settings.PickupDamagedItems_Percentage >= 0)
            {
                PickupDamagedBlockPercentage = 0.01f * (100 - Settings.PickupDamagedItems_Percentage);

                if (IsUndeadLegacy)
                {
                    var typesToPatch = TypeToPatch.GetAllFromAssembly(UndeadLegacyAssembly, "OnBlockActivated", "_blockValue");
                    typesToPatch.AddRange(TypeToPatch.GetAllFromAssembly(UndeadLegacyAssembly, "TakeWithTimer", "_blockValue"));
                    var prefix = SymbolExtensions.GetMethodInfo((BlockValue _blockValue) => SomeBlockType_WithMethodThatUsesBlockValue.Prefix(ref _blockValue));
                    foreach (var ttp in typesToPatch)
                    {
                        try
                        {
                            Harmony.Patch(AccessTools.Method(ttp.Type, ttp.MethodName, ttp.MethodParameters), prefix: new HarmonyMethod(prefix));
                        }
                        catch (Exception ex)
                        {
                            LogModException($"Failed to patch UndeadLegacy type method '{ttp.Type.Name}.{ttp.MethodName}()'. {ex.Message}");
                        }
                    }
                }
                else
                {
                    var nativeAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.FullName.StartsWith("Assembly-CSharp,"));

                    var typesToPatch = TypeToPatch.GetAllFromAssembly(nativeAssembly, "TakeItemWithTimer", "_blockValue");
                    var prefix = SymbolExtensions.GetMethodInfo((BlockValue _blockValue) => SomeBlockType_WithMethodThatUsesBlockValue.Prefix(ref _blockValue));
                    foreach (var ttp in typesToPatch)
                    {
                        Harmony.Patch(AccessTools.Method(ttp.Type, ttp.MethodName, ttp.MethodParameters), prefix: new HarmonyMethod(prefix));
                    }
                    
                    Harmony.Patch(AccessTools.Method(typeof(Block), nameof(Block.OnBlockActivated), [typeof(WorldBase), typeof(int), typeof(Vector3i), typeof(BlockValue), typeof(EntityPlayerLocal)]),
                        prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((BlockValue _blockValue) => SomeBlockType_WithMethodThatUsesBlockValue.Prefix(ref _blockValue))));
                    
                    Harmony.Patch(AccessTools.Method(typeof(World), nameof(World.GetBlock), [typeof(Vector3i)]),
                        postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((BlockValue _blockValue) => World_GetBlock.Postfix(ref _blockValue))));
                }
            }
            else
            {
                if (Settings.PickupDamagedItems_Percentage != 100)
                {
                    LogModException($"Invalid value for setting '{nameof(Settings.PickupDamagedItems_Percentage)}'.");
                }
            }
        }

        private static float PickupDamagedBlockPercentage = 0.2f;
        
        /// <summary>
        /// Makes block undamaged for futher checks.
        /// </summary>
        public class SomeBlockType_WithMethodThatUsesBlockValue
        {
            public static void Prefix(ref BlockValue _blockValue)
            {
                if (_blockValue.damage > 0 && _blockValue.Block != null)
                {
                    if (PickupDamagedBlockPercentage * _blockValue.Block.MaxDamage > _blockValue.damage)
                    {
                        _blockValue.damage = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Allows to pickup heavy items like crafting stations (event when the timer elapsed).
        /// Had to add this patch because of an odd code in the original methods like <see cref="BlockCampfire.EventData_Event"/>:
        /// </summary>
        public class World_GetBlock
        {
            public static Type[] AllowedToPickupBlockTypes = [typeof(BlockWorkstation), typeof(BlockDewCollector)];

            public static void Postfix(ref BlockValue __result)
            {
                if (__result.damage > 0 && __result.Block != null)
                {
                    if (PickupDamagedBlockPercentage * __result.Block.MaxDamage > __result.damage)
                    {
                        var caller = Helper.GetCallerMethod();
                        if (caller.Name == "EventData_Event" && AllowedToPickupBlockTypes.Contains(caller.DeclaringType))
                        {
                            __result.damage = 0;
                        }
                    }
                }
            }
        }
    }
}
