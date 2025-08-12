using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_DamageModifier()
        {
            LogApplyingPatch(nameof(Settings.DamageModifier));

            if (Settings.DamageModifier_Gun != 1f)
            {
                Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.DamageEntity)),
                    prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive_DamageEntity_2.APrefix p) => EntityAlive_DamageEntity_2.Prefix(p._damageSource, ref p._strength))));
            }
        }

        /// <summary>
        /// Modifies guns damage.
        /// </summary>
        public class EntityAlive_DamageEntity_2
        {
            public static FastTags<TagGroup.Global> GunTag = FastTags<TagGroup.Global>.Parse("gun");

            public struct APrefix
            {
                public DamageSource _damageSource;
                public int _strength;
            }

            public static void Prefix(DamageSource _damageSource, ref int _strength)
            {
                if (_damageSource.damageType == EnumDamageTypes.Piercing &&
                    _damageSource.ItemClass?.HasAllTags(GunTag) == true)
                {
                    _strength = Mathf.CeilToInt(_strength * Settings.DamageModifier_Gun);
                }
            }
        }
    }
}
