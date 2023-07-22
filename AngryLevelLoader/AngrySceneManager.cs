using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader
{
	public static class AngrySceneManager
	{
		public static string CurrentTempLoadPath = "";
		public static string CurrentScenePath = "";

		public static void LoadLevel(string levelPath, string tempFolder)
		{
			// LEGACY
			LegacyPatchController.enablePatches = false;

			Plugin.config.presetButtonInteractable = false;
			CurrentTempLoadPath = tempFolder;
			CurrentScenePath = levelPath;

			SceneHelper.LoadScene(levelPath);
		}

		// LEGACY
		public static void LoadLegacyLevel(string levelPath)
		{
			LegacyPatchController.enablePatches = true;
			LegacyPatchController.Patch();
			CurrentScenePath = levelPath;
			SceneManager.LoadScene(levelPath);

			LegacyPatchController.LinkMixers();
			LegacyPatchController.ReplaceShaders();
		}
	}
}
