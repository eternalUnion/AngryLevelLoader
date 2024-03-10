using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches.NoMo
{
    [HarmonyPatch(typeof(DisabledEnemiesChecker))]
    public static class DisabledEnemiesCheckerPatches
    {
        [HarmonyPatch(nameof(DisabledEnemiesChecker.Update))]
        [HarmonyPrefix]
        public static bool CheckNoMoActivation(DisabledEnemiesChecker __instance)
        {
            if (!AngrySceneManager.isInCustomLevel || !Plugin.NoMonsters)
                return true;

            //NoMo activated so skip the method regardless of if we activate it ourselves.
            if (StatsManager.Instance == null || !StatsManager.Instance.levelStarted || __instance.activated)
                return false;

            __instance.activated = true;
            //Base method uses invoke so im just gonna do the same for simplicity sake.
            __instance.Invoke(nameof(__instance.Activate), __instance.delay);
            
            return false;
        }
    }
}
