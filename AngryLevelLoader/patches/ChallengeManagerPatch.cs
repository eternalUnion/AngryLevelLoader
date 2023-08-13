using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.patches
{
    [HarmonyPatch(typeof(ChallengeManager))]
    public static class ChallengeManagerPatch
    {
        [HarmonyPatch(nameof(ChallengeManager.OnEnable))]
        [HarmonyPrefix]
        public static bool CancelOnEnable()
        {
            if (Plugin.isInCustomScene)
                return false;
            return true;
        }
    }
}
