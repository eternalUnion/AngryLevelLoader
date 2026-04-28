using AngryLevelLoader.Managers;
using AngryUiComponents;
using PluginConfig;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static AngryLevelLoader.Managers.ConfigManager;

namespace AngryLevelLoader.UserInterface
{
	internal static class AngryCustomLevelButton
	{
		// Create the shortcut in chapters menu
		internal const string CUSTOM_LEVEL_BUTTON_ASSET_PATH = "AngryLevelLoader/UI/CustomLevels.prefab";
		internal static AngryCustomLevelButtonComponent currentCustomLevelButton;
		internal static RectTransform bossRushButton;

		private static void CreateCustomLevelButtonOnMainMenu()
		{
			Plugin.instance.StartCoroutine(CreateCustomLevelButtonOnMainMenuAsync());
		}

		private static IEnumerator CreateCustomLevelButtonOnMainMenuAsync()
		{
			yield return null;

			GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvasObj == null)
			{
				Plugin.logger.LogWarning("Angry tried to create main menu buttons, but root canvas was not found!");
				yield break;
			}

			Transform chapters = canvasObj.transform.Find("Chapter Select/Chapters");
			if (chapters != null)
			{
				Transform chapterSelect = canvasObj.transform.Find("Chapter Select");

				GameObject customLevelButtonObj = Addressables.InstantiateAsync(CUSTOM_LEVEL_BUTTON_ASSET_PATH, chapters).WaitForCompletion();
				Transform bossRush = chapters.Find("Boss Rush Button");
				if (bossRush != null)
					bossRushButton = bossRush.gameObject.GetComponent<RectTransform>();
				currentCustomLevelButton = customLevelButtonObj.GetComponent<AngryCustomLevelButtonComponent>();

				currentCustomLevelButton.button.onClick = new Button.ButtonClickedEvent();
				currentCustomLevelButton.button.onClick.AddListener(() =>
				{
					// Find the options menu
					Transform optionsMenu = canvasObj.transform.Find("OptionsMenu");
					if (optionsMenu == null)
					{
						Plugin.logger.LogError("Angry tried to find the options menu but failed!");
						return;
					}

					// Disable act selection panel
					chapterSelect.gameObject.SetActive(false);

					// Open options menu
					optionsMenu.gameObject.SetActive(true);

					// Open plugin config panel
					Transform pluginConfigButton = optionsMenu.transform.Find("Navigation Rail/PluginConfiguratorButton(Clone)");
					if (pluginConfigButton == null)
						pluginConfigButton = optionsMenu.transform.Find("Navigation Rail/PluginConfiguratorButton");

					if (pluginConfigButton == null)
					{
						Plugin.logger.LogError("Angry tried to find the plugin configurator button but failed!");
						return;
					}

					// Two buttons may be highlighted at the same time if the menu is not opened before
					Transform panel = optionsMenu.Find("Navigation Rail");
					if (panel != null && panel.gameObject.TryGetComponent(out ButtonHighlightParent highlightManager))
					{
						if (highlightManager.buttons == null || highlightManager.buttons.Length == 0)
						{
							highlightManager.Start();
							highlightManager.targetOnStart = null;
						}
					}

					// Click the plugin config button and open the main panel of angry
					pluginConfigButton.gameObject.GetComponent<Button>().onClick.Invoke();
					if (PluginConfiguratorController.activePanel != null)
						PluginConfiguratorController.activePanel.SetActive(false);
					PluginConfiguratorController.mainPanel.gameObject.SetActive(false);
					ConfigManager.config.rootPanel.OpenPanelInternally(false);
					ConfigManager.config.rootPanel.currentPanel.rect.normalizedPosition = new Vector2(0, 1);

					// Set the difficulty based on the previously selected act
					AngryDifficultyManager.SetDifficultyFromPrefs();
				});
				ConfigManager.customLevelButtonPosition.TriggerPostValueChangeEvent();
				ConfigManager.customLevelButtonFrameColor.TriggerPostValueChangeEvent();
				ConfigManager.customLevelButtonTextColor.TriggerPostValueChangeEvent();
			}
			else
			{
				Plugin.logger.LogWarning("Angry tried to find chapter select menu, but root canvas was not found!");
			}
		}

		internal static void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				if (SceneHelper.CurrentScene == "Main Menu")
				{
					CreateCustomLevelButtonOnMainMenu();
				}
			};

			ConfigManager.InitializeConfig();

			ConfigManager.customLevelButtonPosition.postValueChangeEvent += (pos) =>
			{
				if (currentCustomLevelButton == null)
					return;

				currentCustomLevelButton.gameObject.SetActive(true);
				switch (pos)
				{
					case CustomLevelButtonPosition.Disabled:
						currentCustomLevelButton.gameObject.SetActive(false);
						break;

					case CustomLevelButtonPosition.Bottom:
						currentCustomLevelButton.transform.localPosition = new Vector3(currentCustomLevelButton.transform.localPosition.x, -303, currentCustomLevelButton.transform.localPosition.z);
						break;

					case CustomLevelButtonPosition.Top:
						currentCustomLevelButton.transform.localPosition = new Vector3(currentCustomLevelButton.transform.localPosition.x, 192, currentCustomLevelButton.transform.localPosition.z);
						break;
				}

				if (bossRushButton != null)
				{
					if (pos == CustomLevelButtonPosition.Bottom)
					{
						currentCustomLevelButton.rect.sizeDelta = new Vector2((380f - 5) / 2, 50);
						currentCustomLevelButton.transform.localPosition = new Vector3((380f + 5) / -4, currentCustomLevelButton.transform.localPosition.y, currentCustomLevelButton.transform.localPosition.z);

						bossRushButton.sizeDelta = new Vector2((380f - 5) / 2, 50);
						bossRushButton.transform.localPosition = new Vector3((380f + 5) / 4, -303, 0);
					}
					else
					{
						currentCustomLevelButton.rect.sizeDelta = new Vector2(380, 50);
						currentCustomLevelButton.transform.localPosition = new Vector3(0, currentCustomLevelButton.transform.localPosition.y, currentCustomLevelButton.transform.localPosition.z);

						bossRushButton.sizeDelta = new Vector2(380, 50);
						bossRushButton.transform.localPosition = new Vector3(0, -303, 0);
					}
				}
			};
		
			ConfigManager.customLevelButtonFrameColor.postValueChangeEvent += (clr) =>
			{
				if (currentCustomLevelButton == null)
					return;

				ColorBlock block = new ColorBlock();
				block.colorMultiplier = 1f;
				block.fadeDuration = 0.1f;
				block.normalColor = clr;
				block.selectedColor = clr * 0.8f;
				block.highlightedColor = clr * 0.8f;
				block.pressedColor = clr * 0.5f;
				block.disabledColor = Color.gray;

				currentCustomLevelButton.button.colors = block;
			};
		
			ConfigManager.customLevelButtonTextColor.postValueChangeEvent += (clr) =>
			{
				if (currentCustomLevelButton == null)
					return;

				currentCustomLevelButton.text.color = clr;
			};
		}
	}
}
