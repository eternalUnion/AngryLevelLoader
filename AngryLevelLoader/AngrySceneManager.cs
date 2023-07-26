using PluginConfig;
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
			{
				if (data.requiredDllNames == null)
					continue;

				foreach (string script in data.requiredDllNames)
					if (!requiredScripts.Contains(script))
						requiredScripts.Add(script);
			}

			List<string> scriptsToDownload = new List<string>();
			foreach (string script in requiredScripts)
			{
				if (Plugin.ScriptLoaded(script))
				{
					ScriptInfo info = ScriptCatalogLoader.scriptCatalog == null ? null : ScriptCatalogLoader.scriptCatalog.Scripts.Where(s => s.FileName == script).FirstOrDefault();
					if (info != null)
					{
						string hash = CryptographyUtils.GetMD5String(File.ReadAllBytes(Path.Combine(Plugin.workingDir, "Scripts", script)));
						if (hash != info.Hash)
							scriptsToDownload.Add(script);
					}					
				}
				else if (!Plugin.ScriptExists(script))
				{
					scriptsToDownload.Add(script);
				}
			}
		
			if (scriptsToDownload.Count != 0)
			{
				NotificationPanel.Open(new ScriptUpdateNotification(scriptsToDownload, requiredScripts, bundleContainer, levelContainer, levelData, levelName));
			}
			else
			{
				LoadLevelWithScripts(requiredScripts ,bundleContainer, levelContainer, levelData, levelName);
			}
		}

		public static void LoadLevelWithScripts(List<string> scripts, AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
		{
			Stack<ScriptWarningNotification> notifications = new Stack<ScriptWarningNotification>();
			foreach (string script in scripts)
			{
				if (Plugin.ScriptLoaded(script))
					continue;

				ScriptWarningNotification notification= null;

				if (!Plugin.ScriptExists(script))
				{
					notification = new ScriptWarningNotification("<color=yellow>Missing Script</color>", $"Script {script} is missing and may cause issues in the level", "Cancel", "Continue", (inst) =>
					{
						inst.Close();
						foreach (var not in notifications)
							not.Close();
					}, (inst) =>
					{
						inst.Close();
						notifications.Pop();

						if (notifications.Count == 0)
						{
							LoadLevel(bundleContainer, levelContainer, levelData, levelName);
						}
					});
				}
				else
				{
					var result = Plugin.AttemptLoadScriptWithCertificate(script);

					if (result == Plugin.LoadScriptResult.Loaded)
						continue;

					notification = new ScriptWarningNotification("<color=red>Unverified Script</color>", $"Script {script} {(result == Plugin.LoadScriptResult.NoCertificate ? "has no certificate" : "has invalid certificate")}, loading scripts from unknown sources could be dangerous", "Cancel", "Load", (inst) =>
					{
						inst.Close();
						foreach (var not in notifications)
							not.Close();
					}, (inst) =>
					{
						inst.Close();
						notifications.Pop();

						Plugin.ForceLoadScript(script);

						if (notifications.Count == 0)
						{
							LoadLevel(bundleContainer, levelContainer, levelData, levelName);
						}
					});
				}

				if (notification != null)
				{
					notifications.Push(notification);
					NotificationPanel.Open(notification);
				}
			}

			if (notifications.Count == 0)
				LoadLevel(bundleContainer, levelContainer, levelData, levelName);
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
