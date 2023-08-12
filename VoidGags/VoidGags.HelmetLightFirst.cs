using System.Collections.Generic;
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
            harmony.Patch(AccessTools.Method(typeof(EntityAlive), "CollectActivatableItems"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((List<ItemValue> _pool) => EntityAlive_CollectActivatableItems.Postfix(_pool))));

            LogPatchApplied(nameof(Settings.HelmetLightByDefault));
        }

        /// <summary>
        /// Use helmet light mod by default when F pressed.
        /// </summary>
        public class EntityAlive_CollectActivatableItems
        {
            public static void Postfix(List<ItemValue> _pool)
            {
                if (_pool != null && _pool.Count > 1)
                {
                    var index = _pool.FindIndex(i => i.ItemClass?.Name == "modArmorHelmetLight");
                    if (index > 0)
                    {
                        (_pool[index], _pool[0]) = (_pool[0], _pool[index]);
                    }
                }
            }
        }
    }
}
