using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(ButtonHighlightParent))]
    class ButtonHighlightParentPatches
    {
        /*
         * A simple patch which prevents ButtonHighlightParent.Start() from getting called
         * twice. It could manually be called from Plugin.CreateCustomLevelButtonOnMainMenuAsync()
         * and when it is called again naturally, it causes Plugin Configurator button to be white
         * when selected.
         */

        [HarmonyPatch(nameof(ButtonHighlightParent.Start))]
        [HarmonyPrefix]
        static bool PreventDoubleStart(ButtonHighlightParent __instance)
        {
            if (__instance.buttons != null && __instance.buttons.Length != 0)
                return false;

            return true;
        }
    }
}
