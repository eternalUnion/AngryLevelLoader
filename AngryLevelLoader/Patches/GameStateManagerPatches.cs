using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(LeaderboardController))]
    class GameStateManagerPatches
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

            __result = MonoSingleton<AssistController>.Instance.cheatsEnabled || MonoSingleton<StatsManager>.Instance.majorUsed;
			return false;
        }
    }
}
