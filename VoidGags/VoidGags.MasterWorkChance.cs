using Audio;
using HarmonyLib;
using UniLinq;
using VoidGags.NetPackages;
using static VoidGags.VoidGags.MasterWorkChance;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_MasterWorkChance()
        {
            LogApplyingPatch(nameof(Settings.MasterWorkChance));

            if (Settings.MasterWorkChance <= 20 && Settings.MasterWorkChance > 0)
            {
                if (Settings.MasterWorkChance_MaxQuality < 1 || Settings.MasterWorkChance_MaxQuality > 6)
                {
                    LogModException($"Invalid value for setting '{nameof(Settings.MasterWorkChance_MaxQuality)}'. Should be in range 1..6.");
                    return;
                }

                if (Settings.MasterWorkChance_MaxQuality > 1)
                {
                    MasterWorkChanceValue = 0.01f * Settings.MasterWorkChance;

                    Harmony.Patch(AccessTools.Method(typeof(XUiC_RecipeStack), nameof(XUiC_RecipeStack.outputStack)),
                        prefix: new HarmonyMethod(XUiC_RecipeStack_outputStack.Prefix),
                        postfix: new HarmonyMethod(XUiC_RecipeStack_outputStack.Postfix));

                    Harmony.Patch(AccessTools.Method(typeof(TileEntityWorkstation), nameof(TileEntityWorkstation.HandleRecipeQueue)),
                        prefix: new HarmonyMethod(TileEntityWorkstation_HandleRecipeQueue.Prefix),
                        postfix: new HarmonyMethod(TileEntityWorkstation_HandleRecipeQueue.Postfix));

                    Harmony.Patch(AccessTools.Constructor(typeof(ItemValue), [typeof(int), typeof(int), typeof(int), typeof(bool), typeof(string[]), typeof(float)]),
                        prefix: new HarmonyMethod(ItemValue_ctor.Prefix));

                    OnGameLoadedActions.Add(RequestMasterWorkChanceServerValue);
                }
            }
            else if (Settings.MasterWorkChance != 0)
            {
                LogModException($"Invalid value for setting '{nameof(Settings.MasterWorkChance)}'. Should be in range 0..20 percent.");
            }
        }

        public static class MasterWorkChance
        {
            public static float MasterWorkChanceValue = 0.1f;
            public static int PlayerId = -1;

            public static void RequestMasterWorkChanceServerValue()
            {
                if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                {
                    SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageMasterWorkChance>()
                        .Setup(Settings.MasterWorkChance, Settings.MasterWorkChance_MaxQuality));
                }
            }

            public static void PlayMasterWorkSound()
            {
                Manager.PlayInsidePlayerHead("ui_challenge_redeem");
                Manager.PlayInsidePlayerHead("recipe_unlocked");
                Helper.DeferredAction(0.1f, () => Manager.PlayInsidePlayerHead("recipe_unlocked"));
                Helper.DeferredAction(0.2f, () => Manager.PlayInsidePlayerHead("recipe_unlocked"));
                Helper.DeferredAction(0.3f, () => Manager.PlayInsidePlayerHead("recipe_unlocked"));
            }

            /// <summary>
            /// Keep Player ID in the static field while this method is running (craft in hands or open workstations).
            /// </summary>
            public static class XUiC_RecipeStack_outputStack
            {
                public static void Prefix(XUiC_RecipeStack __instance, ItemValue ___originalItem)
                {
                    if (___originalItem == null || ___originalItem.Equals(ItemValue.None))
                    {
                        var recipe = __instance.GetRecipe();
                        if (recipe != null && recipe.GetOutputItemClass().ShowQualityBar)
                        {
                            PlayerId = __instance.StartingEntityId;
                        }
                    }
                }

                public static void Postfix()
                {
                    PlayerId = -1;
                }
            }

            /// <summary>
            /// Keep Player ID in the static field while this method is running (craft in workstations in background).
            /// </summary>
            public static class TileEntityWorkstation_HandleRecipeQueue
            {
                public static void Prefix(TileEntityWorkstation __instance)
                {
                    if (__instance.Queue != null && __instance.Queue.Length > 0)
                    {
                        RecipeQueueItem recipeQueueItem = __instance.Queue[__instance.Queue.Length - 1];
                        if (recipeQueueItem != null && recipeQueueItem.Multiplier > 0 && recipeQueueItem.Recipe != null && recipeQueueItem.Recipe.GetOutputItemClass().ShowQualityBar)
                        {
                            var lockedTiles = GameManager.Instance.lockedTileEntities;
                            if (!lockedTiles.Any(l => ((TileEntity)l.Key).entityId == __instance.entityId)) // if workstation is not opened by any player
                            {
                                var crafterId = recipeQueueItem.StartingEntityId;
                                PlayerId = crafterId;
                            }
                        }
                    }
                }

                public static void Postfix()
                {
                    PlayerId = -1;
                }
            }

            /// <summary>
            /// Apply master work chance once item is created.
            /// </summary>
            public static class ItemValue_ctor
            {
                public static void Prefix(ref int minQuality, ref int maxQuality)
                {
                    if (minQuality == maxQuality && maxQuality > 0 && maxQuality < 6 && Settings.MasterWorkChance_MaxQuality > maxQuality)
                    {
                        if (GameManager.Instance.World.GetGameRandom().RandomFloat <= MasterWorkChanceValue)
                        {
                            minQuality++;
                            maxQuality++;

                            if (PlayerId > 0)
                            {
                                var localPlayer = GameManager.Instance?.World?.GetPrimaryPlayer();
                                if (localPlayer?.entityId == PlayerId)
                                {
                                    PlayMasterWorkSound();
                                }
                                else
                                {
                                    SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageMasterWorkCreated>()
                                        .Setup(PlayerId), _onlyClientsAttachedToAnEntity: true, _attachedToEntityId: PlayerId);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
