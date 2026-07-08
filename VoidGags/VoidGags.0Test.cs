using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using static EntityVehicle;

namespace VoidGags
{
    /// <summary>
    /// Test file for development and testing new features.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        //Utils.DrawOutline(new Rect(num + 5f, num4, num2 - 10f, inputAreaHeight), text2, labelStyle, Color.black, Color.white);



        //[HarmonyPatch(typeof(AvatarZombieController), nameof(AvatarZombieController.StartAnimationDodge))]
        //public static class knqwefkjeqw
        //{
        //    public static void Prefix(AvatarZombieController __instance, float _blend)
        //    {
        //        LogWarning($"StartAnimationDodge: {__instance.Entity.EntityName}, blend = {_blend:0.00}");
        //    }
        //}



        //[HarmonyPatch(typeof(LootManager), nameof(LootManager.LootContainerOpened))]
        //public static class doivjmndf
        //{
        //    [HarmonyReversePatch]
        //    [MethodImpl(MethodImplOptions.NoInlining)]
        //    public static void Reverse(LootManager __instance, ITileEntityLootable _tileEntity, int _entityIdThatOpenedIt, FastTags<TagGroup.Global> _containerTags)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public static bool Prefix(LootManager __instance, ITileEntityLootable _tileEntity, int _entityIdThatOpenedIt, FastTags<TagGroup.Global> _containerTags)
        //    {
        //        var pos = _tileEntity.ToWorldPos();
        //        if (!dfnghdngfmfgh.LootManagerActions.ContainsKey(pos))
        //        {
        //            dfnghdngfmfgh.LootManagerActions.Add(pos, () => Reverse(__instance, _tileEntity, _entityIdThatOpenedIt, _containerTags));
        //            Helper.DeferredAction(30f, () => dfnghdngfmfgh.LootManagerActions.Remove(pos));
        //        }
        //        return false;
        //    }
        //}
    }
}
