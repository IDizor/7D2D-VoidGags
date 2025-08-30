using System.Collections.Generic;
using HarmonyLib;
using static VoidGags.VoidGags.HelmetLightByDefault;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_HelmetLightByDefault()
        {
            LogApplyingPatch(nameof(Settings.HelmetLightByDefault));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.CollectActivatableItems)),
                postfix: new HarmonyMethod(EntityAlive_CollectActivatableItems.Postfix));
        }

        public static class HelmetLightByDefault
        {
            /// <summary>
            /// Use helmet light mod by default when F pressed.
            /// </summary>
            public static class EntityAlive_CollectActivatableItems
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
}
