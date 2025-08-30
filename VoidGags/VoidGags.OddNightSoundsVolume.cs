using HarmonyLib;
using static VoidGags.VoidGags.OddNightSoundsVolume;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_OddNightSoundsVolume()
        {
            if (Settings.OddNightSoundsVolume >= 0 && Settings.OddNightSoundsVolume < 100)
            {
                LogApplyingPatch(nameof(Settings.OddNightSoundsVolume));

                Harmony.Patch(AccessTools.Method(typeof(AudioObject), nameof(AudioObject.SetBiomeVolume)),
                    prefix: new HarmonyMethod(AudioObject_SetBiomeVolume.Prefix));
            }
            else if (Settings.OddNightSoundsVolume != 100)
            {
                LogModException($"Invalid value for setting '{nameof(Settings.OddNightSoundsVolume)}'. Valid range is 0..100.");
            }
        }

        public static class OddNightSoundsVolume
        {
            /*
            [HarmonyPatch(typeof(EnvironmentAudioManager))]
            [HarmonyPatch("InitSounds")]
            public static class dfgjghkfgk
            {
                public static void Postfix(EnvironmentAudioManager __instance)
                {
                    Debug.LogError("InitSounds()");
                    foreach (var sound in __instance.mixedBiomeSounds)
                    {
                        if (sound.name.Contains("Night"))
                        {
                            var s = $"{sound.trigger} [{sound.audioClips?.Length}] : {sound.name}";
                            Debug.LogWarning(s);
                        }
                    }
                }
            }
            */

            /// <summary>
            /// Modify odd night sounds volume.
            /// </summary>
            public static class AudioObject_SetBiomeVolume
            {
                public static void Prefix(AudioObject __instance, ref float _volume)
                {
                    if (__instance.name == "Night_Oneshots")
                    {
                        _volume *= 0.33f;
                    }
                }
            }
        }
    }
}
