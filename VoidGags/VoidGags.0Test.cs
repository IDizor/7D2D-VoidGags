using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// Test file for development and testing new features.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        //[HarmonyPatch(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.PostInit))]
        //public static class knqwefkjeqw
        //{
        //    public static void Postfix()
        //    {
        //    }
        //}

        //[HarmonyPatch(typeof(XUiC_MapArea), nameof(XUiC_MapArea.OnOpen))]
        //public static class sgfdgsdgbvc
        //{
        //    public static void Prefix(XUiC_MapArea __instance)
        //    {
        //        var w = __instance.GetParentWindow();
        //        var g = w.Controller.WindowGroup;
        //        if (w != null)
        //        {
        //            LogModWarning("XUiC_MapArea.OnOpen");
        //            g.isModal = false;
        //            //w.UiTransform.gameObject.SetActive(false);
        //            //w.xui.playerUI.RefreshNavigationTarget();
        //        }
        //    }

        //    public static void Postfix(XUiC_MapArea __instance)
        //    {
        //        var w = __instance.GetParentWindow();
        //        var g = w.Controller.WindowGroup;
        //        if (w != null)
        //        {
        //            LogModWarning("XUiC_MapArea.OnOpen.p");
        //            g.isModal = false;

        //            __instance.isOpen = false;
        //            __instance.xui.playerUI.GetComponentInParent<LocalPlayerCamera>().PreRender -= __instance.OnPreRender;
        //            __instance.xui.calloutWindow.DisableCallouts(XUiC_GamepadCalloutWindow.CalloutType.Menu);
        //            __instance.xui.playerUI.CursorController.Locked = false;
        //            SoftCursor.SetCursor(CursorControllerAbs.ECursorType.Default);
        //            __instance.closestMouseOverNavObject = null;
        //            g.windowManager.DisableWindowActionSet(g);
        //            g.windowManager.modalWindow = null;
        //            g.windowManager.cursorWindowOpen = false;
        //        }
        //    }
        //}
    }
}
