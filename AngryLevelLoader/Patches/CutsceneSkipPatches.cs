using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(CutsceneSkip))]
	internal class CutsceneSkipPatches
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
