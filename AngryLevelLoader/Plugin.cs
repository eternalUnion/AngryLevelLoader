using BepInEx;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Audio;
using RudeLevelScript;
using PluginConfig;
using BepInEx.Bootstrap;
using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Fields;
using PluginConfiguratorComponents;
using System.Text;
using AngryLevelLoader.Managers.ServerManager;
using UnityEngine.UI;
using AngryUiComponents;
using Unity.Audio;
using BepInEx.Logging;
using AngryLevelLoader.Managers.BannedMods;
using Newtonsoft.Json;
using System.Threading.Tasks;
using static AngryLevelLoader.Managers.ServerManager.AngryLeaderboards;
using AngryLevelLoader.Notifications;
using AngryLevelLoader.Managers.LegacyPatches;

namespace AngryLevelLoader
{
    public class SpaceField : CustomConfigField
    {
        public SpaceField(ConfigPanel parentPanel, float space) : base(parentPanel, 60, space)
        {

        }
    }

	[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
	[BepInDependency(PluginConfiguratorController.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(Ultrapain.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.heaven.orhell", BepInDependency.DependencyFlags.SoftDependency)]
	// Soft ban dependencies
	[BepInDependency(BannedModsManager.HYDRA_LIB_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(DualWieldPunchesSoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(UltraTweakerSoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(MovementPlusSoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(UltraCoinsSoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(UltraFunGunsSoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(FasterPunchSoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(AtlasWeaponsSoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(WipFixHardBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(MasqueradeDivinitySoftBan.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Plugin : BaseUnityPlugin
	{
        public const string PLUGIN_NAME = "AngryLevelLoader";
        public const string PLUGIN_GUID = "com.eternalUnion.angryLevelLoader";
        public const string PLUGIN_VERSION = "2.7.3";

		public const string PLUGIN_CONFIG_MIN_VERSION = "1.8.0";

		public static readonly Vector3 defaultGravity = new Vector3(0, -40, 0);

		public static string workingDir;
		// This is the path addressable remote load path uses
		// {AngryLevelLoader.Plugin.tempFolderPath}\\{guid}
		public static string tempFolderPath;
		public static string dataPath;
        public static string levelsPath;

		// This is the path angry addressables use
		public static string angryCatalogPath;

        public static Plugin instance;
		public static ManualLogSource logger;
		
		public static PluginConfigurator internalConfig;
		public static BoolField devMode;
		public static StringField lastVersion;
		public static StringField updateLastVersion;
		public static BoolField ignoreUpdates;
		public static StringField configDataPath;
		public static BoolField leaderboardToggle;
		public static BoolField askedPermissionForLeaderboards;
		public static BoolField showLeaderboardOnLevelEnd;
		public static BoolField showLeaderboardOnSecretLevelEnd;
		public static StringField pendingRecordsField;

		public static bool ultrapainLoaded = false;
		public static bool heavenOrHellLoaded = false;

		public static Dictionary<string, RudeLevelData> idDictionary = new Dictionary<string, RudeLevelData>();
		public static Dictionary<string, AngryBundleContainer> angryBundles = new Dictionary<string, AngryBundleContainer>();

		// System which tracks when a bundle was played last in unix time
		public static Dictionary<string, long> lastPlayed = new Dictionary<string, long>();
		public static void LoadLastPlayedMap()
		{
			lastPlayed.Clear();

			string path = AngryPaths.LastPlayedMapPath;
			if (!File.Exists(path))
				return;

			using (StreamReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read)))
			{
				while (!reader.EndOfStream)
				{
					string key = reader.ReadLine();
					if (reader.EndOfStream)
					{
						logger.LogWarning("Invalid end of last played map file");
						break;
					}

					string value = reader.ReadLine();
					if (long.TryParse(value, out long seconds))
					{
						lastPlayed[key] = seconds;
					}
					else
					{
						logger.LogInfo($"Invalid last played time '{value}'");
					}
				}
			}
		}

		public static void UpdateLastPlayed(AngryBundleContainer bundle)
		{
			string guid = bundle.bundleData.bundleGuid;
			if (guid.Length != 32)
				return;

			if (bundleSortingMode.value == BundleSorting.LastPlayed)
				bundle.rootPanel.siblingIndex = 0;
			long secondsNow = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			lastPlayed[guid] = secondsNow;

			string path = AngryPaths.LastPlayedMapPath;
            IOUtils.TryCreateDirectoryForFile(path);
            using (StreamWriter writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)))
			{
				writer.BaseStream.Seek(0, SeekOrigin.Begin);
				writer.BaseStream.SetLength(0);
				foreach (var pair in lastPlayed)
				{
					writer.WriteLine(pair.Key);
					writer.WriteLine(pair.Value.ToString());
				}
			}
		}

		public static Dictionary<string, long> lastUpdate = new Dictionary<string, long>();
		public static void LoadLastUpdateMap()
		{
			lastUpdate.Clear();

			string path = AngryPaths.LastUpdateMapPath;
			if (!File.Exists(path))
				return;

			using (StreamReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read)))
			{
				while (!reader.EndOfStream)
				{
					string key = reader.ReadLine();
					if (reader.EndOfStream)
					{
						logger.LogWarning("Invalid end of last played map file");
						break;
					}

					string value = reader.ReadLine();
					if (long.TryParse(value, out long seconds))
					{
						lastUpdate[key] = seconds;
					}
					else
					{
						logger.LogInfo($"Invalid last played time '{value}'");
					}
				}
			}
		}

		public static void UpdateLastUpdate(AngryBundleContainer bundle)
		{
			string guid = bundle.bundleData.bundleGuid;
			if (guid.Length != 32)
				return;

			if (bundleSortingMode.value == BundleSorting.LastUpdate)
				bundle.rootPanel.siblingIndex = 0;
			long secondsNow = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			lastUpdate[guid] = secondsNow;

			string path = AngryPaths.LastUpdateMapPath;
			IOUtils.TryCreateDirectoryForFile(path);
			using (StreamWriter writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)))
			{
				writer.BaseStream.Seek(0, SeekOrigin.Begin);
				writer.BaseStream.SetLength(0);
				foreach (var pair in lastUpdate)
				{
					writer.WriteLine(pair.Key);
					writer.WriteLine(pair.Value.ToString());
				}
			}
		}

		public static AngryBundleContainer GetAngryBundleByGuid(string guid)
		{
			return angryBundles.Values.Where(bundle => bundle.bundleData.bundleGuid == guid).FirstOrDefault();
		}

		public static LevelContainer GetLevel(string id)
		{
			foreach (AngryBundleContainer container in angryBundles.Values)
			{
				foreach (LevelContainer level in container.levels.Values)
				{
					if (level.field.data.uniqueIdentifier == id)
						return level;
				}
			}

			return null;
		}

		public static void ProcessPath(string path)
		{
            if (AngryFileUtils.TryGetAngryBundleData(path, out AngryBundleData data, out Exception error))
			{
                if (angryBundles.TryGetValue(data.bundleGuid, out AngryBundleContainer bundle))
                {
					// Duplicate file check
					if (File.Exists(bundle.pathToAngryBundle) && !IOUtils.PathEquals(path, bundle.pathToAngryBundle))
					{
						logger.LogError($"Duplicate angry files. Original: {Path.GetFileName(bundle.pathToAngryBundle)}. Duplicate: {Path.GetFileName(path)}");

						if (!string.IsNullOrEmpty(errorText.text))
							errorText.text += '\n';
						errorText.text += $"<color=red>Error loading {Path.GetFileName(path)}</color> Duplicate file, original is {Path.GetFileName(bundle.pathToAngryBundle)}";

						return;
					}

					bool newFile = !IOUtils.PathEquals(bundle.pathToAngryBundle, path);
                    bundle.pathToAngryBundle = path;
                    bundle.rootPanel.interactable = true;
                    bundle.rootPanel.hidden = false;

                    if (newFile)
                        bundle.UpdateScenes(false, false);

                    return;
                }

                AngryBundleContainer newBundle = new AngryBundleContainer(path, data);
                angryBundles[data.bundleGuid] = newBundle;
                newBundle.UpdateOrder();

                try
                {
                    // If rank score is not cached (invalid value) do not lazy load and calculate rank data
                    if (newBundle.finalRankScore.value < 0)
                    {
						logger.LogWarning("Final rank score for the bundle not cached, skipping lazy reload");
                        newBundle.UpdateScenes(false, false);
                    }
                    else
                    {
                        newBundle.UpdateScenes(false, true);
                    }
                }
                catch (Exception e)
                {
					logger.LogWarning($"Exception thrown while loading level bundle: {e}");
                    if (!string.IsNullOrEmpty(errorText.text))
                        errorText.text += '\n';
                    errorText.text += $"<color=red>Error loading {Path.GetFileNameWithoutExtension(path)}</color>. Check the logs for more information";
                }
            }
			else
			{
                if (AngryFileUtils.IsV1LegacyFile(path))
                {
                    if (!string.IsNullOrEmpty(errorText.text))
                        errorText.text += '\n';
                    errorText.text += $"<color=yellow>{Path.GetFileName(path)} is a V1 legacy file. Support for legacy files were dropped after 2.5.0</color>";
                }
                else
                {
					logger.LogError($"Could not load the bundle at {path}\n{error}");

                    if (!string.IsNullOrEmpty(errorText.text))
                        errorText.text += '\n';
                    errorText.text += $"<color=yellow>Failed to load {Path.GetFileNameWithoutExtension(path)}</color>";
                }

                return;
            }
        }

		// This does NOT reload the files, only
		// loads newly added angry levels
		public static void ScanForLevels()
        {
            errorText.text = "";
            if (!Directory.Exists(levelsPath))
            {
				logger.LogWarning("Could not find the Levels folder at " + levelsPath);
				errorText.text = "<color=red>Error: </color>Levels folder not found";
				return;
            }

			foreach (string path in Directory.GetFiles(levelsPath))
			{
				ProcessPath(path);
			}

			OnlineLevelsManager.UpdateUI();
		}

		public static void SortBundles()
		{
			int i = 0;
			if (bundleSortingMode.value == BundleSorting.Alphabetically)
			{
				foreach (var bundle in angryBundles.Values.OrderBy(b => b.bundleData.bundleName))
					bundle.rootPanel.siblingIndex = i++;
			}
			else if (bundleSortingMode.value == BundleSorting.Author)
			{
				foreach (var bundle in angryBundles.Values.OrderBy(b => b.bundleData.bundleAuthor))
					bundle.rootPanel.siblingIndex = i++;
			}
			else if (bundleSortingMode.value == BundleSorting.LastPlayed)
			{
				foreach (var bundle in angryBundles.Values.OrderByDescending((b) => {
					if (lastPlayed.TryGetValue(b.bundleData.bundleGuid, out long time))
						return time;
					return 0;
				}))
				{
					bundle.rootPanel.siblingIndex = i++;
				}
			}
			else if (bundleSortingMode.value == BundleSorting.LastUpdate)
			{
				foreach (var bundle in angryBundles.Values.OrderByDescending((b) => {
					if (lastUpdate.TryGetValue(b.bundleData.bundleGuid, out long time))
						return time;
					return 0;
				}))
				{
					bundle.rootPanel.siblingIndex = i++;
				}
			}
		}

		public static void UpdateAllUI()
		{
			foreach (AngryBundleContainer angryBundle in  angryBundles.Values)
			{
				if (angryBundle.finalRankScore.value < 0)
					angryBundle.UpdateScenes(false, false);
				else
					angryBundle.UpdateFinalRankUI();

                foreach (LevelContainer level in angryBundle.levels.Values)
				{
					level.UpdateUI();
				}
			}
		}

        public static bool LoadEssentialScripts()
        {
			bool loaded = true;

			var res = ScriptManager.AttemptLoadScriptWithCertificate("AngryLoaderAPI.dll");
			if (res == ScriptManager.LoadScriptResult.NotFound)
			{
				logger.LogError("Required script AngryLoaderAPI.dll not found");
				loaded = false;
			}
			else
			{
				ScriptManager.ForceLoadScript("AngryLoaderAPI.dll");
			}

			res = ScriptManager.AttemptLoadScriptWithCertificate("RudeLevelScripts.dll");
			if (res == ScriptManager.LoadScriptResult.NotFound)
			{
				logger.LogError("Required script RudeLevelScripts.dll not found");
				loaded = false;
			}
			else
			{
				ScriptManager.ForceLoadScript("RudeLevelScripts.dll");
			}

			return loaded;
		}

		// Defaults to violent
        public static int selectedDifficulty = 3;
		public static DifficultyField difficultyField;
		internal static List<string> difficultyList = new List<string> { "HARMLESS", "LENIENT", "STANDARD", "VIOLENT" };
		internal static List<string> gamemodeList = new List<string> { "None", "No Monsters", "No Monsters/Weapons" };

		public static bool NoMonsters => difficultyField.gamemodeListValueIndex == 1 || difficultyField.gamemodeListValueIndex == 2;
		public static bool NoWeapons => difficultyField.gamemodeListValueIndex == 2;

		public static Harmony harmony;

		#region Config Fields
		// Main panel
		public static PluginConfigurator config;
		public static ConfigHeader levelUpdateNotifier;
		public static ConfigHeader newLevelNotifier;
		public static StringField newLevelNotifierLevels;
		public static BoolField newLevelToggle;
        public static ConfigHeader errorText;
		public static ConfigDivision bundleDivision;
		public static ConfigDivision leaderboardsDivision;
		public static ConfigPanel bannedModsPanel;
		public static ConfigHeader bannedModsText;
		public static ConfigPanel pendingRecords;
		public static ButtonField sendPendingRecords;
		public static ConfigHeader pendingRecordsStatus;
		public static ConfigHeader pendingRecordsInfo;

		// Settings panel
		public static ButtonArrayField openButtons;
		public static KeyCodeField reloadFileKeybind;
		public enum CustomLevelButtonPosition
		{
			Top,
			Bottom,
			Disabled
		}
		public static EnumField<CustomLevelButtonPosition> customLevelButtonPosition;
		public static ColorField customLevelButtonFrameColor;
		public static ColorField customLevelButtonBackgroundColor;
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
		public enum BundleSorting
		{
			Alphabetically,
			Author,
			LastPlayed,
			LastUpdate
		}
		public static EnumField<BundleSorting> bundleSortingMode;
		public enum DefaultLeaderboardCategory
		{
			All,
			PRank,
			Challenge,
			Nomo,
			Nomow
		}
		public static EnumField<DefaultLeaderboardCategory> defaultLeaderboardCategory;
		public enum DefaultLeaderboardDifficulty
		{
			Any,
			Harmless,
			Lenient,
			Standard,
			Violent,
		}
		public static EnumField<DefaultLeaderboardDifficulty> defaultLeaderboardDifficulty;
		public enum DefaultLeaderboardFilter
		{
			Global,
			Friends,
		}
		public static EnumField<DefaultLeaderboardFilter> defaultLeaderboardFilter;

		// Developer panel

		#endregion

		// Set every fields' interactable field to false
		// Used by move data process to force a restart
		private static void DisableAllConfig()
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

		// Delayed refresh online catalog on boot
		private static void RefreshCatalogOnMainMenu(Scene newScene, LoadSceneMode mode)
		{
			if (SceneHelper.CurrentScene != "Main Menu")
				return;

			if (refreshCatalogOnBoot.value)
				OnlineLevelsManager.RefreshAsync();

			SceneManager.sceneLoaded -= RefreshCatalogOnMainMenu;
		}

		// Is ultrapain difficulty enabled?
		private static bool GetUltrapainDifficultySet()
		{
			return Ultrapain.Plugin.ultrapainDifficulty;
		}

		// Is Heaven or Hell difficulty enabled?
		private static bool GetHeavenOrHellDifficultySet()
		{
			return MyCoolMod.Plugin.isHeavenOrHell;
		}

		// Create the shortcut in chapters menu
		private const string CUSTOM_LEVEL_BUTTON_ASSET_PATH = "AngryLevelLoader/UI/CustomLevels.prefab";
		private static AngryCustomLevelButtonComponent currentCustomLevelButton;
		private static RectTransform bossRushButton;
		private static void CreateCustomLevelButtonOnMainMenu()
		{
			GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvasObj == null)
			{
				logger.LogWarning("Angry tried to create main menu buttons, but root canvas was not found!");
				return;
			}

			Transform chapterSelect = canvasObj.transform.Find("Chapter Select");
			if (chapterSelect != null)
			{
				GameObject customLevelButtonObj = Addressables.InstantiateAsync(CUSTOM_LEVEL_BUTTON_ASSET_PATH, chapterSelect).WaitForCompletion();
				Transform bossRush = chapterSelect.Find("Boss Rush Button");
				if (bossRush != null)
					bossRushButton = bossRush.gameObject.GetComponent<RectTransform>();
				currentCustomLevelButton = customLevelButtonObj.GetComponent<AngryCustomLevelButtonComponent>();

				currentCustomLevelButton.button.onClick = new Button.ButtonClickedEvent();
				currentCustomLevelButton.button.onClick.AddListener(() =>
				{
					// Disable act selection panel
					chapterSelect.gameObject.SetActive(false);

					// Open the options menu
					Transform optionsMenu = canvasObj.transform.Find("OptionsMenu");
					if (optionsMenu == null)
					{
						logger.LogError("Angry tried to find the options menu but failed!");
						chapterSelect.gameObject.SetActive(true);
						return;
					}
					optionsMenu.gameObject.SetActive(true);

					// Open plugin config panel
					Transform pluginConfigButton = optionsMenu.transform.Find("PluginConfiguratorButton(Clone)");
					if (pluginConfigButton == null)
						pluginConfigButton = optionsMenu.transform.Find("PluginConfiguratorButton");

					if (pluginConfigButton == null)
					{
						logger.LogError("Angry tried to find the plugin configurator button but failed!");
						return;
					}

					// Click the plugin config button and open the main panel of angry
					pluginConfigButton.gameObject.GetComponent<Button>().onClick.Invoke();
					if (PluginConfiguratorController.activePanel != null)
						PluginConfiguratorController.activePanel.SetActive(false);
					PluginConfiguratorController.mainPanel.gameObject.SetActive(false);
					config.rootPanel.OpenPanelInternally(false);
					config.rootPanel.currentPanel.rect.normalizedPosition = new Vector2(0, 1);

					// Set the difficulty based on the previously selected act
					int difficulty = PrefsManager.Instance.GetInt("difficulty", 3);
					switch (difficulty)
					{
						// Stock difficulties
						case 0:
						case 1:
						case 2:
						case 3:
							logger.LogInfo($"Angry setting difficulty to {difficultyList[difficulty]}");
							difficultyField.difficultyListValueIndex = difficulty;
							break;

						// Possibly ultrapain
						case 5:
							if (ultrapainLoaded)
							{
								if (GetUltrapainDifficultySet())
								{
									difficultyField.difficultyListValueIndex = difficultyList.IndexOf("ULTRAPAIN");
								}
								else
								{
									logger.LogWarning("Difficulty was set to UKMD, but angry does not support it. Setting to violent");
									difficultyField.difficultyListValueIndex = 3;
								}
							}
							break;

						// Possibly Heaven or Hell, or invalid difficulty
						default:
							if (heavenOrHellLoaded)
							{
								if (GetHeavenOrHellDifficultySet())
								{
									difficultyField.difficultyListValueIndex = difficultyList.IndexOf("HEAVEN OR HELL");
								}
								else
								{
									logger.LogWarning("Unknown difficulty, defaulting to violent");
									difficultyField.difficultyListValueIndex = 3;
								}
							}
							break;
					}

					difficultyField.TriggerPostDifficultyChangeEvent();
				});

				customLevelButtonPosition.TriggerPostValueChangeEvent();
				customLevelButtonFrameColor.TriggerPostValueChangeEvent();
				customLevelButtonBackgroundColor.TriggerPostValueChangeEvent();
				customLevelButtonTextColor.TriggerPostValueChangeEvent();
			}
			else
			{
				logger.LogWarning("Angry tried to find chapter select menu, but root canvas was not found!");
			}
		}

		// Create the angry canvas
		private const string ANGRY_UI_PANEL_ASSET_PATH = "AngryLevelLoader/UI/AngryUIPanel.prefab";
		public static AngryUIPanelComponent currentPanel;
		private static void CreateAngryUI()
		{
			if (currentPanel != null)
				return;

			GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvasObj == null)
			{
				logger.LogWarning("Angry tried to create main menu buttons, but root canvas was not found!");
				return;
			}

			GameObject panelObj = Addressables.InstantiateAsync(ANGRY_UI_PANEL_ASSET_PATH, canvasObj.transform).WaitForCompletion();
			currentPanel = panelObj.GetComponent<AngryUIPanelComponent>();

			currentPanel.reloadBundlePrompt.MakeTransparent(true);
		}

		internal static FileSystemWatcher watcher;
		private static void InitializeFileWatcher()
		{
			if (watcher != null)
				return;

			watcher = new FileSystemWatcher(levelsPath);
			watcher.SynchronizingObject = CrossThreadInvoker.Instance;
			watcher.Changed += (sender, e) =>
			{
				// Notify the bundle that the file is outdated

				string fullPath = e.FullPath;
				foreach (var bundle in angryBundles.Values)
				{
					if (IOUtils.PathEquals(fullPath, bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {fullPath} was updated, container notified");
						bundle.FileChanged();
						return;
					}
				}
			};
			watcher.Renamed += (sender, e) =>
			{
				// Try to find if a bundle owns the file, then update its file path

				string fullPath = e.FullPath;
				foreach (var bundle in angryBundles.Values)
				{
					if (IOUtils.PathEquals(fullPath, bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {fullPath} was renamed, path updated");
						bundle.pathToAngryBundle = fullPath;
						return;
					}
				}
			};
			watcher.Deleted += (sender, e) =>
			{
				// Try to find if a bundle owns the file, then unlink it

				string fullPath = e.FullPath;
				foreach (var bundle in angryBundles.Values)
				{
					if (IOUtils.PathEquals(fullPath, bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {fullPath} was deleted, unlinked");
						bundle.pathToAngryBundle = "";
						return;
					}
				}
			};
			watcher.Created += (sender, e) =>
			{
				// Try to find a bundle matching the file's guid

				string fullPath = e.FullPath;
				if (!AngryFileUtils.TryGetAngryBundleData(fullPath, out AngryBundleData data, out Exception exp))
					return;

				if (angryBundles.TryGetValue(data.bundleGuid, out AngryBundleContainer bundle))
				{
					if (bundle.bundleData.bundleGuid == data.bundleGuid && !File.Exists(bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {fullPath} was just added, and a container with the same guid had no file linked. Linked, container notified");
						bundle.pathToAngryBundle = fullPath;
						bundle.FileChanged();
						return;
					}
				}
			};

			watcher.Filter = "*";

			watcher.IncludeSubdirectories = false;
			watcher.EnableRaisingEvents = true;
		}

		private static void InitializeConfig()
		{
			if (config != null)
				return;

			config = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
			config.postPresetChangeEvent += (b, a) => UpdateAllUI();
			config.SetIconWithURL("file://" + Path.Combine(workingDir, "plugin-icon.png"));
			newLevelToggle = new BoolField(config.rootPanel, "", "v_newLevelToggle", false);
			newLevelToggle.hidden = true;
			config.rootPanel.onPannelOpenEvent += (external) =>
			{
				if (newLevelToggle.value)
				{
					newLevelNotifier.text = string.Join("\n", newLevelNotifierLevels.value.Split('`').Where(level => !string.IsNullOrEmpty(level)).Select(name => $"<color=lime>New level: {name}</color>"));
					newLevelNotifier.hidden = false;
					newLevelNotifierLevels.value = "";
				}
				newLevelToggle.value = false;
			};

			newLevelNotifier = new ConfigHeader(config.rootPanel, "<color=lime>New levels are available!</color>", 16);
			newLevelNotifier.hidden = true;
			levelUpdateNotifier = new ConfigHeader(config.rootPanel, "<color=lime>Level updates available!</color>", 16);
			levelUpdateNotifier.hidden = true;
			OnlineLevelsManager.onlineLevelsPanel = new ConfigPanel(internalConfig.rootPanel, "Online Levels", "b_onlineLevels", ConfigPanel.PanelFieldType.StandardWithIcon);
			new ConfigBridge(OnlineLevelsManager.onlineLevelsPanel, config.rootPanel);
			OnlineLevelsManager.onlineLevelsPanel.SetIconWithURL("file://" + Path.Combine(workingDir, "online-icon.png"));
			OnlineLevelsManager.onlineLevelsPanel.onPannelOpenEvent += (e) =>
			{
				newLevelNotifier.hidden = true;
			};
			OnlineLevelsManager.Init();
			leaderboardsDivision = new ConfigDivision(config.rootPanel, "leaderboardsDivision");
			leaderboardsDivision.hidden = !leaderboardToggle.value;
			bannedModsPanel = new ConfigPanel(leaderboardsDivision, "Leaderboard banned mods", "bannedModsPanel", ConfigPanel.PanelFieldType.StandardWithIcon);
			bannedModsPanel.SetIconWithURL("file://" + Path.Combine(workingDir, "banned-mods-icon.png"));
			bannedModsPanel.hidden = true;
			bannedModsText = new ConfigHeader(bannedModsPanel, "", 24, TextAnchor.MiddleLeft);
			pendingRecords = new ConfigPanel(leaderboardsDivision, "Pending records", "pendingRecords", ConfigPanel.PanelFieldType.StandardWithIcon);
			pendingRecords.SetIconWithURL("file://" + Path.Combine(workingDir, "pending.png"));
			sendPendingRecords = new ButtonField(pendingRecords, "Send Pending Records", "sendPendingRecordsButton");
			sendPendingRecords.onClick += ProcessPendingRecords;
			pendingRecordsStatus = new ConfigHeader(pendingRecords, "", 20, TextAnchor.MiddleLeft);
			new ConfigSpace(pendingRecords, 5f);
			pendingRecordsInfo = new ConfigHeader(pendingRecords, "", 18, TextAnchor.MiddleLeft);
			UpdatePendingRecordsUI();

			difficultyField = new DifficultyField(config.rootPanel);

			bundleSortingMode = new EnumField<BundleSorting>(internalConfig.rootPanel, "Bundle sorting", "s_bundleSortingMode", BundleSorting.LastPlayed);
			bundleSortingMode.onValueChange += (e) =>
			{
				bundleSortingMode.value = e.value;
				SortBundles();
			};
			bundleSortingMode.SetEnumDisplayName(BundleSorting.LastPlayed, "Last Played");
			bundleSortingMode.SetEnumDisplayName(BundleSorting.LastUpdate, "Last Update");
			new ConfigBridge(bundleSortingMode, config.rootPanel);

			ConfigHeader difficultyOverrideWarning = new ConfigHeader(config.rootPanel, "Difficulty is overridden by gamemode\nWarning: Some levels may not be compatible with gamemodes", 18);
			difficultyOverrideWarning.textColor = Color.yellow;
			difficultyOverrideWarning.hidden = true;

			difficultyField.postDifficultyChange += (difficultyName, difficultyIndex) =>
			{
				selectedDifficulty = Array.IndexOf(difficultyList.ToArray(), difficultyName);
				if (selectedDifficulty == -1)
				{
					logger.LogWarning("Invalid difficulty, setting to violent");
					selectedDifficulty = 3;
					difficultyField.difficultyListValue = "VIOLENT";
				}
				else
				{
					if (difficultyName == "ULTRAPAIN")
						selectedDifficulty = 4;
					else if (difficultyName == "HEAVEN OR HELL")
						selectedDifficulty = 5;
				}

				if (difficultyField.gamemodeListValueIndex == 1 || difficultyField.gamemodeListValueIndex == 2)
				{
					difficultyOverrideWarning.hidden = false;
					difficultyField.difficultyInteractable = false;
					difficultyField.ForceSetDifficultyUI(0);
					selectedDifficulty = 0;
				}
				else
				{
					difficultyOverrideWarning.hidden = true;
					difficultyField.difficultyInteractable = true;
					difficultyField.ForceSetDifficultyUI(selectedDifficulty);
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

			ConfigPanel settingsPanel = new ConfigPanel(internalConfig.rootPanel, "Settings", "p_settings", ConfigPanel.PanelFieldType.Standard);
			new ConfigBridge(settingsPanel, config.rootPanel);
			settingsPanel.hidden = true;

			// Settings panel
			openButtons = new ButtonArrayField(settingsPanel, "settingButtons", 2, new float[] { 0.5f, 0.5f }, new string[] { "Open Levels Folder", "Changelog" });
			openButtons.OnClickEventHandler(0).onClick += () => Application.OpenURL(levelsPath);
			openButtons.OnClickEventHandler(1).onClick += () =>
			{
				openButtons.SetButtonInteractable(1, false);
				_ = PluginUpdateHandler.CheckPluginUpdate();
			};

			reloadFileKeybind = new KeyCodeField(settingsPanel, "Reload File", "f_reloadFile", KeyCode.None);
			reloadFileKeybind.onValueChange += (e) =>
			{
				if (e.value == KeyCode.Mouse0 || e.value == KeyCode.Mouse1 || e.value == KeyCode.Mouse2)
					e.canceled = true;
			};

			new ConfigHeader(settingsPanel, "User Interface") { textColor = new Color(1f, 0.504717f, 0.9454f) };

			customLevelButtonPosition = new EnumField<CustomLevelButtonPosition>(settingsPanel, "Custom level button position", "s_customLevelButtonPosition", CustomLevelButtonPosition.Bottom);
			customLevelButtonPosition.postValueChangeEvent += (pos) =>
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

			ConfigPanel customLevelButtonPanel = new ConfigPanel(settingsPanel, "Custom level button colors", "customLevelButtonPanel");

			customLevelButtonFrameColor = new ColorField(customLevelButtonPanel, "Custom level button frame color", "s_customLevelButtonFrameColor", Color.white);
			customLevelButtonFrameColor.postValueChangeEvent += (clr) =>
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

			customLevelButtonBackgroundColor = new ColorField(customLevelButtonPanel, "Custom level button background color", "s_customLevelButtonBgColor", Color.black);
			customLevelButtonBackgroundColor.postValueChangeEvent += (clr) =>
			{
				if (currentCustomLevelButton == null)
					return;

				currentCustomLevelButton.background.color = clr;
			};

			customLevelButtonTextColor = new ColorField(customLevelButtonPanel, "Custom level button text color", "s_customLevelButtonTextColor", Color.white);
			customLevelButtonTextColor.postValueChangeEvent += (clr) =>
			{
				if (currentCustomLevelButton == null)
					return;

				currentCustomLevelButton.text.color = clr;
			};

			new ConfigHeader(settingsPanel, "Leaderboards") { textColor = new Color(1f, 0.692924f, 0.291f) };
			new ConfigBridge(leaderboardToggle, settingsPanel);
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
			if (!devMode.value)
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
				OnlineLevelsManager.CheckLevelUpdateText();
			};
			levelUpdateIgnoreCustomBuilds = new BoolField(settingsPanel, "Ignore updates for custom build", "s_levelUpdateIgnoreCustomBuilds", false);
			levelUpdateIgnoreCustomBuilds.onValueChange += (e) =>
			{
				levelUpdateIgnoreCustomBuilds.value = e.value;
				OnlineLevelsManager.CheckLevelUpdateText();
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
			StringField dataPathInput = new StringField(settingsPanel, "Data Path", "s_dataPathInput", dataPath, false, false);
			ButtonField changeDataPath = new ButtonField(settingsPanel, "Move Data", "s_changeDataPath");
			ConfigHeader dataInfo = new ConfigHeader(settingsPanel, "<color=red>RESTART REQUIRED</color>", 18);
			dataInfo.hidden = true;
			changeDataPath.onClick += () =>
			{
				string newPath = dataPathInput.value;
				if (newPath == configDataPath.value)
					return;

				if (!Directory.Exists(newPath))
				{
					dataInfo.text = "<color=red>Could not find the directory</color>";
					dataInfo.hidden = false;
					return;
				}

				string newLevelsFolder = Path.Combine(newPath, "Levels");
				IOUtils.TryCreateDirectory(newLevelsFolder);
				foreach (string levelFile in Directory.GetFiles(levelsPath))
				{
					File.Copy(levelFile, Path.Combine(newLevelsFolder, Path.GetFileName(levelFile)), true);
					File.Delete(levelFile);
				}
				Directory.Delete(levelsPath, true);
				levelsPath = newLevelsFolder;

				string newLevelsUnpackedFolder = Path.Combine(newPath, "LevelsUnpacked");
				IOUtils.TryCreateDirectory(newLevelsUnpackedFolder);
				foreach (string unpackedLevelFolder in Directory.GetDirectories(tempFolderPath))
				{
					string dest = Path.Combine(newLevelsUnpackedFolder, Path.GetFileName(unpackedLevelFolder));
					if (Directory.Exists(dest))
						Directory.Delete(dest, true);

					IOUtils.DirectoryCopy(unpackedLevelFolder, dest, true, true);
				}
				Directory.Delete(tempFolderPath, true);
				tempFolderPath = newLevelsUnpackedFolder;

				dataInfo.text = "<color=red>RESTART REQUIRED</color>";
				dataInfo.hidden = false;
				configDataPath.value = newPath;

				DisableAllConfig();
			};

			ButtonArrayField settingsAndReload = new ButtonArrayField(config.rootPanel, "settingsAndReload", 2, new float[] { 0.5f, 0.5f }, new string[] { "Settings", "Scan For Levels" });
			settingsAndReload.OnClickEventHandler(0).onClick += () =>
			{
				settingsPanel.OpenPanel();
			};
			settingsAndReload.OnClickEventHandler(1).onClick += () =>
			{
				ScanForLevels();
			};

			// Developer panel
			ConfigPanel devPanel = new ConfigPanel(config.rootPanel, "Developer Panel", "devPanel", ConfigPanel.PanelFieldType.BigButton);
			if (!devMode.value)
				devPanel.hidden = true;

			new ConfigHeader(devPanel, "Angry Server Interface");
			ConfigHeader output = new ConfigHeader(devPanel, "Output", 18, TextAnchor.MiddleLeft);
			ConfigDivision devDiv = new ConfigDivision(devPanel, "devDiv");
			ButtonField addAllBundles = new ButtonField(devDiv, "Update All Bundles", "updateAllBundles");
			addAllBundles.onClick += async () =>
			{
				devDiv.interactable = false;

				try
				{
					if (OnlineLevelsManager.catalog == null)
					{
						output.text = "Catalog is not loaded";
						return;
					}

					output.text = "<color=grey>Fetching existing bundles...</color>";

					var existingBundles = await AngryAdmin.GetAllLevelInfoTask();
					if (existingBundles.networkError)
					{
						output.text += "\nNetwork error, check connection";
						return;
					}
					if (existingBundles.httpError)
					{
						output.text += "\nHttp error, check server";
						return;
					}
					if (existingBundles.status != AngryAdmin.GetAllLevelInfoStatus.OK)
					{
						output.text += $"\nStatus error: {existingBundles.message}:{existingBundles.status}";
						return;
					}

					output.text += "\n<color=grey>Updating all bundles...</color>";

					foreach (var bundle in OnlineLevelsManager.catalog.Levels)
					{
						output.text += $"\nChecking {bundle.Name}...";
						var existingBundle = existingBundles.response.result.Where(b => b.bundleGuid == bundle.Guid).FirstOrDefault();

						if (existingBundle == null)
						{
							output.text += $"\nMissing, adding to the server";
							output.text += $"\n<color=grey>command: add_bundle {bundle.Guid}</color>";
							AngryAdmin.CommandResult res = await AngryAdmin.SendCommand($"add_bundle {bundle.Guid}");

							if (res.completedSuccessfully && res.status == AngryAdmin.CommandStatus.OK)
							{
								output.text += $"\n{res.response.result}";
							}
							else if (res.networkError)
							{
								output.text += $"\n<color=red>NETWORK ERROR</color> Check conntection";
							}
							else if (res.httpError)
							{
								output.text += $"\n<color=red>HTTP ERROR</color> Check server";
							}
							else
							{
								if (res.response != null)
									output.text += $"\n<color=red>ERROR: </color>{res.message}:{res.status}";
								else
									output.text += $"\n<color=red>ERROR: </color>Encountered unknown error. Status: " + res.status;
							}
						}

						if (existingBundle == null || existingBundle.hash != bundle.Hash)
						{
							output.text += $"\nOut of date hash, updating";
							output.text += $"\n<color=grey>command: update_leaderboard_hash {bundle.Guid} {bundle.Hash}</color>";
							AngryAdmin.CommandResult res = await AngryAdmin.SendCommand($"update_leaderboard_hash {bundle.Guid} {bundle.Hash}");

							if (res.completedSuccessfully && res.status == AngryAdmin.CommandStatus.OK)
							{
								output.text += $"\n{res.response.result}";
							}
							else if (res.networkError)
							{
								output.text += $"\n<color=red>NETWORK ERROR</color> Check conntection";
							}
							else if (res.httpError)
							{
								output.text += $"\n<color=red>HTTP ERROR</color> Check server";
							}
							else
							{
								if (res.response != null)
									output.text += $"\n<color=red>ERROR: </color>{res.message}:{res.status}";
								else
									output.text += $"\n<color=red>ERROR: </color>Encountered unknown error. Status: " + res.status;
							}
						}
					
						AngryBundleContainer container = GetAngryBundleByGuid(bundle.Guid);
						if (container == null)
						{
							output.text += $"\n<color=red>Bundle not installed locally to check levels</color>";
						}
						else if (container.bundleData.buildHash != bundle.Hash)
						{
							output.text += $"\n<color=red>Local level out of date</color>";
						}
						else
						{
							if (container.locator == null)
							{
								if (container.updating)
									await container.UpdateScenes(false, false);
								await container.UpdateScenes(false, false);
							}

							string[] levelIds = container.GetAllLevelData().Select(data => data.uniqueIdentifier).ToArray();
							string[] existingLevels = existingBundle == null ? new string[0] : existingBundle.levels;
							foreach (string levelId in levelIds)
							{
								if (existingLevels.Contains(levelId))
									continue;

								if (levelId.Contains('~'))
								{
									output.text += $"\n<color=red>Level ID '{levelId}' contains ~. Cannot process</color>";
									continue;
								}

								string commandId = levelId.Replace(' ', '~');

								output.text += $"\n<color=grey>command: add_leaderboard {bundle.Guid} {bundle.Hash} {commandId}</color>";
								AngryAdmin.CommandResult res = await AngryAdmin.SendCommand($"add_leaderboard {bundle.Guid} {bundle.Hash} {commandId}");

								if (res.completedSuccessfully && res.status == AngryAdmin.CommandStatus.OK)
								{
									output.text += $"\n{res.response.result}";
								}
								else if (res.networkError)
								{
									output.text += $"\n<color=red>NETWORK ERROR</color> Check conntection";
								}
								else if (res.httpError)
								{
									output.text += $"\n<color=red>HTTP ERROR</color> Check server";
								}
								else
								{
									if (res.response != null)
										output.text += $"\n<color=red>ERROR: </color>{res.message}:{res.status}";
									else
										output.text += $"\n<color=red>ERROR: </color>Encountered unknown error. Status: " + res.status;
								}
							}
						}
					}

					output.text += $"\n<color=lime>done</color>";
				}
				finally
				{
					devDiv.interactable = true;
				}
			};

			errorText = new ConfigHeader(config.rootPanel, "", 16, TextAnchor.UpperLeft); ;

			new ConfigHeader(config.rootPanel, "Level Bundles");
			bundleDivision = new ConfigDivision(config.rootPanel, "div_bundles");
		}

		#region Leaderboards
		public static void CheckForBannedMods()
		{
			if (!AngryLeaderboards.bannedModsListLoaded)
				return;

			bool bannedModsFound = false;
			bannedModsText.text = "";

			string[] bannedModsList = AngryLeaderboards.bannedMods;
			if (bannedModsList == null)
			{
				logger.LogWarning("Banned mods list cannot be fetched from angry servers, using the local list");
				bannedModsList = BannedModsManager.LOCAL_BANNED_MODS_LIST;
			}

			foreach (string plugin in Chainloader.PluginInfos.Keys)
			{
				if (Array.IndexOf(bannedModsList, plugin) == -1)
					continue;

				if (!BannedModsManager.guidToName.TryGetValue(plugin, out string realName))
					realName = plugin;

				// First, check for a soft ban checker
				if (BannedModsManager.checkers.TryGetValue(plugin, out Func<SoftBanCheckResult> checker))
				{
					try
					{
						var result = checker();

						if (result.banned)
						{
							if (!string.IsNullOrEmpty(bannedModsText.text))
								bannedModsText.text += '\n';

							bannedModsText.text += $"<color=red>{realName}</color>\n<size=18>{result.message}</size>\n\n";
							bannedModsFound = true;
						}
					}
					catch (Exception e)
					{
						logger.LogError($"Exception thrown while checking for soft ban for {realName}\n{e}");

						if (!string.IsNullOrEmpty(bannedModsText.text))
							bannedModsText.text += '\n';

						bannedModsText.text += $"<color=red>{realName}</color>\n<size=18>- Encountered an error while checking for the soft ban status, check console</size>\n\n";
						bannedModsFound = true;
					}
				}
				// Failsafe: assume banned
				else
				{
					if (!string.IsNullOrEmpty(bannedModsText.text))
						bannedModsText.text += '\n';

					bannedModsText.text += $"<color=red>{realName}</color>\n<size=18>- Could not find a soft ban check for this mod. Is angry up to date?</size>\n\n";
					bannedModsFound = true;
				}
			}
		
			bannedModsPanel.hidden = !bannedModsFound;
		}

		private class RecordInfoJsonWrapper
		{
			public string category { get; set; }
			public string difficulty { get; set; }
			public string bundleGuid { get; set; }
			public string hash { get; set; }
			public string levelId { get; set; }
			public int time { get; set; }

			public RecordInfoJsonWrapper() { }

			public RecordInfoJsonWrapper(AngryLeaderboards.PostRecordInfo record)
			{
				category = AngryLeaderboards.RECORD_CATEGORY_DICT[record.category];
				difficulty = AngryLeaderboards.RECORD_DIFFICULTY_DICT[record.difficulty];
				bundleGuid = record.bundleGuid;
				hash = record.hash;
				levelId = record.levelId;
				time = record.time;
			}

			public bool TryParseRecordInfo(out AngryLeaderboards.PostRecordInfo record)
			{
				record = new AngryLeaderboards.PostRecordInfo();

				record.category = AngryLeaderboards.RECORD_CATEGORY_DICT.FirstOrDefault(i => i.Value == category).Key;
				if (AngryLeaderboards.RECORD_CATEGORY_DICT[record.category] != category)
					return false;

				record.difficulty = AngryLeaderboards.RECORD_DIFFICULTY_DICT.FirstOrDefault(i => i.Value == difficulty).Key;
				if (AngryLeaderboards.RECORD_DIFFICULTY_DICT[record.difficulty] != difficulty)
					return false;

				record.bundleGuid = bundleGuid;
				record.hash = hash;
				record.levelId = levelId;
				record.time = time;
				return true;
			}
		}

		internal static void AddPendingRecord(AngryLeaderboards.PostRecordInfo record, bool recursiveCall = false)
		{
			if (pendingRecordsTask != null && !pendingRecordsTask.IsCompleted && !recursiveCall)
			{
				pendingRecordsTask.ContinueWith((task) => AddPendingRecord(record, recursiveCall: true), TaskScheduler.FromCurrentSynchronizationContext());
				return;
			}

			List<RecordInfoJsonWrapper> pendingRecordsList;
			try
			{
				pendingRecordsList = JsonConvert.DeserializeObject<List<RecordInfoJsonWrapper>>(pendingRecordsField.value);
				if (pendingRecordsList == null)
					pendingRecordsList = new List<RecordInfoJsonWrapper>();
			}
			catch (Exception ex)
			{
				logger.LogError($"Caught exception while trying to deserialize pending records\n{ex}");
				pendingRecordsField.value = "[]";
				pendingRecordsList = new List<RecordInfoJsonWrapper>();
			}

			pendingRecordsList.Add(new RecordInfoJsonWrapper(record));
			pendingRecordsField.value = JsonConvert.SerializeObject(pendingRecordsList);
			UpdatePendingRecordsUI();
		}

		internal static void UpdatePendingRecordsUI()
		{
			try
			{
				List<RecordInfoJsonWrapper> pendingRecordsList = JsonConvert.DeserializeObject<List<RecordInfoJsonWrapper>>(pendingRecordsField.value);
				if (pendingRecordsList == null)
					pendingRecordsList = new List<RecordInfoJsonWrapper>();

				pendingRecords.hidden = pendingRecordsList.Count == 0;
				pendingRecordsInfo.text = "";

				foreach (var record in pendingRecordsList)
				{
					string bundleName = record.bundleGuid;
					string levelName = record.levelId;
					
					var bundle = GetAngryBundleByGuid(bundleName);
					if (bundle != null)
						bundleName = bundle.bundleData.bundleName;

					var level = GetLevel(levelName);
					if (level != null)
						levelName = level.data.levelName;

					pendingRecordsInfo.text += $"Bundle: <color=grey>{bundleName}</color>\nLevel: <color=grey>{levelName}</color>\nCategory: <color=grey>{record.category}</color>\nDifficulty: <color=grey>{record.difficulty}</color>\nTime: <color=grey>{record.time}</color>\n\n\n";
				}
			}
			catch (Exception ex)
			{
				logger.LogError($"Caught exception while trying to deserialize pending records\n{ex}");
				pendingRecordsField.value = "[]";
				pendingRecords.hidden = true;
			}
		}

		private static Task pendingRecordsTask = null;
		private static async Task ProcessPendingRecordsTask()
		{
			pendingRecordsStatus.text = "";

			List<RecordInfoJsonWrapper> pendingRecordsList;
			try
			{
				pendingRecordsList = JsonConvert.DeserializeObject<List<RecordInfoJsonWrapper>>(pendingRecordsField.value);
				if (pendingRecordsList == null)
					pendingRecordsList = new List<RecordInfoJsonWrapper>();
			}
			catch (Exception ex)
			{
				pendingRecordsStatus.text = $"<color=red>Exception thrown while deserializing pending records, discarding\n\n{ex}</color>";
				pendingRecordsField.value = "[]";
				UpdatePendingRecordsUI();
				return;
			}

			List<RecordInfoJsonWrapper> failedToSend = new List<RecordInfoJsonWrapper>();
			foreach (var record in pendingRecordsList)
			{
				string bundleName = record.bundleGuid;
				string levelName = record.levelId;

				var bundle = GetAngryBundleByGuid(bundleName);
				if (bundle != null)
					bundleName = bundle.bundleData.bundleName;

				var level = GetLevel(levelName);
				if (level != null)
					levelName = level.data.levelName;

				if (!record.TryParseRecordInfo(out AngryLeaderboards.PostRecordInfo parsedRecord))
				{
					pendingRecordsStatus.text += $"<color=red>Failed to parse record info for level {levelName} in bundle {bundleName}. Discarded.</color>\n\n";
					continue;
				}

				pendingRecordsStatus.text += $"Posting record for level <color=grey>{levelName}</color> in bundle <color=grey>{bundleName}</color>...\n";

				var postResult = await PostRecordTask(parsedRecord.category, parsedRecord.difficulty, parsedRecord.bundleGuid, parsedRecord.hash, parsedRecord.levelId, parsedRecord.time);
				if (postResult.completedSuccessfully)
				{
					if (postResult.status == PostRecordStatus.OK)
					{
						pendingRecordsStatus.text += $"<color=lime>Record posted successfully!</color> Ranking: #{postResult.response.ranking}, New Best: {postResult.response.newBest}\n\n";
					}
					else
					{
						switch (postResult.status)
						{
							case PostRecordStatus.BANNED:
								pendingRecordsStatus.text += "<color=red>User banned from the leaderboards. Discarded.</color>\n\n";
								break;

							case PostRecordStatus.INVALID_BUNDLE:
							case PostRecordStatus.INVALID_ID:
								pendingRecordsStatus.text += "<color=red>Level's leaderboards are not enabled. Discarded.</color>\n\n";
								break;

							case PostRecordStatus.RATE_LIMITED:
								pendingRecordsStatus.text += "<color=red>Too many requests sent. Returning record to the pending list</color>\n\n";
								failedToSend.Add(record);
								break;

							case PostRecordStatus.INVALID_HASH:
								pendingRecordsStatus.text += "<color=red>Record bundle version is not up to date with the leaderboard. Discarded.</color>\n\n";
								break;

							case PostRecordStatus.INVALID_TIME:
								pendingRecordsStatus.text += $"<color=red>Angry server rejected the sent time {record.time}. Discarded.</color>\n\n";
								break;

							default:
								pendingRecordsStatus.text += $"<color=red>Encountered an unknown error while posting record. Status: {postResult.status}, Message: '{postResult.message}'. Returning record to the pending list</color>\n\n";
								failedToSend.Add(record);
								break;
						}
					}
				}
				else
				{
					pendingRecordsStatus.text += $"<color=red>Encountered a network error while posting record. Returning record to the pending list</color>\n\n";
					failedToSend.Add(record);
				}
			}

			pendingRecordsStatus.text += $"<color=lime>Done!</color>";
			pendingRecordsField.value = JsonConvert.SerializeObject(failedToSend);
			UpdatePendingRecordsUI();
		}

		internal static void ProcessPendingRecords()
		{
			if (pendingRecordsTask != null && !pendingRecordsTask.IsCompleted)
				return;

			sendPendingRecords.interactable = false;
			pendingRecordsTask = ProcessPendingRecordsTask().ContinueWith((task) => sendPendingRecords.interactable = true, TaskScheduler.FromCurrentSynchronizationContext());
		}
		#endregion

		private void DisplayPluginConfigVersionError()
		{
			var errorConfig = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
			errorConfig.SetIconWithURL("file://" + Path.Combine(workingDir, "plugin-icon.png"));
			new ConfigHeader(errorConfig.rootPanel, $"<color=red>Plugin config version too low, {PLUGIN_CONFIG_MIN_VERSION} or above needed</color>");
		}

		// First validate all dependencies are installed and they meet the minimum requirements
		private void Awake()
		{
			// Plugin startup logic
			instance = this;
			logger = Logger;
			workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			if (Chainloader.PluginInfos.TryGetValue("com.eternalUnion.pluginConfigurator", out var configuratorInfo))
			{
				if (configuratorInfo.Metadata.Version < new Version(PLUGIN_CONFIG_MIN_VERSION))
				{
					logger.LogError($"Angry level loader needs Plugin Configurator minimum version {PLUGIN_CONFIG_MIN_VERSION} to work properly. Disabled.");
					DisplayPluginConfigVersionError();

					enabled = false;
					return;
				}
			}
			else
			{
				logger.LogError($"Angry level loader needs Plugin Configurator minimum version {PLUGIN_CONFIG_MIN_VERSION} to work properly. Disabled.");
				enabled = false;
				return;
			}

			PostAwake();
		}

		private void PostAwake()
		{
			BannedModsManager.Init();

			// Initialize internal config
			internalConfig = PluginConfigurator.Create("Angry Level Loader (INTERNAL)" ,PLUGIN_GUID + "_internal");
			internalConfig.hidden = true;
			internalConfig.interactable = false;
			internalConfig.presetButtonHidden = true;
			internalConfig.presetButtonInteractable = false;

			devMode = new BoolField(internalConfig.rootPanel, "devMode", "devMode", false);
            lastVersion = new StringField(internalConfig.rootPanel, "lastPluginVersion", "lastPluginVersion", "", true, true, false);
			updateLastVersion = new StringField(internalConfig.rootPanel, "updateLastVersion", "updateLastVersion", "", true, true, false);
			ignoreUpdates = new BoolField(internalConfig.rootPanel, "ignoreUpdate", "ignoreUpdate", false, true, false);
			configDataPath = new StringField(internalConfig.rootPanel, "dataPath", "dataPath", Path.Combine(IOUtils.AppData, "AngryLevelLoader"), false, true, false);
			pendingRecordsField = new StringField(internalConfig.rootPanel, "pendingRecordsField", "pendingRecordsField", "", true, true, false);
			askedPermissionForLeaderboards = new BoolField(internalConfig.rootPanel, "askedPermissionForLeaderboards", "askedPermissionForLeaderboards", false);
			leaderboardToggle = new BoolField(internalConfig.rootPanel, "Post records to leaderboards", "leaderboardToggle", false);
			leaderboardToggle.onValueChange += (e =>
			{
				if (e.value == true)
				{
					e.canceled = true;
					NotificationPanel.Open(new LeaderboardPermissionNotification());
				}
			});
			leaderboardToggle.postValueChangeEvent += (newVal =>
			{
				leaderboardsDivision.hidden = newVal;
			});

			if (askedPermissionForLeaderboards.value == false)
				NotificationPanel.Open(new LeaderboardPermissionNotification());

			// Setup variable dependent paths
			dataPath = configDataPath.value;
			IOUtils.TryCreateDirectory(dataPath);
			levelsPath = Path.Combine(dataPath, "Levels");
            IOUtils.TryCreateDirectory(levelsPath);
            tempFolderPath = Path.Combine(dataPath, "LevelsUnpacked");
            IOUtils.TryCreateDirectory(tempFolderPath);

			AngryPaths.TryCreateAllPaths();

			// To detect angry file changes in the levels folder
			CrossThreadInvoker.Init();
			InitializeFileWatcher();
			
			// Load the loader's assets
			Addressables.InitializeAsync().WaitForCompletion();
			angryCatalogPath = Path.Combine(workingDir, "Assets");
			Addressables.LoadContentCatalogAsync(Path.Combine(angryCatalogPath, "catalog.json"), true).WaitForCompletion();
			AssetManager.Init();

			LegacyPatchManager.Init();
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				if (AngrySceneManager.isInCustomLevel)
				{
					int levelVersion = AngrySceneManager.currentBundleContainer.bundleData.bundleVersion;

					if (levelVersion == 2)
						LegacyPatchManager.SetLegacyPatchState(LegacyPatchState.Ver2);
					else
						LegacyPatchManager.SetLegacyPatchState(LegacyPatchState.None);
				}
				else
				{
					LegacyPatchManager.SetLegacyPatchState(LegacyPatchState.None);
				}
			};

			// These scripts are common among all the levels
			if (!LoadEssentialScripts())
			{
				logger.LogError("Disabling AngryLevelLoader because one or more of its dependencies have failed to load");
				enabled = false;
				return;
			}

			// Tracks when each bundle was last played in unix time
			LoadLastPlayedMap();
			// Tracks when the bundle file was last written to
			LoadLastUpdateMap();

			harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();

			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

                if (AngrySceneManager.isInCustomLevel)
				{
					Logger.LogInfo("Running post scene load event");
					AngrySceneManager.PostSceneLoad();

					Logger.LogInfo("Creating UI panel");
					CreateAngryUI();

					Logger.LogInfo("Checking bundle file status");
					AngrySceneManager.currentBundleContainer.CheckReloadPrompt();
				}
				else if (SceneHelper.CurrentScene == "Main Menu")
				{
					CreateCustomLevelButtonOnMainMenu();
				}
			};

			// Delay the catalog reload on boot until the main menu since steam must be initialized for the ticket request
			SceneManager.sceneLoaded += RefreshCatalogOnMainMenu;

			// See if custom difficulties are loaded. BepInEx soft dependency forces them to be loaded first
			if (Chainloader.PluginInfos.ContainsKey(Ultrapain.Plugin.PLUGIN_GUID))
			{
				ultrapainLoaded = true;
				difficultyList.Add("ULTRAPAIN");
			}
			if (Chainloader.PluginInfos.ContainsKey("com.heaven.orhell"))
			{
				heavenOrHellLoaded = true;
				difficultyList.Add("HEAVEN OR HELL");
			}

			InitializeConfig();
			config.rootPanel.onPannelOpenEvent += (externally) =>
			{
				if (AngryLeaderboards.bannedModsListLoaded)
					CheckForBannedMods();
				else
					AngryLeaderboards.LoadBannedModsList();
			};
			AngryLeaderboards.LoadBannedModsList();

			// TODO: Investigate further on this issue:
			//
			// if I don't do that, when I load an addressable scene (custom level)
			// it results in whatever this is. I guess it doesn't load the dependencies
			// but I am not too sure. Same thing happens when I load trough asset bundles
			// instead and everything is white unless I load a prefab which creates a chain
			// reaction of texture, material, shader dependency loads. Though it MIGHT be incorrect,
			// and I am not sure of the actual origin of the issue (because when I check the loaded
			// bundles every addressable bundle is already in the memory like what?)
			Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Projectile Decorative.prefab");

			// Migrate from legacy versions, and check for a new version from web
			PluginUpdateHandler.Check();

            ScanForLevels();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

		float lastPress = 0;
		private void OnGUI()
		{
			if (reloadFileKeybind.value == KeyCode.None)
				return;

			if (!AngrySceneManager.isInCustomLevel)
				return;

			Event current = Event.current;
			KeyCode keyCode = KeyCode.None;
			if (current.keyCode == KeyCode.Escape)
			{
				return;
			}
			if (current.isKey || current.isMouse || current.button > 2 || current.shift)
			{
				if (current.isKey)
				{
					keyCode = current.keyCode;
				}
				else if (Input.GetKey(KeyCode.LeftShift))
				{
					keyCode = KeyCode.LeftShift;
				}
				else if (Input.GetKey(KeyCode.RightShift))
				{
					keyCode = KeyCode.RightShift;
				}
				else if (current.button <= 6)
				{
					keyCode = KeyCode.Mouse0 + current.button;
				}
			}
			else if (Input.GetKey(KeyCode.Mouse3) || Input.GetKey(KeyCode.Mouse4) || Input.GetKey(KeyCode.Mouse5) || Input.GetKey(KeyCode.Mouse6))
			{
				keyCode = KeyCode.Mouse3;
				if (Input.GetKey(KeyCode.Mouse4))
				{
					keyCode = KeyCode.Mouse4;
				}
				else if (Input.GetKey(KeyCode.Mouse5))
				{
					keyCode = KeyCode.Mouse5;
				}
				else if (Input.GetKey(KeyCode.Mouse6))
				{
					keyCode = KeyCode.Mouse6;
				}
			}
			else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				keyCode = KeyCode.LeftShift;
				if (Input.GetKey(KeyCode.RightShift))
				{
					keyCode = KeyCode.RightShift;
				}
			}
			
			if (keyCode == reloadFileKeybind.value)
			{
				if (Time.time - lastPress < 3)
					return;

				lastPress = Time.time;

				if (NotificationPanel.CurrentNotificationCount() == 0)
					ReloadFileKeyPressed();
			}
		}
	
		private void ReloadFileKeyPressed()
		{
			if (AngrySceneManager.currentBundleContainer != null)
				AngrySceneManager.currentBundleContainer.UpdateScenes(false, false);
		}
	}

    public static class RudeLevelInterface
    {
		public static char INCOMPLETE_LEVEL_CHAR = '-';
		public static char GetLevelRank(string levelId)
        {
			LevelContainer level = Plugin.GetLevel(levelId);
			if (level == null)
				return INCOMPLETE_LEVEL_CHAR;
			return level.finalRank.value[0];
		}
	
        public static bool GetLevelChallenge(string levelId)
		{
			LevelContainer level = Plugin.GetLevel(levelId);
			if (level == null)
				return false;
			return level.challenge.value;
		}

		public static bool GetLevelSecret(string levelId, int secretIndex)
		{
			if (secretIndex < 0)
				return false;

			LevelContainer level = Plugin.GetLevel(levelId);
			if (level == null)
				return false;

			level.AssureSecretsSize();
			if (secretIndex >= level.field.data.secretCount)
				return false;
			return level.secrets.value[secretIndex] == 'T';
		}

        public static string GetCurrentLevelId()
        {
            return AngrySceneManager.isInCustomLevel ? AngrySceneManager.currentLevelData.uniqueIdentifier : "";
        }
    }

	public static class RudeBundleInterface
	{
		public static bool BundleExists(string bundleGuid)
		{
			return Plugin.angryBundles.Values.Where(bundle => bundle.bundleData.bundleGuid == bundleGuid).FirstOrDefault() != null;
		}

		public static string GetBundleBuildHash(string bundleGuid)
		{
			var bundle = Plugin.angryBundles.Values.Where(bundle => bundle.bundleData.bundleGuid == bundleGuid).FirstOrDefault();
			return bundle == null ? "" : bundle.bundleData.buildHash;
		}
    }
}
