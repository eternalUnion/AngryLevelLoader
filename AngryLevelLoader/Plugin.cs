using AngryLevelLoader.Containers;
using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Fields;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.BannedMods;
using AngryLevelLoader.Managers.LegacyPatches;
using AngryLevelLoader.Managers.ServerManager;
using AngryLevelLoader.Notifications;
using AngryLevelLoader.Patches;
using AngryUiComponents;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Logic;
using Newtonsoft.Json;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using RudeLevelScript;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static AngryLevelLoader.Managers.ServerManager.AngryLeaderboards;

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
	// Soft ban dependencies
	[BepInDependency("com.eternalUnion.ultraPain", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.banana.BananaDifficulty", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("billy.billiondifficulty", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.sinai.unityexplorer", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("ironfarm.uk.uc", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("Hydraxous.ULTRAKILL.UltraFunGuns", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("ironfarm.uk.muda", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("maranara_whipfix", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("maranara_project_prophet", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("dev.galvin.timestopper", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.banana.timestop", BepInDependency.DependencyFlags.SoftDependency)]
	public class Plugin : BaseUnityPlugin
	{
        public const string PLUGIN_NAME = "AngryLevelLoader";
        public const string PLUGIN_GUID = "com.eternalUnion.angryLevelLoader";
        public const string PLUGIN_VERSION = "3.2.3";

		public const string PLUGIN_CONFIG_MIN_VERSION = "1.8.0";

		public static readonly Vector3 defaultGravity = new Vector3(0, -40, 0);

		public static string workingDir;
		// This is the path addressable remote load path uses
		// {AngryLevelLoader.Plugin.tempFolderPath}\\{guid}
		public static string tempFolderPath;
		public static string dataPath;
        public static string levelsPath;
        public static string mapVarsFolderPath;

        // This is the path angry addressables use
        public static string angryCatalogPath;

        public static Plugin instance;
		public static ManualLogSource logger;
		public static bool ultrapainLoaded = false;
		public static bool bananasDifficultyLoaded = false;
		public static bool billionDifficultyLoaded = false;

		#region Loaded bundles
		public static Dictionary<string, BundleContainer> angryBundles = new Dictionary<string, BundleContainer>();

		public static BundleContainer GetAngryBundleByGuid(string guid)
		{
			return angryBundles.Values.Where(bundle => bundle.bundleGuid == guid).FirstOrDefault();
		}
		#endregion



		#region Folder subsystem
		internal static Dictionary<string, FolderButtonField> pathToFolderMap = new Dictionary<string, FolderButtonField>();
		internal static Stack<FolderButtonField> folderStack = new Stack<FolderButtonField>();

		private static FolderButtonField GetFolder(string folder)
		{
			if (!pathToFolderMap.TryGetValue(folder, out FolderButtonField folderField))
			{
				folderField = new FolderButtonField(ConfigManager.folderDivision);
				folderField.folderName = Path.GetFileName(folder);
				folderField.onPressed.AddListener(() =>
				{
					DisplayFolder(folderField);
					folderStack.Push(folderField);
				});

				pathToFolderMap[folder] = folderField;

				string currentPath = folder;
				FolderButtonField currentFolder = folderField;
				while (currentPath != "/")
				{
					string parentPath = Path.GetDirectoryName(currentPath).Replace('\\', '/');
					bool parentFolderExisted = true;

					if (!pathToFolderMap.TryGetValue(parentPath, out FolderButtonField parentFolder))
					{
						parentFolderExisted = false;

						parentFolder = new FolderButtonField(ConfigManager.folderDivision);
						parentFolder.folderName = Path.GetFileName(parentPath);
						parentFolder.onPressed.AddListener(() =>
						{
							DisplayFolder(parentFolder);
							folderStack.Push(parentFolder);
						});

						pathToFolderMap[parentPath] = parentFolder;
					}

					parentFolder.folders.Add(currentFolder);
					if (parentFolderExisted)
						break;

					currentFolder = parentFolder;
					currentPath = parentPath;
				}
			}

			return folderField;
		}

		internal static void DisplayFolder(FolderButtonField folderField)
		{
			if (folderField == null)
				folderField = pathToFolderMap["/"];

			foreach (var bundle in angryBundles.Values)
				bundle.Hidden = true;
			foreach (var folder in pathToFolderMap.Values)
				folder.hidden = true;

			foreach (var bundle in folderField.bundles)
				if (bundle.LazyLoaded)
					bundle.Hidden = false;

			foreach (var folder in folderField.folders)
			{
				if (folder.Any())
					folder.hidden = false;
			}

			if (folderField == pathToFolderMap["/"])
				ConfigManager.levelBundlesHeader.text = "Level Bundles";
			else
				ConfigManager.levelBundlesHeader.text = $"Level Bundles <color=grey>{pathToFolderMap.Where(e => e.Value == folderField).FirstOrDefault().Key}</color>";
		}
		#endregion



		#region Search subsystem
		private static string[] currentSearchKeywords = new string[0];
		private static char[] whitespaceSeparator = new char[] { ' ' };

		private static void UpdateBundleSearch(string newVal)
		{
			string[] newKeywords = newVal.Split(whitespaceSeparator, StringSplitOptions.RemoveEmptyEntries).Select(keyword => keyword.ToLower()).ToArray();
			if (newKeywords.Length == currentSearchKeywords.Length && newKeywords.SequenceEqual(currentSearchKeywords))
				return;

			currentSearchKeywords = newKeywords;

			// If not searching anything, open the current folder
			if (currentSearchKeywords.Length == 0)
			{
				ConfigManager.folderDivision.hidden = false;
				ConfigManager.searchInfo.hidden = true;

				foreach (BundleContainer bundle in angryBundles.Values)
					bundle.SearchKeywords = new string[0];

				DisplayFolder(folderStack.Peek());
				return;
			}

			ConfigManager.folderDivision.hidden = true;
			ConfigManager.searchInfo.hidden = false;
			int filterCount = 0, totalCount = 0;
			foreach (BundleContainer bundle in angryBundles.Values)
			{
				bundle.SearchKeywords = currentSearchKeywords;

				if (!bundle.HasValidAngryFile)
					continue;

				totalCount += 1;
				if (bundle.SearchMatch)
					filterCount += 1;
			}

			ConfigManager.levelBundlesHeader.text = "Level Bundles";
			ConfigManager.searchInfo.text = $"Showing {filterCount} of {totalCount} bundles";
		}
		#endregion



		private static int numOfOldBundles = 0;
		public static void ProcessPath(string path, string folder)
		{
			FolderButtonField folderField = GetFolder(folder);

			if (AngryFileUtils.TryGetAngryBundleData(path, out AngryBundleData data, out Exception error))
			{
                if (angryBundles.TryGetValue(data.bundleGuid, out BundleContainer bundle))
                {
					// Duplicate file check
					if (bundle.HasValidAngryFile && !AngryIOUtils.PathEquals(path, bundle.pathToAngryBundle))
					{
						logger.LogError($"Duplicate angry files. Original: {Path.GetFileName(bundle.pathToAngryBundle)}. Duplicate: {Path.GetFileName(path)}");

						if (!string.IsNullOrEmpty(ConfigManager.errorText.text))
							ConfigManager.errorText.text += '\n';
						ConfigManager.errorText.text += $"<color=red>Error loading {Path.GetFileName(path)}</color> Duplicate file, original is {Path.GetFileName(bundle.pathToAngryBundle)}";

						return;
					}

					folderField.bundles.Add(bundle);

					if (data.bundleVersion < 6)
					{
						numOfOldBundles += 1;
					}

                    bundle.pathToAngryBundle = path;

					// May need to reload the bundle if the loaded bundle is out of date
					if (bundle.LazyLoaded && bundle.BuildHash != data.buildHash)
					{
						if (bundle.Loaded && AngrySceneManager.isInCustomLevel && AngrySceneManager.currentBundleContainer == bundle)
						{
							// If the bundle is currently being played, only show a prompt
							bundle.FileChanged();
						}
						else
						{
							// Otherwise, only load the required data
							bundle.ReloadBundle(false, true);
						}
					}

					return;
                }

                BundleContainer newBundle = new BundleContainer(path, data);
				angryBundles[data.bundleGuid] = newBundle;
				folderField.bundles.Add(newBundle);
				newBundle.UpdateOrder();

                try
                {
					newBundle.ReloadBundle(false, true);
				}
                catch (Exception e)
                {
					logger.LogWarning($"Exception thrown while loading level bundle: {e}");
                    if (!string.IsNullOrEmpty(ConfigManager.errorText.text))
						ConfigManager.errorText.text += '\n';
					ConfigManager.errorText.text += $"<color=red>Error loading {Path.GetFileNameWithoutExtension(path)}</color>. Check the logs for more information";
                }

                // Old bundle, cannot open level
                if (data.bundleVersion < 6)
                {
					numOfOldBundles += 1;
                }
            }
			else
			{
                if (AngryFileUtils.IsV1LegacyFile(path))
                {
                    if (!string.IsNullOrEmpty(ConfigManager.errorText.text))
						ConfigManager.errorText.text += '\n';
					ConfigManager.errorText.text += $"<color=yellow>{Path.GetFileName(path)} is a V1 legacy file. Support for legacy files were dropped after 2.5.0</color>";
                }
                else
                {
					logger.LogError($"Could not load the bundle at {path}\n{error}");

                    if (!string.IsNullOrEmpty(ConfigManager.errorText.text))
						ConfigManager.errorText.text += '\n';
					ConfigManager.errorText.text += $"<color=yellow>Failed to load {Path.GetFileNameWithoutExtension(path)}</color>";
                }

                return;
            }
        }

		private static string[] validImageExts = new string[]
		{
			".jpg", ".jpeg", ".png"
		};

		// This does NOT reload the files, only
		// loads newly added angry levels
		internal static void ScanForLevels()
        {
			numOfOldBundles = 0;
			ConfigManager.errorText.text = "";
			ConfigManager.searchBar.value = "";

			if (!Directory.Exists(levelsPath))
            {
				logger.LogWarning("Could not find the Levels folder at " + levelsPath);
				ConfigManager.errorText.text = "<color=red>Error: </color>Levels folder not found";
				return;
            }

			if (!pathToFolderMap.TryGetValue("/", out FolderButtonField rootFolder))
			{
				rootFolder = new FolderButtonField(ConfigManager.config.rootPanel);
				rootFolder.hidden = true;
				pathToFolderMap["/"] = rootFolder;
			}

			foreach (FolderButtonField folder in pathToFolderMap.Values)
			{
				folder.bundles.Clear();
			}

			foreach (AngryIOUtils.SubFileInfo file in AngryIOUtils.GetAllFilesRecursive(levelsPath))
			{
				if (!file.filePath.EndsWith(".angry"))
					continue;
				ProcessPath(file.filePath, file.subFolder);
			}

			foreach (KeyValuePair<string, FolderButtonField> folder in pathToFolderMap)
			{
				if (folder.Value == rootFolder)
					continue;

				string realPath = Path.Combine(levelsPath, folder.Key.Substring(1));
				if (Directory.Exists(realPath))
				{
					string pathToIcon = Directory.GetFiles(realPath).Where(path => validImageExts.Contains(Path.GetExtension(path))).FirstOrDefault();
					if (!string.IsNullOrEmpty(pathToIcon))
					{
						folder.Value.CreateIcon(pathToIcon);
						continue;
					}
				}

				folder.Value.CreateIcon(folder.Value);
			}

			if (numOfOldBundles != 0)
			{
				if (!string.IsNullOrEmpty(ConfigManager.errorText.text))
					ConfigManager.errorText.text += '\n';
				ConfigManager.errorText.text += $"<color=yellow>Hidden {numOfOldBundles} old angry file(s). These files can be deleted at the bottom of the settings page.</color>";
			}

			DisplayFolder(rootFolder);
			folderStack.Clear();
			folderStack.Push(rootFolder);

			OnlineLevelsUI.UpdateUI();
		}

		public static void SortBundles()
		{
			int i = 0;
			if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.Alphabetically)
			{
				foreach (var bundle in angryBundles.Values.OrderBy(b => b.BundleName))
					bundle.SiblingIndex = i++;
			}
			else if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.Author)
			{
				foreach (var bundle in angryBundles.Values.OrderBy(b => b.BundleAuthor))
					bundle.SiblingIndex = i++;
			}
			else if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.LastPlayed)
			{
				foreach (var bundle in angryBundles.Values.OrderByDescending((b) => {
					if (LastPlayedMapManager.lastPlayed.TryGetValue(b.bundleGuid, out long time))
						return time;
					return 0;
				}))
				{
					bundle.SiblingIndex = i++;
				}
			}
			else if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.LastUpdate)
			{
				foreach (var bundle in angryBundles.Values.OrderByDescending((b) => {
					if (LastPlayedMapManager.lastUpdate.TryGetValue(b.bundleGuid, out long time))
						return time;
					return 0;
				}))
				{
					bundle.SiblingIndex = i++;
				}
			}
		}

		internal static void UpdateAllUI()
		{
			foreach (BundleContainer angryBundle in  angryBundles.Values)
			{
				angryBundle.UpdateAllUI();
			}
		}

        private static bool LoadEssentialScripts()
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
		
		public static bool NoMonsters => ConfigManager.difficultyField.gamemodeListValueIndex == 1 || ConfigManager.difficultyField.gamemodeListValueIndex == 2;
		public static bool NoWeapons => ConfigManager.difficultyField.gamemodeListValueIndex == 2;

		public static Harmony harmony;

		// Delayed refresh online catalog on boot
		private static void RefreshCatalogOnMainMenu(Scene newScene, LoadSceneMode mode)
		{
			if (SceneHelper.CurrentScene != "Main Menu")
				return;

			if (ConfigManager.refreshCatalogOnBoot.value)
				OnlineLevelsUI.RefreshAsync();

			SceneManager.sceneLoaded -= RefreshCatalogOnMainMenu;
		}

		// Create the shortcut in chapters menu
		internal const string CUSTOM_LEVEL_BUTTON_ASSET_PATH = "AngryLevelLoader/UI/CustomLevels.prefab";
		internal static AngryCustomLevelButtonComponent currentCustomLevelButton;
		internal static RectTransform bossRushButton;
		internal static void CreateCustomLevelButtonOnMainMenu()
		{
			instance.StartCoroutine(CreateCustomLevelButtonOnMainMenuAsync());
		}

		private static IEnumerator CreateCustomLevelButtonOnMainMenuAsync()
		{
			yield return null;

            GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvasObj == null)
			{
				logger.LogWarning("Angry tried to create main menu buttons, but root canvas was not found!");
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
						logger.LogError("Angry tried to find the options menu but failed!");
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
						logger.LogError("Angry tried to find the plugin configurator button but failed!");
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
				logger.LogWarning("Angry tried to find chapter select menu, but root canvas was not found!");
			}
		}

		// Create the angry canvas
		private const string ANGRY_UI_PANEL_ASSET_PATH = "AngryLevelLoader/UI/AngryUIPanel.prefab";
		public static AngryUIPanelComponent currentPanel;
		private static void CreateAngryUI()
		{
			instance.StartCoroutine(CreateAngryUIAsync());
		}

		private static IEnumerator CreateAngryUIAsync()
		{
			yield return null;

			if (currentPanel != null)
				yield break;

			GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvasObj == null)
			{
				logger.LogWarning("Angry tried to create main menu buttons, but root canvas was not found!");
                yield break;
			}

			GameObject panelObj = Addressables.InstantiateAsync(ANGRY_UI_PANEL_ASSET_PATH, canvasObj.transform).WaitForCompletion();
			currentPanel = panelObj.GetComponent<AngryUIPanelComponent>();

			currentPanel.reloadBundlePrompt.MakeTransparent(true);
		}

		internal static FileSystemWatcher levelsWatcher;
		internal static FileSystemWatcher scriptsWatcher;
		private static void InitializeFileWatcher()
		{
			if (levelsWatcher != null)
				return;

			levelsWatcher = new FileSystemWatcher(levelsPath);
			levelsWatcher.SynchronizingObject = CrossThreadInvoker.Instance;
			levelsWatcher.Changed += (sender, e) =>
			{
				// Notify the bundle that the file is outdated

				string fullPath = e.FullPath;
				foreach (var bundle in angryBundles.Values)
				{
					if (AngryIOUtils.PathEquals(fullPath, bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {fullPath} was updated, container notified");
						bundle.FileChanged();
						return;
					}
				}
			};
			levelsWatcher.Renamed += (sender, e) =>
			{
				// Try to find if a bundle owns the file, then update its file path

				string fullPath = e.FullPath;
				foreach (var bundle in angryBundles.Values)
				{
					if (AngryIOUtils.PathEquals(fullPath, bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {fullPath} was renamed, path updated");
						bundle.pathToAngryBundle = fullPath;
						return;
					}
				}
			};
			levelsWatcher.Deleted += (sender, e) =>
			{
				// Try to find if a bundle owns the file, then unlink it

				string fullPath = e.FullPath;
				foreach (var bundle in angryBundles.Values)
				{
					if (AngryIOUtils.PathEquals(fullPath, bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {fullPath} was deleted, unlinked");
						bundle.pathToAngryBundle = "";
						return;
					}
				}
			};
			levelsWatcher.Created += (sender, e) =>
			{
				// Try to find a bundle matching the file's guid

				string fullPath = e.FullPath;
				if (!AngryFileUtils.TryGetAngryBundleData(fullPath, out AngryBundleData data, out Exception exp))
					return;

				if (angryBundles.TryGetValue(data.bundleGuid, out BundleContainer bundle))
				{
					if ((bundle.bundleGuid == data.bundleGuid) && !bundle.HasValidAngryFile)
					{
						logger.LogWarning($"Bundle {fullPath} was just added, and a container with the same guid had no file linked. Linked, container notified");
						bundle.pathToAngryBundle = fullPath;
						bundle.FileChanged();
						return;
					}
				}
			};

			levelsWatcher.Filter = "*";

			levelsWatcher.IncludeSubdirectories = false;
			levelsWatcher.EnableRaisingEvents = true;

			scriptsWatcher = new FileSystemWatcher(ScriptManager.ScriptsPath);
			scriptsWatcher.SynchronizingObject = CrossThreadInvoker.Instance;
			void OnScriptChange(object sender, FileSystemEventArgs e)
			{
				string fullPath = e.FullPath;
				if (!fullPath.EndsWith(".dll"))
					return;

				if (!ScriptManager.ScriptChanged(Path.GetFileName(fullPath)))
					return;
				logger.LogMessage($"Detected script change {Path.GetFileName(fullPath)}");

				IEnumerator ShowScriptPrompt()
				{
					yield return new WaitUntil(() => currentPanel != null && AngrySceneManager.isInCustomLevel);

					currentPanel.reloadScriptPrompt.gameObject.SetActive(true);
					currentPanel.reloadScriptPrompt.audio.Play();
					currentPanel.reloadScriptPrompt.text.text = $"Script update detected\nPress <color=orange>{ConfigManager.reloadScriptKeybind.value}</color> to reload\n(Can be binded in the settings)";
					currentPanel.reloadScriptPrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
					currentPanel.reloadScriptPrompt.reloadButton.onClick.AddListener(() =>
					{
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

						string[] variablesToRemove = new string[]
						{
							"DOORSTOP_DISABLE",
							"DOORSTOP_DLL_SEARCH_DIRS",
							"DOORSTOP_INITIALIZED",
							"DOORSTOP_INVOKE_DLL_PATH",
							"DOORSTOP_MANAGED_FOLDER_DIR",
							"DOORSTOP_MONO_LIB_PATH",
							"DOORSTOP_PROCESS_PATH"
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
					});

					currentPanel.reloadScriptPrompt.ignoreButton.onClick = new Button.ButtonClickedEvent();
					currentPanel.reloadScriptPrompt.ignoreButton.onClick.AddListener(() =>
					{
						currentPanel.reloadScriptPrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
						currentPanel.reloadScriptPrompt.gameObject.SetActive(false);
					});
				}

				instance.StopCoroutine(nameof(ShowScriptPrompt));
				instance.StartCoroutine(ShowScriptPrompt());
			}
			scriptsWatcher.Changed += OnScriptChange;

			scriptsWatcher.Filter = "*";

			scriptsWatcher.IncludeSubdirectories = false;
			scriptsWatcher.EnableRaisingEvents = true;

			IEnumerator LoadLevelInstantly()
			{
				yield return null;

				BundleContainer bundle = GetAngryBundleByGuid(InternalConfigManager.instantLoadLevelGuid.value);
				if (bundle == null)
				{
					logger.LogInfo("Bundle not found");
					yield break;
				}

				var handler = bundle.ReloadBundle(false, false);
				yield return new WaitUntil(() => handler.IsCompleted);

				if (!bundle.TryGetLevelContainer(InternalConfigManager.instantLoadLevelId.value, out LevelContainer level))
				{
					logger.LogInfo("Level not found");
					yield break;
				}

				GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
				if (canvasObj == null)
				{
					logger.LogWarning("Angry tried to create main menu buttons, but root canvas was not found!");
					yield break;
				}

				// Find the options menu
				Transform optionsMenu = canvasObj.transform.Find("OptionsMenu");
				if (optionsMenu == null)
				{
					logger.LogError("Angry tried to find the options menu but failed!");
					yield break;
				}

				// Open options menu
				optionsMenu.gameObject.SetActive(true);
				yield return null;

				// Open plugin config panel
				Transform pluginConfigButton = optionsMenu.transform.Find("Navigation Rail/PluginConfiguratorButton(Clone)");
				if (pluginConfigButton == null)
					pluginConfigButton = optionsMenu.transform.Find("Navigation Rail/PluginConfiguratorButton");

				if (pluginConfigButton == null)
				{
					logger.LogError("Angry tried to find the plugin configurator button but failed!");
					yield break;
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
				yield return null;

				if (PluginConfiguratorController.activePanel != null)
					PluginConfiguratorController.activePanel.SetActive(false);
				yield return null;

				PluginConfiguratorController.mainPanel.gameObject.SetActive(false);
				yield return null;
				ConfigManager.config.rootPanel.OpenPanelInternally(false);
				yield return null;

				if (!bundle.Loaded)
				{
					Task reloadTask = bundle.ReloadBundle(false, false);
					yield return new WaitUntil(() => reloadTask.IsCompleted);
				}
				yield return null;

				AngrySceneManager.LevelButtonPressed(level);
			}

			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				if (AngrySceneManager.isInCustomLevel || SceneHelper.CurrentScene != "Main Menu")
					return;

				if (!InternalConfigManager.instantLoadLevel.value)
					return;
				InternalConfigManager.instantLoadLevel.value = false;

				logger.LogInfo("Starting custom level instantly");
				instance.StartCoroutine(LoadLevelInstantly());
			};
		}

		private void DisplayPluginConfigVersionError()
		{
			var errorConfig = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
			errorConfig.SetIconWithURL("file://" + Path.Combine(workingDir, "plugin-icon.png"));
			new ConfigHeader(errorConfig.rootPanel, $"<color=red>Plugin config version too low, {PLUGIN_CONFIG_MIN_VERSION} or above needed</color>");
		}

		// First validate all dependencies are installed and they meet the minimum requirements
		private void Start()
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

			try
			{
				PostAwake();
			}
			catch (Exception ex)
			{
				logger.LogError(ex);
				ConfigManager.InitializeErrorConfig("Encountered an unknown error, cannot recover!", ex);
				enabled = false;
			}
		}

		private void PostAwake()
		{
			InternalConfigManager.InitializeInternalConfig();

			if (InternalConfigManager.askedPermissionForLeaderboards.value == false)
				NotificationPanel.Open(new LeaderboardPermissionNotification());

			// Setup variable dependent paths
			dataPath = InternalConfigManager.configDataPath.value;

			try
			{
				AngryIOUtils.TryCreateDirectory(dataPath);
			}
			catch (IOException ex)
			{
				logger.LogError($"Failed to create data path at '{dataPath}'! Overwriting with the default value '{InternalConfigManager.configDataPath.defaultValue}'");
				logger.LogError(ex);
				InternalConfigManager.configDataPath.value = InternalConfigManager.configDataPath.defaultValue;
				dataPath = InternalConfigManager.configDataPath.defaultValue;

				try
				{
					AngryIOUtils.TryCreateDirectory(dataPath);
				}
				catch (IOException innerEx)
				{
					logger.LogError($"Failed to create data path at '{dataPath}'! Cannot recover, disabling plugin.");
					ConfigManager.InitializeErrorConfig($"Error! Failed to create plugin's data folder at '{dataPath}'", innerEx);

					enabled = false;
					return;
				}
			}

			levelsPath = Path.Combine(dataPath, "Levels");
            AngryIOUtils.TryCreateDirectory(levelsPath);
            tempFolderPath = Path.Combine(dataPath, "LevelsUnpacked");
            AngryIOUtils.TryCreateDirectory(tempFolderPath);
			mapVarsFolderPath = Path.Combine(dataPath, "MapVars");
			AngryIOUtils.TryCreateDirectory(mapVarsFolderPath);

			AngryPaths.TryCreateAllPaths();

			// To detect angry file changes in the levels folder
			CrossThreadInvoker.Init();
			InitializeFileWatcher();
			
			// Load the loader's assets
			Addressables.InitializeAsync().WaitForCompletion();
			ForceLoadAddressableDependencies();

			angryCatalogPath = Path.Combine(workingDir, "Assets");
			Addressables.LoadContentCatalogAsync(Path.Combine(angryCatalogPath, "catalog.json"), true).WaitForCompletion();
			AssetManager.Init();

            LegacyPatchManager.Init();

			// These scripts are common among all the levels
			if (!LoadEssentialScripts())
			{
				logger.LogError("Disabling AngryLevelLoader because one or more of its dependencies have failed to load");
				enabled = false;
				return;
			}

			LastPlayedMapManager.LoadLastPlayedMap();
			LastPlayedMapManager.LoadLastUpdateMap();

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

					//Make sure mapvars are ready to go
					MapVarManager.Instance.ReloadMapVars();
				}
				else if (SceneHelper.CurrentScene == "Main Menu")
				{
					CreateCustomLevelButtonOnMainMenu();
				}
			};

			// Delay the catalog reload on boot until the main menu since steam must be initialized for the ticket request
			SceneManager.sceneLoaded += RefreshCatalogOnMainMenu;

			ConfigManager.InitializeConfig();

			ConfigManager.config.rootPanel.onPannelOpenEvent += (externally) =>
			{
				Button.ButtonClickedEvent backButtonEvent = PluginConfiguratorController.backButton.onClick;

				PluginConfiguratorController.backButton.onClick = new Button.ButtonClickedEvent();
				PluginConfiguratorController.backButton.onClick.AddListener(() =>
				{
					if (folderStack.Count <= 1 || !string.IsNullOrEmpty(ConfigManager.searchBar.value))
					{
						backButtonEvent.Invoke();
					}
					else
					{
						folderStack.Pop();

						if (folderStack.Count != 0)
							DisplayFolder(folderStack.Peek());
						else
							DisplayFolder(null);
					}
				});
			};

			ConfigManager.searchBar.onValueChange += UpdateBundleSearch;
			ConfigManager.searchBar.onReset += () => ConfigManager.searchBar.value = "";
			ConfigManager.searchBar.onEndEdit += (bool wasCanceled) =>
			{
				if (!wasCanceled)
					return;

				if (!string.IsNullOrWhiteSpace(ConfigManager.searchBar.value))
				{
					ConfigManager.searchBar.value = "";
					return;
				}

				if (folderStack.Count > 1)
					return;

				ConfigManager.config.rootPanel.ClosePanel();
			};

			BannedModsManager.Init();

			// Also load some necessary assets which are needed during scene load
			MeshCombineManagerPatches.Initialize();

            // Migrate from legacy versions, and check for a new version from web
            PluginUpdateHandler.Check();

            ScanForLevels();

			AngryUser.Init();
			AngryUser.GetPermissionsTask().ContinueWith((res) =>
			{
				if (!res.IsCompletedSuccessfully || !res.Result.completedSuccessfully)
				{
					logger.LogError($"Could not obtain user permissions");
					return;
				}

				var perms = res.Result;
				if (perms.status != AngryUser.UserPermissionsStatus.OK)
				{
					logger.LogError($"Could not obtain user permissions: {perms.message}");
					return;
				}

				AngryUser.hasLeaderboardPermissions = perms.response.hasLeaderboardModificationPermission;

			}, TaskScheduler.FromCurrentSynchronizationContext());

			Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        }

		private void ForceLoadAddressableDependencies()
		{
			// For some reason, we need a dangling reference to load in game addressable asset bundles.
			// Level asset bundle dependencies do not work for some reason. That is, loading a custom
			// level alone does not load the dependencies.
			Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Projectile Decorative.prefab").WaitForCompletion();

			// Rant #2: Addressables being a pain again
			//
			// After the update from Unity 2019 to 2022, it seems like attempting to load an asset
			// from addressables during a scene load synchronously could cause a deadlock because
			// addressables now tries to load the asset bundle asynchronously. Most of the scene load
			// stuff can be done in a co-routine but room spawns should be done in-time, else many
			// scripts that reference the player on start will fail. So force these assets to be
			// always loaded. BTW, this is """"THE SOLUTION""" unity provides, yes the SOLUTION, and they
			// are not planning to do anything about it (flagged as Won't Fix).
			Addressables.LoadAssetAsync<GameObject>("FirstRoom").WaitForCompletion();
			Addressables.LoadAssetAsync<GameObject>("FirstRoom Secret").WaitForCompletion();
			Addressables.LoadAssetAsync<GameObject>("FirstRoom Prime").WaitForCompletion();
			Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Levels/Special Rooms/FirstRoom Encore.prefab").WaitForCompletion();

			Addressables.LoadAssetAsync<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf").WaitForCompletion();
			Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/meter.png").WaitForCompletion();
			Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/arrow.png").WaitForCompletion();
			Addressables.LoadAssetAsync<Material>("Assets/Materials/Environment/Metal/Metal Decoration 20.mat").WaitForCompletion();
		}

		float lastPress = 0;
		private void OnGUI()
		{
			if (ConfigManager.reloadFileKeybind.value == KeyCode.None && ConfigManager.reloadScriptKeybind.value == KeyCode.None)
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

			if (keyCode == KeyCode.None)
				return;
			
			if (keyCode == ConfigManager.reloadFileKeybind.value)
			{
				if (Time.time - lastPress < 3)
					return;

				lastPress = Time.time;

				if (NotificationPanel.CurrentNotificationCount() == 0)
					ReloadFileKeyPressed();
			}

			if (keyCode == ConfigManager.reloadScriptKeybind.value)
			{
				if (currentPanel != null && currentPanel.reloadScriptPrompt != null && currentPanel.reloadScriptPrompt.reloadButton != null)
					currentPanel.reloadScriptPrompt.reloadButton.onClick?.Invoke();
			}
		}
	
		private void ReloadFileKeyPressed()
		{
			if (AngrySceneManager.currentBundleContainer != null)
				AngrySceneManager.currentBundleContainer.ReloadBundle(false, false);
		}
	}

    public static class RudeLevelInterface
    {
		public static char INCOMPLETE_LEVEL_CHAR = '-';
		public static char GetLevelRank(string levelId)
        {
			if (AngrySceneManager.TryFindLevel(levelId, out LevelContainer level))
				return level.FinalRank;
			return INCOMPLETE_LEVEL_CHAR;
		}
	
        public static bool GetLevelChallenge(string levelId)
		{
			if (AngrySceneManager.TryFindLevel(levelId, out LevelContainer level))
				return level.ChallengeDone;
			return false;
		}

		public static bool GetLevelSecret(string levelId, int secretIndex)
		{
			if (secretIndex < 0)
				return false;

			if (AngrySceneManager.TryFindLevel(levelId, out LevelContainer level))
				return level.SecretDiscovered(secretIndex);

			return false;
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
			return Plugin.angryBundles.Values.Where(bundle => bundle.bundleGuid == bundleGuid).FirstOrDefault() != null;
		}

		public static string GetBundleBuildHash(string bundleGuid)
		{
			var bundle = Plugin.angryBundles.Values.Where(bundle => bundle.bundleGuid == bundleGuid).FirstOrDefault();
			return bundle == null ? "" : bundle.BuildHash;
		}
    }
}
