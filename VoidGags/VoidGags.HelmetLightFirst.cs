using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_HelmetLightFirst(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(EntityAlive), "GetActivatableItemPool"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((List<ItemValue> __result) => EntityAlive_GetActivatableItemPool.Postfix(ref __result))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.HelmetLightByDefault)}");
        }

        /// <summary>
        /// Uses helmet light mod as default when pressing F.
        /// </summary>
        public class EntityAlive_GetActivatableItemPool
        {
            public static void Postfix(ref List<ItemValue> __result)
            {
                if (__result != null && __result.Count > 1)
                {
                    var lightMod = __result.FirstOrDefault(i => i.ItemClass?.Name == "modArmorHelmetLight");
                    if (lightMod != null)
                    {
                        __result.Remove(lightMod);
                        __result.Insert(0, lightMod);
                    }
                }
            }
        }
    }
}
