using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(LeaderboardController))]
	internal class GameStateManagerPatches
    {
        /**
         * This patch prevents scores never being posted because of GameStateManager.CanSubmitScores
         * always returning false when playing custom levels. The custom level requirement is ignored
         * if playing an angry level.
         */
        [HarmonyPatch(nameof(LeaderboardController.LeaderboardsBlocked), MethodType.Getter)]
        [HarmonyPrefix]
        static bool EnablePostingScoresInCustomLevels(ref bool __result)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            __result = AssistController.Instance.cheatsEnabled || StatsManager.Instance.majorUsed;
			return false;
        }
    }
}
