using AngryLevelLoader.Managers;
using HarmonyLib;
using Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(MapVarSaver))]
    public static class MapVarSaverPatches
    {
        [HarmonyPatch(nameof(MapVarSaver.AssembleCurrentFilePath))]
        [HarmonyPostfix]
        public static void ReplaceLevelID(ref string __result)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return;

            string levelID = AngrySceneManager.currentLevelData.uniqueIdentifier;
            string bundleID = AngrySceneManager.currentBundleContainer.bundleData.bundleGuid;
            //Final path should be something like ULTRAKILL/Saves/Slot1/MapVars/AngryLevelLoader/VeryUnqiueBundleID/VeryUniqueLevelID.vars.json
            __result = Path.Combine(MapVarSaver.MapVarDirectory, Plugin.PLUGIN_NAME, bundleID, levelID + ".vars.json");
        }
    }
}
