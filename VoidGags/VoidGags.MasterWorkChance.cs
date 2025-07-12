using System;
using Audio;
using HarmonyLib;
using UniLinq;
using VoidGags.NetPackages;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_MasterWorkChance(Harmony harmony)
        {
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

                    harmony.Patch(AccessTools.Method(typeof(XUiC_RecipeStack), nameof(XUiC_RecipeStack.outputStack)),
                        new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_RecipeStack_outputStack.APrefix p) => XUiC_RecipeStack_outputStack.Prefix(p.__instance, p.___originalItem))),
                        new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => XUiC_RecipeStack_outputStack.Postfix())));

                    harmony.Patch(AccessTools.Method(typeof(TileEntityWorkstation), nameof(TileEntityWorkstation.HandleRecipeQueue)),
                        new HarmonyMethod(SymbolExtensions.GetMethodInfo((TileEntityWorkstation __instance) => TileEntityWorkstation_HandleRecipeQueue.Prefix(__instance))),
                        new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => TileEntityWorkstation_HandleRecipeQueue.Postfix())));

                    harmony.Patch(AccessTools.Constructor(typeof(ItemValue), new Type[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(string[]), typeof(float) }),
                        new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemValue_ctor.APrefix p) => ItemValue_ctor.Prefix(ref p.minQuality, ref p.maxQuality))));

                    OnGameLoadedActions.Add(RequestMasterWorkChanceServerValue);

                    LogPatchApplied(nameof(Settings.MasterWorkChance));
                }
            }
            else if (Settings.MasterWorkChance != 0)
            {
                LogModException($"Invalid value for setting '{nameof(Settings.MasterWorkChance)}'. Should be in range 0..20 percent.");
            }
        }

        public static float MasterWorkChanceValue = 0.1f;

        public void RequestMasterWorkChanceServerValue()
        {
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
            {
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageMasterWorkChance>()
                    .Setup(Settings.MasterWorkChance, Settings.MasterWorkChance_MaxQuality));
            }
        }

        /// <summary>
        /// Keep Player ID in the static field while this method is running (craft in hands or open workstations).
        /// </summary>
        public class XUiC_RecipeStack_outputStack
        {
            public struct APrefix
            {
                public XUiC_RecipeStack __instance;
                public ItemValue ___originalItem;
            }

            public static void Prefix(XUiC_RecipeStack __instance, ItemValue ___originalItem)
            {
                if (___originalItem == null || ___originalItem.Equals(ItemValue.None))
                {
                    var recipe = __instance.GetRecipe();
                    if (recipe != null && recipe.GetOutputItemClass().ShowQualityBar)
                    {
                        ItemValue_ctor.PlayerId = __instance.StartingEntityId;
                    }
                }
            }

            public static void Postfix()
            {
                ItemValue_ctor.PlayerId = -1;
            }
        }

        /// <summary>
        /// Keep Player ID in the static field while this method is running (craft in workstations in background).
        /// </summary>
        public class TileEntityWorkstation_HandleRecipeQueue
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
                            ItemValue_ctor.PlayerId = crafterId;
                        }
                    }
                }
            }

            public static void Postfix()
            {
                ItemValue_ctor.PlayerId = -1;
            }
        }

        /// <summary>
        /// Apply master work chance once item is created.
        /// </summary>
        public class ItemValue_ctor
        {
            public static int PlayerId = -1;

            public struct APrefix
            {
                public int minQuality;
                public int maxQuality;
            }

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

            public static void PlayMasterWorkSound()
            {
                Manager.PlayInsidePlayerHead("ui_challenge_redeem");
                Manager.PlayInsidePlayerHead("recipe_unlocked");
                Helper.DeferredAction(0.1f, () => Manager.PlayInsidePlayerHead("recipe_unlocked"));
                Helper.DeferredAction(0.2f, () => Manager.PlayInsidePlayerHead("recipe_unlocked"));
                Helper.DeferredAction(0.3f, () => Manager.PlayInsidePlayerHead("recipe_unlocked"));
            }
        }
    }
}
