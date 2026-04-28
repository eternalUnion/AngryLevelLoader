using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(ChallengeManager))]
	internal static class ChallengeManagerPatch
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
