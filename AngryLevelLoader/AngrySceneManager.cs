using RudeLevelScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader
{
	public static class AngrySceneManager
	{
		public static string CurrentSceneName = "";

		public static void LevelButtonPressed(AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
		{
			List<string> requiredScripts = new List<string>();
			foreach (var data in bundleContainer.GetAllLevelData())
				foreach (string script in data.requiredDllNames)
					if (!requiredScripts.Contains(script))
						requiredScripts.Add(script);

			List<string> scriptsToLoad = new List<string>();
			List<string> scriptsToUpdate= new List<string>();
			foreach (string script in requiredScripts)
			{
				if (Plugin.ScriptLoaded(script))
				{
					ScriptInfo info = ScriptCatalogLoader.scriptCatalog == null ? null : ScriptCatalogLoader.scriptCatalog.Scripts.Where(s => s.FileName == script).FirstOrDefault();
					if (info != null)
					{
						string hash = CryptographyUtils.GetMD5String(File.ReadAllBytes(Path.Combine(Plugin.workingDir, "Scripts", script)));
						if (hash != info.Hash)
							scriptsToUpdate.Add(script);
					}					
				}
				else
				{
					scriptsToLoad.Add(script);
				}
			}
		
			if (scriptsToLoad.Count != 0 || scriptsToUpdate.Count != 0)
			{

			}
		}

		public static void LoadLevel(AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
		{
			// LEGACY
			LegacyPatchController.enablePatches = false;

			Plugin.config.presetButtonInteractable = false;
			CurrentSceneName = levelName;

			Plugin.currentBundleContainer = bundleContainer;
			Plugin.currentLevelContainer = levelContainer;
			Plugin.currentLevelData = levelData;

			MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", Plugin.selectedDifficulty);

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
			MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", Plugin.selectedDifficulty);
			
			LegacyPatchController.enablePatches = true;
			LegacyPatchController.Patch();
			CurrentSceneName = levelPath;
			SceneManager.LoadScene(levelPath);

			LegacyPatchController.LinkMixers();
			LegacyPatchController.ReplaceShaders();
		}
	}
}
