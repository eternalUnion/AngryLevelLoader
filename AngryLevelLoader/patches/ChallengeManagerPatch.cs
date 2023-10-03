using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(ChallengeManager))]
    public static class ChallengeManagerPatch
    {
        [HarmonyPatch(nameof(ChallengeManager.OnEnable))]
        [HarmonyPrefix]
        public static bool CancelOnEnable()
        {
            if (AngrySceneManager.isInCustomLevel)
                return false;
            return true;
        }
    }
}
