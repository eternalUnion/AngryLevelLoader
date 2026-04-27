using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(CutsceneSkip))]
    public class CutsceneSkipPatches
    {
        [HarmonyPatch(nameof(CutsceneSkip.Start))]
        [HarmonyPrefix]
        private static bool StartPatch(CutsceneSkip __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            bool playedLevelBefore = AngrySceneManager.currentLevelContainer.FinalRank != '-';
            return playedLevelBefore;
        }
    }
}
