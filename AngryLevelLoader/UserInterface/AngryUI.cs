using AngryLevelLoader.Managers;
using AngryUiComponents;
using PluginConfig;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AngryLevelLoader.UserInterface
{
	internal static class AngryUI
	{
		private const string ANGRY_UI_PANEL_ASSET_PATH = "AngryLevelLoader/UI/AngryUIPanel.prefab";

		private static AngryUIPanelComponent currentPanel;
		private static void CreateAngryUI()
		{
			Plugin.instance.StartCoroutine(CreateAngryUIAsync());
		}

		private static IEnumerator CreateAngryUIAsync()
		{
			yield return null;

			if (currentPanel != null)
				yield break;

			GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvasObj == null)
			{
				Plugin.logger.LogWarning("Angry tried to create main menu buttons, but root canvas was not found!");
				yield break;
			}

			GameObject panelObj = Addressables.InstantiateAsync(ANGRY_UI_PANEL_ASSET_PATH, canvasObj.transform).WaitForCompletion();
			currentPanel = panelObj.GetComponent<AngryUIPanelComponent>();

			currentPanel.reloadBundlePrompt.MakeTransparent(true);
			currentPanel.reloadScriptPrompt.MakeTransparent(true);

			if (AngrySceneManager.currentBundleContainer.FileChangeDetected && !AngrySceneManager.currentBundleContainer.IgnoreFileChange)
			{
				ShowReloadBundlePrompt();
				yield break;
			}

			if (!string.IsNullOrEmpty(UpdatedScript))
			{
				ShowReloadScriptPrompt();
				yield break;
			}
		}

		internal static void ReloadScript()
		{
			if (string.IsNullOrEmpty(UpdatedScript))
				return;

			// Save state
			if (AngrySceneManager.isInCustomLevel)
			{
				InternalConfigManager.instantLoadLevel.value = true;
				InternalConfigManager.instantLoadLevelGuid.value = AngrySceneManager.currentBundleContainer.bundleGuid;
				InternalConfigManager.instantLoadLevelId.value = AngrySceneManager.currentLevelContainer.levelId;
			}

			PluginConfiguratorController.FlushAllConfigs();

			// Restart the game
			ProcessStartInfo procInfo = new ProcessStartInfo()
			{
				FileName = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ULTRAKILL.exe"),	
				WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			};
			
			foreach (string arg in Environment.GetCommandLineArgs().Skip(1))
				procInfo.ArgumentList.Add(arg);

			string[] variablesToRemove = new string[]
			{
				"DOORSTOP_DISABLE",
				"DOORSTOP_DLL_SEARCH_DIRS",
				"DOORSTOP_INITIALIZED",
				"DOORSTOP_INVOKE_DLL_PATH",
				"DOORSTOP_MANAGED_FOLDER_DIR",
				"DOORSTOP_MONO_LIB_PATH",
				"DOORSTOP_PROCESS_PATH",
			};

			foreach (string variable in variablesToRemove)
			{
				if (procInfo.EnvironmentVariables.ContainsKey(variable))
					procInfo.EnvironmentVariables.Remove(variable);
				if (procInfo.Environment.ContainsKey(variable))
					procInfo.Environment.Remove(variable);
			}

			Process.Start(procInfo);
			Application.Quit();
		}

		private static void ShowReloadScriptPrompt()
		{
			if (currentPanel == null || !AngrySceneManager.isInCustomLevel || string.IsNullOrEmpty(UpdatedScript) || currentPanel.reloadScriptPrompt.gameObject.activeSelf)
				return;

			currentPanel.reloadBundlePrompt.gameObject.SetActive(false);

			currentPanel.reloadScriptPrompt.gameObject.SetActive(true);
			currentPanel.reloadScriptPrompt.audio.Play();
			currentPanel.reloadScriptPrompt.text.text = $"Script update detected\nPress <color=orange>{ConfigManager.reloadScriptKeybind.value}</color> to reload\n(Can be binded in the settings)";
			currentPanel.reloadScriptPrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
			currentPanel.reloadScriptPrompt.reloadButton.onClick.AddListener(() =>
			{
				ReloadScript();
			});

			currentPanel.reloadScriptPrompt.ignoreButton.onClick = new Button.ButtonClickedEvent();
			currentPanel.reloadScriptPrompt.ignoreButton.onClick.AddListener(() =>
			{
				currentPanel.reloadScriptPrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
				currentPanel.reloadScriptPrompt.gameObject.SetActive(false);
				UpdatedScript = string.Empty;
			});
		}

		internal static void ShowReloadBundlePrompt()
		{
			if (currentPanel == null || !AngrySceneManager.isInCustomLevel || currentPanel.reloadBundlePrompt.gameObject.activeSelf)
				return;

			currentPanel.reloadScriptPrompt.gameObject.SetActive(false);

			currentPanel.reloadBundlePrompt.gameObject.SetActive(true);
			currentPanel.reloadBundlePrompt.audio.Play();
			currentPanel.reloadBundlePrompt.text.text = $"File update detected\nPress <color=orange>{ConfigManager.reloadFileKeybind.value}</color> to reload\n(Can be binded in the settings)";
			currentPanel.reloadBundlePrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
			currentPanel.reloadBundlePrompt.reloadButton.onClick.AddListener(() =>
			{
				AngrySceneManager.currentBundleContainer.ReloadBundle(false, false);
			});

			currentPanel.reloadBundlePrompt.ignoreButton.onClick = new Button.ButtonClickedEvent();
			currentPanel.reloadBundlePrompt.ignoreButton.onClick.AddListener(() =>
			{
				AngrySceneManager.currentBundleContainer.IgnoreFileChange = true;
				currentPanel.reloadBundlePrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
				currentPanel.reloadBundlePrompt.gameObject.SetActive(false);
			});
		}

		private static string _updatedScript = string.Empty;
		internal static string UpdatedScript
		{
			get => _updatedScript;
			set
			{
				_updatedScript = value;

				if (currentPanel != null && !string.IsNullOrEmpty(_updatedScript))
					ShowReloadScriptPrompt();
			}
		}

		internal static void MakeTransparent(bool transparent)
		{
			if (currentPanel != null)
			{
				if (transparent)
				{
					currentPanel.reloadBundlePrompt.MakeTransparent(false);
					currentPanel.reloadScriptPrompt.MakeTransparent(false);
				}
				else
				{
					currentPanel.reloadBundlePrompt.MakeOpaque(false);
					currentPanel.reloadScriptPrompt.MakeOpaque(false);
				}
			}
		}

		internal static void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				if (!AngrySceneManager.isInCustomLevel)
					return;

				Plugin.logger.LogInfo("Creating UI panel");
				CreateAngryUI();
			};
		}
	}
}
