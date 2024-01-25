using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
    //Support custom level ids when using the AbruptLevelChanger component
    [HarmonyPatch]
    public static class AbrutptLevelChangerPatch
    {
        [HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.AbruptChangeLevel)), HarmonyPrefix]
        public static bool OnAbruptChangeLevel(AbruptLevelChanger __instance, string levelName)
        {
            return ChangeLevel(__instance, levelName);
        }

        [HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.NormalChangeLevel)), HarmonyPrefix]
        public static bool OnNormalChangeLevel(AbruptLevelChanger __instance, string levelName)
        {
            return ChangeLevel(__instance, levelName);
        }

        private static bool ChangeLevel(AbruptLevelChanger __instance, string levelName)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            if (string.IsNullOrEmpty(levelName))
                return false;

            if (AngrySceneManager.TryFindLevel(levelName, out LevelContainer result))
            {
                //Prevent the AbruptLevelChanger from loading the level and load angry level
                AngrySceneManager.LoadLevel(result.container, result, result.data, result.data.scenePath);
                return false;
            }
            else
            {
                Plugin.logger.LogWarning("Could not find target level id " + levelName);

                //Don't save the mission since we're in custom level
                if (__instance.saveMission)
                    __instance.saveMission = false;

                return true; //Passthrough as to not break shops that use this component.
            }

        }
    }
}
