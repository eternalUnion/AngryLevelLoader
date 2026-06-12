using AngryLevelLoader.Containers;
using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Fields;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.BannedMods;
using AngryLevelLoader.Managers.LegacyPatches;
using AngryLevelLoader.Managers.ServerManager;
using AngryLevelLoader.Notifications;
using AngryLevelLoader.Patches;
using AngryLevelLoader.UserInterface;
using AngryLevelLoader.Utils;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AngryLevelLoader
{
    internal class SpaceField : CustomConfigField
    {
        public SpaceField(ConfigPanel parentPanel, float space) : base(parentPanel, 60, space)
        {

        }
	}

	[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
	[BepInDependency(PluginConfiguratorController.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(Notiffy.NotiffyPlugin.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
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
        public const string PLUGIN_VERSION = "4.0.3";

		public const string PLUGIN_CONFIG_MIN_VERSION = "1.8.0";

		internal static string workingDir;
		/// <summary>
		/// This is the path addressable remote load path uses ({AngryLevelLoader.Plugin.tempFolderPath}\\{guid}).
		/// Do not modify it.
		/// </summary>
		public static string tempFolderPath;
		internal static string dataPath;
        internal static string levelsPath;
		internal static string mapVarsFolderPath;
		/// <summary>
		/// This field is required by the addressables system. Do not modify it. ({AngryLevelLoader.Plugin.angryCatalogPath}).
		/// </summary>
		public static string angryCatalogPath;

        internal static Plugin instance;
		internal static Harmony harmony;
		internal static ManualLogSource logger;


		#region Bundle data
		private static readonly Dictionary<string, BundleContainer> angryBundles = new Dictionary<string, BundleContainer>();

		/// <summary>
		/// Get all locally installed bundles. <see cref="ScanForLevels"/> ensures that a bundle container is created
		/// for all angry files.
		/// </summary>
		public static IEnumerable<BundleContainer> GetAllBundleContainers()
		{
			return angryBundles.Values;
		}

		/// <summary>
		/// Try to get a locally installed bundle with a specific guid.
		/// <see cref="ScanForLevels"/> ensures that a bundle container is created for all angry files.
		/// </summary>
		public static bool TryGetAngryBundleByGuid(string guid, out BundleContainer bundleContainer)
		{
			bundleContainer = angryBundles.Values.Where(bundle => bundle.bundleGuid == guid).FirstOrDefault();
			return bundleContainer != null;
		}

		/// <summary>
		/// Attempt to find a local level with the given unique id. It is a standard that
		/// no two levels share the same unique id.
		/// <see cref="ScanForLevels"/> ensures that a bundle container is created for all angry files.
		/// </summary>
		public static bool TryGetAngryLevel(string id, out LevelContainer level)
		{
			level = null;

			foreach (BundleContainer container in Plugin.GetAllBundleContainers())
			{
				foreach (LevelContainer levelContainer in container.GetAllLevelContainers())
				{
					if (levelContainer.levelId == id)
					{
						level = levelContainer;
						return true;
					}
				}
			}

			return false;
		}
		#endregion


		#region Angry file loader
		private static int numOfOldBundles = 0;
		private static void ProcessPath(string path, FolderButtonField folder)
		{
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

					folder.bundles.Add(bundle);

					if (data.bundleVersion < 6)
					{
						numOfOldBundles += 1;
					}

                    bundle.pathToAngryBundle = path;

					if (!bundle.LazyLoaded)
					{
						try
						{
							bundle.ReloadBundle(false, true);
						}
						catch (Exception e)
						{
							logger.LogWarning($"Exception thrown while loading level bundle: {e}");
							if (!string.IsNullOrEmpty(ConfigManager.errorText.text))
								ConfigManager.errorText.text += '\n';
							ConfigManager.errorText.text += $"<color=red>Error loading {Path.GetFileNameWithoutExtension(path)}</color>. Check the logs for more information";
						}
					}

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

                bundle = new BundleContainer(path, data);
				angryBundles[data.bundleGuid] = bundle;
				folder.bundles.Add(bundle);

				try
                {
					bundle.ReloadBundle(false, true);
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

		private static void ScanForLevelsRecursive(string folderPath, FolderButtonField folder)
		{
			foreach (string filePath in Directory.GetFiles(folderPath))
			{
				if (!filePath.EndsWith(".angry"))
					continue;

				ProcessPath(Path.Combine(folderPath, filePath), folder);
			}

			foreach (string subfolderPath in Directory.GetDirectories(folderPath))
			{
				string subfolderName = Path.GetFileName(subfolderPath);
				ScanForLevelsRecursive(subfolderPath, folder.GetOrCreateFolder(subfolderName));
			}
		}

		/// <summary>
		/// Read all files in the levels directory and create bundle containers for them.
		/// Bundles are only partially loaded (see <see cref="BundleContainer.LazyLoaded"/>)
		/// to increase responsiveness and save on memory.
		/// </summary>
		public static void ScanForLevels()
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

			AngryBundleList.ResetFolders();
			ScanForLevelsRecursive(levelsPath, AngryBundleList.rootFolder);

			AngryBundleList.SortBundles();
			AngryBundleList.UpdateFolderIcons();

			if (numOfOldBundles != 0)
			{
				if (!string.IsNullOrEmpty(ConfigManager.errorText.text))
					ConfigManager.errorText.text += '\n';
				ConfigManager.errorText.text += $"<color=yellow>Hidden {numOfOldBundles} old angry file(s). These files can be deleted at the bottom of the settings page.</color>";
			}

			AngryBundleList.DisplayFolder(AngryBundleList.rootFolder);
			OnlineLevelsList.UpdateUI();
		}
		#endregion


		#region Startup logic
		private static bool LoadEssentialScripts()
        {
			bool loaded = true;

			switch (ScriptManager.AttemptLoadScriptWithCertificate("AngryLoaderAPI.dll"))
			{
				case ScriptManager.LoadScriptResult.Loaded:
					break;

				case ScriptManager.LoadScriptResult.NotFound:
					logger.LogError("Required script AngryLoaderAPI.dll not found");
					loaded = false;
					break;

				case ScriptManager.LoadScriptResult.NoCertificate:
					logger.LogError("Required script AngryLoaderAPI.dll is not signed");
					loaded = false;
					break;

				case ScriptManager.LoadScriptResult.InvalidCertificate:
					logger.LogError("Required script AngryLoaderAPI.dll signature is invalid");
					loaded = false;
					break;

				default:
					logger.LogError("Required script AngryLoaderAPI.dll could not be loaded");
					loaded = false;
					break;
			}

			switch (ScriptManager.AttemptLoadScriptWithCertificate("RudeLevelScripts.dll"))
			{
				case ScriptManager.LoadScriptResult.Loaded:
					break;

				case ScriptManager.LoadScriptResult.NotFound:
					logger.LogError("Required script RudeLevelScripts.dll not found");
					loaded = false;
					break;

				case ScriptManager.LoadScriptResult.NoCertificate:
					logger.LogError("Required script RudeLevelScripts.dll is not signed");
					loaded = false;
					break;

				case ScriptManager.LoadScriptResult.InvalidCertificate:
					logger.LogError("Required script RudeLevelScripts.dll signature is invalid");
					loaded = false;
					break;

				default:
					logger.LogError("Required script RudeLevelScripts.dll could not be loaded");
					loaded = false;
					break;
			}

			return loaded;
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

		// Delayed refresh online catalog on boot
		private static void RefreshCatalogOnMainMenu(Scene newScene, LoadSceneMode mode)
		{
			if (SceneHelper.CurrentScene != "Main Menu")
				return;

			if (ConfigManager.refreshCatalogOnBoot.value)
				OnlineLevelsList.RefreshAsync();

			SceneManager.sceneLoaded -= RefreshCatalogOnMainMenu;
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
				
				string oldFullPath = e.OldFullPath;
				foreach (var bundle in angryBundles.Values)
				{
					if (AngryIOUtils.PathEquals(oldFullPath, bundle.pathToAngryBundle))
					{
						logger.LogWarning($"Bundle {oldFullPath} was renamed, path updated");
						bundle.pathToAngryBundle = e.FullPath;
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

			levelsWatcher.IncludeSubdirectories = true;
			levelsWatcher.EnableRaisingEvents = true;

			scriptsWatcher = new FileSystemWatcher(AngryPaths.ScriptsPath);
			scriptsWatcher.SynchronizingObject = CrossThreadInvoker.Instance;
			scriptsWatcher.Changed += (sender, e) =>
			{
				string fullPath = e.FullPath;
				if (!fullPath.EndsWith(".dll"))
					return;

				if (!ScriptManager.ScriptChanged(Path.GetFileName(fullPath)))
					return;
				logger.LogMessage($"Detected script change {Path.GetFileName(fullPath)}");

				AngryUI.UpdatedScript = Path.GetFileName(fullPath);
			};

			scriptsWatcher.Filter = "*";

			scriptsWatcher.IncludeSubdirectories = false;
			scriptsWatcher.EnableRaisingEvents = true;

			static IEnumerator LoadLevelInstantly()
			{
				yield return null;

				if (!Plugin.TryGetAngryBundleByGuid(InternalConfigManager.instantLoadLevelGuid.value, out BundleContainer bundle))
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

			static void CheckForInstantLoad(Scene scene, LoadSceneMode mode)
			{
				if (mode == LoadSceneMode.Additive)
					return;

				if (AngrySceneManager.isInCustomLevel || SceneHelper.CurrentScene != "Main Menu")
					return;

				if (!InternalConfigManager.instantLoadLevel.value)
				{
					SceneManager.sceneLoaded -= CheckForInstantLoad;
					return;
				}
				InternalConfigManager.instantLoadLevel.value = false;

				logger.LogInfo("Starting custom level instantly");
				instance.StartCoroutine(LoadLevelInstantly());
			}

			SceneManager.sceneLoaded += CheckForInstantLoad;
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
			catch (Exception ex)
			{
				logger.LogError($"Failed to create data path at '{dataPath}'! Overwriting with the default value '{InternalConfigManager.configDataPath.defaultValue}'");
				logger.LogError(ex);
				InternalConfigManager.configDataPath.value = InternalConfigManager.configDataPath.defaultValue;
				dataPath = InternalConfigManager.configDataPath.defaultValue;

				try
				{
					AngryIOUtils.TryCreateDirectory(dataPath);
				}
				catch (Exception innerEx)
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

			AngryUser.Init();
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

			try
			{
				harmony.PatchAll();
			}
			catch (Exception ex)
			{
				logger.LogError(ex);
				ConfigManager.InitializeErrorConfig($"Error! Failed to patch ULTRAKILL. You might be on an older/wrong version of ULTRAKILL.", ex);
			}

			AngrySceneManager.Init();

			// Delay the catalog reload on boot until the main menu since steam must be initialized for the ticket request
			SceneManager.sceneLoaded += RefreshCatalogOnMainMenu;

			ConfigManager.InitializeConfig();

			BannedModsManager.Init();

			// Also load some necessary assets which are needed during scene load
			MeshCombineManagerPatches.Initialize();

            // Migrate from legacy versions, and check for a new version from web
            PluginUpdateHandler.Check();

            ScanForLevels();

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
				AngryUser.reportState = perms.response.reportState;
				ConfigManager.reportsButton.hidden = !perms.response.hasLeaderboardModificationPermission;

				if (perms.response.hasLeaderboardModificationPermission && perms.response.reportState != null)
				{
					string[] knownReports = InternalConfigManager.reportState.value.Split(',');
					int unknownReportCount = perms.response.reportState.Except(knownReports).Count();

					if (unknownReportCount > 0)
					{
						string header = "New reports";
						string body = $"There are {unknownReportCount} new report{(unknownReportCount > 1 ? "s" : "")} available!";
						NotificationManager.SendNotificationInMainMenu(header, body);
					}
				}

			}, TaskScheduler.FromCurrentSynchronizationContext());

			// Init UI
			AngryCustomLevelButton.Init();
			AngryUI.Init();
			AngryBundleList.Init();

			Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        }
		#endregion


		#region Keybind handler
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
				{
					if (AngrySceneManager.currentBundleContainer != null)
						AngrySceneManager.currentBundleContainer.ReloadBundle(false, false);
				}
			}

			if (keyCode == ConfigManager.reloadScriptKeybind.value)
			{
				AngryUI.ReloadScript();
			}
		}
		#endregion
	}

	#region Rude-Angry Interfaces

	/*
	 * Unity cannot safely load assemblies including BepInEx or Harmony types.
	 * For this reason, AngryLoaderAPI provides a BepInEx/Harmony free assembly
	 * which is used for rude essential scripts.
	 */

	public static class RudeLevelInterface
    {
		public static char INCOMPLETE_LEVEL_CHAR = '-';
		public static char GetLevelRank(string levelId)
        {
			if (Plugin.TryGetAngryLevel(levelId, out LevelContainer level))
				return level.FinalRank;
			return INCOMPLETE_LEVEL_CHAR;
		}
	
        public static bool GetLevelChallenge(string levelId)
		{
			if (Plugin.TryGetAngryLevel(levelId, out LevelContainer level))
				return level.ChallengeDone;
			return false;
		}

		public static bool GetLevelSecret(string levelId, int secretIndex)
		{
			if (secretIndex < 0)
				return false;

			if (Plugin.TryGetAngryLevel(levelId, out LevelContainer level))
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
			return Plugin.GetAllBundleContainers().Where(bundle => bundle.bundleGuid == bundleGuid).FirstOrDefault() != null;
		}

		public static string GetBundleBuildHash(string bundleGuid)
		{
			var bundle = Plugin.GetAllBundleContainers().Where(bundle => bundle.bundleGuid == bundleGuid).FirstOrDefault();
			return bundle == null ? "" : bundle.BuildHash;
		}
    }

	public static class RudeGamemodeInterface
	{
		public static AngryGamemodeManager.Gamemode GetCurrentGamemode()
		{
			return AngryGamemodeManager.SelectedGamemode;
		}
	}

	#endregion
}
