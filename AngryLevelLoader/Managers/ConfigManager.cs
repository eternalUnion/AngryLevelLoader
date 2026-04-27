using AngryLevelLoader.Fields;
using AngryLevelLoader.Notifications;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace AngryLevelLoader.Managers
{
	internal static class ConfigManager
	{
		public enum CustomLevelButtonPosition
		{
			Top,
			Bottom,
			Disabled
		}

		public enum BundleSorting
		{
			Alphabetically,
			Author,
			LastPlayed,
			LastUpdate
		}

		public enum DefaultLeaderboardCategory
		{
			All,
			PRank,
			Challenge,
			Nomo,
			Nomow
		}

		public enum DefaultLeaderboardDifficulty
		{
			Any,
			Harmless,
			Lenient,
			Standard,
			Violent,
			Brutal,
		}

		public enum DefaultLeaderboardFilter
		{
			Global,
			Friends,
		}

		public static PluginConfigurator config;

		// Difficulty and gamemode select
		public static DifficultyField difficultyField;
		

		// Main panel
		public static ConfigHeader levelUpdateNotifier;
		public static ConfigHeader newLevelNotifier;
		public static StringField newLevelNotifierLevels;
		public static BoolField newLevelToggle;
		public static ConfigHeader errorText;
		public static ConfigHeader levelBundlesHeader;
		public static SearchBarField searchBar;
		public static ConfigDivision folderDivision;
		public static ConfigDivision bundleDivision;
		public static ConfigHeader searchInfo;
		public static ConfigDivision leaderboardsDivision;
		public static ConfigPanel bannedModsPanel;
		public static ConfigHeader bannedModsText;
		public static ConfigPanel pendingRecords;
		public static ButtonField sendPendingRecords;
		public static ConfigHeader pendingRecordsStatus;
		public static ConfigHeader pendingRecordsInfo;

		// Settings panel
		public static ButtonField changelogButton;
		public static ButtonArrayField openButtons;
		public static KeyCodeField reloadFileKeybind;
		public static KeyCodeField reloadScriptKeybind;
		public static EnumField<CustomLevelButtonPosition> customLevelButtonPosition;
		public static ColorField customLevelButtonFrameColor;
		public static ColorField customLevelButtonTextColor;
		public static BoolField refreshCatalogOnBoot;
		public static BoolField checkForUpdates;
		public static BoolField levelUpdateNotifierToggle;
		public static BoolField levelUpdateIgnoreCustomBuilds;
		public static BoolField newLevelNotifierToggle;
		public static List<string> scriptCertificateIgnore = new List<string>();
		public static StringMultilineField scriptCertificateIgnoreField;
		public static BoolField useDevelopmentBranch;
		public static BoolField useLocalServer;
		public static BoolField scriptUpdateIgnoreCustom;
		public static EnumField<BundleSorting> bundleSortingMode;

		public static BoolField showLeaderboardOnLevelEnd;
		public static BoolField showLeaderboardOnSecretLevelEnd;
		public static EnumField<DefaultLeaderboardCategory> defaultLeaderboardCategory;
		public static EnumField<DefaultLeaderboardDifficulty> defaultLeaderboardDifficulty;
		public static EnumField<DefaultLeaderboardFilter> defaultLeaderboardFilter;

		// Set every fields' interactable field to false
		// Used by move data process to force a restart
		internal static void DisableAllConfig()
		{
			Stack<ConfigField> toProcess = new Stack<ConfigField>(config.rootPanel.GetAllFields());

			while (toProcess.Count != 0)
			{
				ConfigField field = toProcess.Pop();

				if (field is ConfigPanel concretePanel)
				{
					foreach (var subField in concretePanel.GetAllFields())
						toProcess.Push(subField);
				}

				field.interactable = false;
			}
		}

		public static void InitializeConfig()
		{
			if (config != null)
				return;

			// Some fields are bridged from the internal config
			InternalConfigManager.InitializeInternalConfig();

			config = PluginConfigurator.Create("Angry Level Loader", Plugin.PLUGIN_GUID);
			config.postPresetChangeEvent += (b, a) => Plugin.UpdateAllUI();
			config.SetIconWithURL("file://" + Path.Combine(Plugin.workingDir, "plugin-icon.png"));

			newLevelToggle = new BoolField(config.rootPanel, "", "v_newLevelToggle", false);
			newLevelToggle.hidden = true;
			
			// When the panel is opened, report new levels
			config.rootPanel.onPannelOpenEvent += (external) =>
			{
				if (newLevelToggle.value)
				{
					newLevelNotifier.text = string.Join("\n", newLevelNotifierLevels.value.Split('`').Where(level => !string.IsNullOrEmpty(level)).Select(name => $"<color=#00FF00>New level: {name}</color>"));
					newLevelNotifier.hidden = false;
					newLevelNotifierLevels.value = "";
				}

				newLevelToggle.value = false;
			};

			newLevelNotifier = new ConfigHeader(config.rootPanel, "<color=#00FF00>New levels are available!</color>", 16);
			newLevelNotifier.hidden = true;
			levelUpdateNotifier = new ConfigHeader(config.rootPanel, "<color=#00FF00>Level updates available!</color>", 16);
			levelUpdateNotifier.hidden = true;

			OnlineLevelsUI.onlineLevelsPanel = new ConfigPanel(InternalConfigManager.internalConfig.rootPanel, "Online Levels", "b_onlineLevels", ConfigPanel.PanelFieldType.StandardWithIcon);
			new ConfigBridge(OnlineLevelsUI.onlineLevelsPanel, config.rootPanel);
			OnlineLevelsUI.onlineLevelsPanel.SetIconWithURL("file://" + Path.Combine(Plugin.workingDir, "online-icon.png"));
			OnlineLevelsUI.onlineLevelsPanel.onPannelOpenEvent += (e) =>
			{
				newLevelNotifier.hidden = true;
			};
			
			OnlineLevelsUI.Init();

			leaderboardsDivision = new ConfigDivision(config.rootPanel, "leaderboardsDivision");
			leaderboardsDivision.hidden = !InternalConfigManager.leaderboardToggle.value;
			
			bannedModsPanel = new ConfigPanel(leaderboardsDivision, "Leaderboard banned mods", "bannedModsPanel", ConfigPanel.PanelFieldType.StandardWithIcon);
			bannedModsPanel.SetIconWithURL("file://" + Path.Combine(Plugin.workingDir, "banned-mods-icon.png"));
			bannedModsPanel.hidden = true;
			
			bannedModsText = new ConfigHeader(bannedModsPanel, "", 24, TMPro.TextAlignmentOptions.Left);
			
			pendingRecords = new ConfigPanel(leaderboardsDivision, "Pending records", "pendingRecords", ConfigPanel.PanelFieldType.StandardWithIcon);
			pendingRecords.SetIconWithURL("file://" + Path.Combine(Plugin.workingDir, "pending.png"));
			
			sendPendingRecords = new ButtonField(pendingRecords, "Send Pending Records", "sendPendingRecordsButton");
			sendPendingRecords.onClick += PendingRecordsManager.ProcessPendingRecords;
			
			pendingRecordsStatus = new ConfigHeader(pendingRecords, "", 20, TMPro.TextAlignmentOptions.Left);
			new ConfigSpace(pendingRecords, 5f);
			pendingRecordsInfo = new ConfigHeader(pendingRecords, "", 18, TMPro.TextAlignmentOptions.Left);
			PendingRecordsManager.UpdatePendingRecordsUI();

			difficultyField = new DifficultyField(config.rootPanel);

			bundleSortingMode = new EnumField<BundleSorting>(InternalConfigManager.internalConfig.rootPanel, "Bundle sorting", "s_bundleSortingMode", BundleSorting.LastPlayed);
			bundleSortingMode.onValueChange += (e) =>
			{
				bundleSortingMode.value = e.value;
				Plugin.SortBundles();
			};
			bundleSortingMode.SetEnumDisplayName(BundleSorting.LastPlayed, "Last Played");
			bundleSortingMode.SetEnumDisplayName(BundleSorting.LastUpdate, "Last Update");
			new ConfigBridge(bundleSortingMode, config.rootPanel);

			ConfigHeader difficultyOverrideWarning = new ConfigHeader(config.rootPanel, "Difficulty is overridden by gamemode\nWarning: Some levels may not be compatible with gamemodes", 18);
			difficultyOverrideWarning.textColor = Color.yellow;
			difficultyOverrideWarning.hidden = true;

			difficultyField.postDifficultyChange += (difficultyName, difficultyIndex) =>
			{
				AngryDifficulty selectedDifficulty = AngryDifficultyManager.Difficulties.Where(d => d.name == difficultyName).FirstOrDefault();
				if (selectedDifficulty == null)
					selectedDifficulty = AngryDifficultyManager.VIOLENT;

				AngryDifficultyManager.SelectedDifficulty = selectedDifficulty;

				if (difficultyField.gamemodeListValueIndex == 1 || difficultyField.gamemodeListValueIndex == 2)
				{
					difficultyOverrideWarning.hidden = false;
					difficultyField.difficultyInteractable = false;
					difficultyField.ForceSetDifficultyUI(0);
					AngryDifficultyManager.SelectedDifficulty = AngryDifficultyManager.HARMLESS;
				}
				else
				{
					difficultyOverrideWarning.hidden = true;
					difficultyField.difficultyInteractable = true;

					difficultyIndex = AngryDifficultyManager.Difficulties.IndexOf(selectedDifficulty);
					if (difficultyIndex == -1)
					{
						selectedDifficulty = AngryDifficultyManager.VIOLENT;
						difficultyIndex = AngryDifficultyManager.Difficulties.IndexOf(selectedDifficulty);
					}

					difficultyField.ForceSetDifficultyUI(difficultyIndex);
				}
			};
			difficultyField.postGamemodeChange += (gamemodeName, gamemodeIndex) =>
			{
				difficultyField.TriggerPostDifficultyChangeEvent();
			};
			config.rootPanel.onPannelOpenEvent += (externally) =>
			{
				difficultyField.TriggerPostDifficultyChangeEvent();
			};
			difficultyField.TriggerPostDifficultyChangeEvent();

			ConfigPanel settingsPanel = new ConfigPanel(InternalConfigManager.internalConfig.rootPanel, "Settings", "p_settings", ConfigPanel.PanelFieldType.Standard);
			new ConfigBridge(settingsPanel, config.rootPanel);
			settingsPanel.hidden = true;

			// Settings panel
			changelogButton = new ButtonField(settingsPanel, "Changelog", "changelogButton");
			changelogButton.onClick += () => {
				openButtons.SetButtonInteractable(1, false);
				_ = PluginUpdateHandler.CheckPluginUpdate();
			};
			
			openButtons = new ButtonArrayField(settingsPanel, "settingButtons", 2, new float[] { 0.5f, 0.5f }, new string[] { "Open Levels Folder", "Open Scripts Folder" });
			openButtons.OnClickEventHandler(0).onClick += () => Application.OpenURL(Plugin.levelsPath);
			openButtons.OnClickEventHandler(1).onClick += () => Application.OpenURL(AngryPaths.ScriptsPath);

			reloadFileKeybind = new KeyCodeField(settingsPanel, "Reload File", "f_reloadFile", KeyCode.None);
			reloadFileKeybind.onValueChange += (e) =>
			{
				if (e.value == KeyCode.Mouse0 || e.value == KeyCode.Mouse1 || e.value == KeyCode.Mouse2)
					e.canceled = true;
			};

			reloadScriptKeybind = new KeyCodeField(settingsPanel, "Reload Script", "f_reloadScript", KeyCode.None);
			reloadScriptKeybind.onValueChange += (e) =>
			{
				if (e.value == KeyCode.Mouse0 || e.value == KeyCode.Mouse1 || e.value == KeyCode.Mouse2)
					e.canceled = true;
			};

			new ConfigHeader(settingsPanel, "User Interface") { textColor = new Color(1f, 0.504717f, 0.9454f) };

			customLevelButtonPosition = new EnumField<CustomLevelButtonPosition>(settingsPanel, "Custom level button position", "s_customLevelButtonPosition", CustomLevelButtonPosition.Bottom);

			ConfigPanel customLevelButtonPanel = new ConfigPanel(settingsPanel, "Custom level button colors", "customLevelButtonPanel");
			customLevelButtonFrameColor = new ColorField(customLevelButtonPanel, "Custom level button frame color", "s_customLevelButtonFrameColor", Color.white);
			customLevelButtonTextColor = new ColorField(customLevelButtonPanel, "Custom level button text color", "s_customLevelButtonTextColor", Color.white);

			new ConfigHeader(settingsPanel, "Leaderboards") { textColor = new Color(1f, 0.692924f, 0.291f) };
			
			new ConfigBridge(InternalConfigManager.leaderboardToggle, settingsPanel);

			showLeaderboardOnLevelEnd = new BoolField(settingsPanel, "Show leaderboard on level end", "showLeaderboardOnLevelEnd", true);
			showLeaderboardOnSecretLevelEnd = new BoolField(settingsPanel, "Show leaderboard on secret level end", "showLeaderboardOnSecretLevelEnd", true);
			
			new SpaceField(settingsPanel, 5);
			defaultLeaderboardCategory = new EnumField<DefaultLeaderboardCategory>(settingsPanel, "Default leaderboard category", "defaultLeaderboardCategory", DefaultLeaderboardCategory.All);
			defaultLeaderboardCategory.SetEnumDisplayName(DefaultLeaderboardCategory.PRank, "P Rank");
			defaultLeaderboardCategory.SetEnumDisplayName(DefaultLeaderboardCategory.Nomo, "No Monsters");
			defaultLeaderboardCategory.SetEnumDisplayName(DefaultLeaderboardCategory.Nomow, "No Monsters/Weapons");
			
			defaultLeaderboardDifficulty = new EnumField<DefaultLeaderboardDifficulty>(settingsPanel, "Default leaderboard difficulty", "defaultLeaderboardDifficulty", DefaultLeaderboardDifficulty.Any);
			
			defaultLeaderboardFilter = new EnumField<DefaultLeaderboardFilter>(settingsPanel, "Default leaderboard filter", "defaultLeaderboardFilter", DefaultLeaderboardFilter.Global);

			new ConfigHeader(settingsPanel, "Online") { textColor = new Color(0.532f, 0.8284001f, 1f) };
			
			refreshCatalogOnBoot = new BoolField(settingsPanel, "Refresh online catalog on boot", "s_refreshCatalogBoot", true);
			
			checkForUpdates = new BoolField(settingsPanel, "Check for updates on boot", "s_checkForUpdates", true);
			
			useDevelopmentBranch = new BoolField(settingsPanel, "Use development chanel", "s_useDevChannel", false);
			
			useLocalServer = new BoolField(settingsPanel, "Use local server", "s_useLocalServer", false);
			
			if (!InternalConfigManager.devMode.value)
			{
				useDevelopmentBranch.hidden = true;
				useDevelopmentBranch.value = false;

				useLocalServer.hidden = true;
				useLocalServer.value = false;
			}

			levelUpdateNotifierToggle = new BoolField(settingsPanel, "Notify on level updates", "s_levelUpdateNofify", true);
			levelUpdateNotifierToggle.onValueChange += (e) =>
			{
				levelUpdateNotifierToggle.value = e.value;
				OnlineLevelsUI.CheckLevelUpdateText();
			};
			
			levelUpdateIgnoreCustomBuilds = new BoolField(settingsPanel, "Ignore updates for custom build", "s_levelUpdateIgnoreCustomBuilds", false);
			levelUpdateIgnoreCustomBuilds.onValueChange += (e) =>
			{
				levelUpdateIgnoreCustomBuilds.value = e.value;
				OnlineLevelsUI.CheckLevelUpdateText();
			};
			
			newLevelNotifierLevels = new StringField(settingsPanel, "h_New levels", "s_newLevelNotifierLevels", "", true);
			newLevelNotifierLevels.hidden = true;
			
			newLevelNotifierToggle = new BoolField(settingsPanel, "Notify on new level release", "s_newLevelNotiftToggle", true);
			newLevelNotifierToggle.onValueChange += (e) =>
			{
				newLevelNotifierToggle.value = e.value;
				if (!e.value)
					newLevelNotifier.hidden = true;
			};

			new ConfigHeader(settingsPanel, "Scripts") { textColor = new Color(0.6248745f, 1f, 0.617f) };
			
			scriptUpdateIgnoreCustom = new BoolField(settingsPanel, "Ignore updates for custom builds", "s_scriptUpdateIgnoreCustom", false);
			
			scriptCertificateIgnoreField = new StringMultilineField(settingsPanel, "Certificate ignore", "s_scriptCertificateIgnore", "", true);
			
			scriptCertificateIgnore = scriptCertificateIgnoreField.value.Split('\n').ToList();

			new SpaceField(settingsPanel, 5);

			new ConfigHeader(settingsPanel, "Danger Zone") { textColor = Color.red };
			
			StringField dataPathInput = new StringField(settingsPanel, "Data Path", "s_dataPathInput", Plugin.dataPath, false, false);
			ButtonField changeDataPath = new ButtonField(settingsPanel, "Move Data", "s_changeDataPath");
			ConfigHeader dataInfo = new ConfigHeader(settingsPanel, "<color=red>RESTART REQUIRED</color>", 18);

			dataInfo.hidden = true;
			
			changeDataPath.onClick += () =>
			{
				string newPath = dataPathInput.value;
				if (newPath == InternalConfigManager.configDataPath.value)
					return;

				if (!Directory.Exists(newPath))
				{
					dataInfo.text = "<color=red>Could not find the directory</color>";
					dataInfo.hidden = false;
					return;
				}

				string newLevelsFolder = Path.Combine(newPath, "Levels");
				AngryIOUtils.TryCreateDirectory(newLevelsFolder);
				foreach (string levelFile in Directory.GetFiles(Plugin.levelsPath))
				{
					string destinationLevelFile = Path.Combine(newLevelsFolder, Path.GetFileName(levelFile));
					if (File.Exists(destinationLevelFile))
					{
						File.Copy(levelFile, destinationLevelFile, true);
						File.Delete(levelFile);
					}
					else
					{
						File.Move(levelFile, destinationLevelFile);
					}
				}
				Directory.Delete(Plugin.levelsPath, true);
				Plugin.levelsPath = newLevelsFolder;

				string newLevelsUnpackedFolder = Path.Combine(newPath, "LevelsUnpacked");
				AngryIOUtils.TryCreateDirectory(newLevelsUnpackedFolder);
				foreach (string unpackedLevelFolder in Directory.GetDirectories(Plugin.tempFolderPath))
				{
					string dest = Path.Combine(newLevelsUnpackedFolder, Path.GetFileName(unpackedLevelFolder));
					if (Directory.Exists(dest))
						Directory.Delete(dest, true);

					AngryIOUtils.DirectoryCopy(unpackedLevelFolder, dest, true, true);
				}
				Directory.Delete(Plugin.tempFolderPath, true);
				Plugin.tempFolderPath = newLevelsUnpackedFolder;

				string newMapVarsFolder = Path.Combine(newPath, "MapVars");
				AngryIOUtils.TryCreateDirectory(newMapVarsFolder);
				foreach (string mapVarPresetFolder in Directory.GetDirectories(Path.Combine(Plugin.dataPath, "MapVars")))
				{
					string dest = Path.Combine(newMapVarsFolder, Path.GetFileName(mapVarPresetFolder));
					if (Directory.Exists(dest))
						Directory.Delete(dest, true);

					AngryIOUtils.DirectoryCopy(mapVarPresetFolder, dest, true, true);
				}
				if (Directory.Exists(Path.Combine(Plugin.dataPath, "MapVars")))
					Directory.Delete(Path.Combine(Plugin.dataPath, "MapVars"), true);

				dataInfo.text = "<color=red>RESTART REQUIRED</color>";
				dataInfo.hidden = false;
				InternalConfigManager.configDataPath.value = newPath;

				DisableAllConfig();
			};

			ButtonField deleteOldBundles = new ButtonField(settingsPanel, "Delete Old Bundles", "s_deleteOldBundles");
			deleteOldBundles.onClick += () =>
			{
				NotificationPanel.Open(new DeleteOldBundlesNotification());
			};

			ButtonArrayField settingsAndReload = new ButtonArrayField(config.rootPanel, "settingsAndReload", 2, new float[] { 0.5f, 0.5f }, new string[] { "Settings", "Scan For Levels" });
			settingsAndReload.OnClickEventHandler(0).onClick += () =>
			{
				settingsPanel.OpenPanel();
			};
			settingsAndReload.OnClickEventHandler(1).onClick += () =>
			{
				Plugin.ScanForLevels();
			};

			errorText = new ConfigHeader(config.rootPanel, "", 16, TMPro.TextAlignmentOptions.Left);

			levelBundlesHeader = new ConfigHeader(config.rootPanel, "Level Bundles");
			searchBar = new SearchBarField(config.rootPanel);
			folderDivision = new ConfigDivision(config.rootPanel, "div_folders");
			bundleDivision = new ConfigDivision(config.rootPanel, "div_bundles");
			searchInfo = new ConfigHeader(config.rootPanel, "", 18);
			searchInfo.textColor = Color.gray;
			searchInfo.hidden = true;
		}

		public static void InitializeErrorConfig(string text, Exception ex)
		{
			if (config == null)
			{
				config = PluginConfigurator.Create("Angry Level Loader", Plugin.PLUGIN_GUID);
				config.SetIconWithURL("file://" + Path.Combine(Plugin.workingDir, "plugin-icon.png"));
			}
			else
			{
				foreach (var field in config.rootPanel.GetAllFields())
				{
					field.hidden = true;
				}
			}

			ButtonField bugReport = new ButtonField(config.rootPanel, "GitHub bug report", "error_bugReport");
			bugReport.onClick += () =>
			{
				Application.OpenURL("https://github.com/eternalUnion/AngryLevelLoader/issues");
			};

			ConfigHeader errorHeader = new ConfigHeader(config.rootPanel, $"Error! Failed to create plugin's data folder at '{Plugin.dataPath}'", 16, TMPro.TextAlignmentOptions.Left);
			errorHeader.textColor = Color.red;

			if (ex != null)
			{
				ConfigHeader exceptionHeader = new ConfigHeader(config.rootPanel, $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", 16, TMPro.TextAlignmentOptions.Left);
			}
		}
	}
}