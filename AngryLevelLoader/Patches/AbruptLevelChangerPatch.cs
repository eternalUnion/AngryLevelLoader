using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(AbruptLevelChanger), nameof(AbruptLevelChanger.AbruptChangeLevel))]
    public static class AbrutptLevelChanger_AbruptChangeLevel_Patch
    {
        //Support custom level ids when using the AbruptLevelChanger component
        [HarmonyPrefix]
        public static bool Prefix(AbruptLevelChanger __instance, string levelName)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            if (string.IsNullOrEmpty(levelName))
                return false;

            if(AngrySceneManager.TryFindLevel(levelName, out LevelContainer result))
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
