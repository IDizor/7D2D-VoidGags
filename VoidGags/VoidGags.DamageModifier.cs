using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.DamageModifier;

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
                    prefix: new HarmonyMethod(EntityAlive_DamageEntity.Prefix));
            }
        }

        public static class DamageModifier
        {
            public static FastTags<TagGroup.Global> GunTag = FastTags<TagGroup.Global>.Parse("gun");

            /// <summary>
            /// Modifies guns damage.
            /// </summary>
            public static class EntityAlive_DamageEntity
            {
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
}
