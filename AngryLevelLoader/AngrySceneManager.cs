using RudeLevelScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader
{
	public static class AngrySceneManager
	{
		public static string CurrentSceneName = "";

		public static void LoadLevel(AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
		{
			// LEGACY
			LegacyPatchController.enablePatches = false;

			Plugin.config.presetButtonInteractable = false;
			CurrentSceneName = levelName;

			Plugin.currentBundleContainer = bundleContainer;
			Plugin.currentLevelContainer = levelContainer;
			Plugin.currentLevelData = levelData;

			SceneHelper.LoadScene(levelName);
			Plugin.UpdateLastPlayed(bundleContainer);
		}

		public static void PostSceneLoad()
		{
			Plugin.currentLevelContainer.AssureSecretsSize();

			string secretString = Plugin.currentLevelContainer.secrets.value;
			foreach (Bonus bonus in Resources.FindObjectsOfTypeAll<Bonus>().Where(bonus => bonus.gameObject.scene.path == Plugin.currentLevelData.scenePath))
			{
				if (bonus.gameObject.scene.path != Plugin.currentLevelData.scenePath)
					continue;

				if (bonus.secretNumber >= 0 && bonus.secretNumber < secretString.Length && secretString[bonus.secretNumber] == 'T')
				{
					bonus.beenFound = true;
					bonus.BeenFound();
				}
			}
		}

		// LEGACY
		public static void LoadLegacyLevel(string levelPath)
		{
			LegacyPatchController.enablePatches = true;
			LegacyPatchController.Patch();
			CurrentSceneName = levelPath;
			SceneManager.LoadScene(levelPath);

			LegacyPatchController.LinkMixers();
			LegacyPatchController.ReplaceShaders();
		}
	}
}
