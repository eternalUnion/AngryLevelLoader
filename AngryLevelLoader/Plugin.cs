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

namespace AngryLevelLoader
{
	public class SpaceField : CustomConfigField
    {
        public SpaceField(ConfigPanel parentPanel, float space) : base(parentPanel, 60, space)
        {

        }
    }

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(PluginConfig.PluginConfiguratorController.PLUGIN_GUID, "1.6.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_NAME = "AngryLevelLoader";
        public const string PLUGIN_GUID = "com.eternalUnion.angryLevelLoader";
        public const string PLUGIN_VERSION = "2.0.0";
		public static string tempFolderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LevelsUnpacked");
		public static Plugin instance;

		public static Dictionary<string, RudeLevelData> idDictionary = new Dictionary<string, RudeLevelData>();
		public static Dictionary<string, AngryBundleContainer> angryBundles = new Dictionary<string, AngryBundleContainer>();
		public static Dictionary<string, long> lastPlayed = new Dictionary<string, long>();

		public static void LoadLastPlayedMap()
		{
			lastPlayed.Clear();

			string path = Path.Combine(workingDir, "lastPlayedMap.txt");
			if (!File.Exists(path))
				return;

			using (StreamReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read)))
			{
				while (!reader.EndOfStream)
				{
					string key = reader.ReadLine();
					if (reader.EndOfStream)
					{
						Debug.LogWarning("Invalid end of last played map file");
						break;
					}

					string value = reader.ReadLine();
					if (long.TryParse(value, out long seconds))
					{
						lastPlayed[key] = seconds;
					}
					else
					{
						Debug.Log($"Invalid last played time '{value}'");
					}
				}
			}
		}

		public static void UpdateLastPlayed(AngryBundleContainer bundle)
		{
			string guid = bundle.guid;
			if (guid.Length != 32)
				return;

			if (bundleSortingMode.value == BundleSorting.LastPlayed)
				bundle.rootPanel.siblingIndex = 0;
			long secondsNow = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			lastPlayed[guid] = secondsNow;

			string path = Path.Combine(workingDir, "lastPlayedMap.txt");
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

		public static AngryBundleContainer GetAngryBundleByGuid(string guid)
		{
			return angryBundles.Values.Where(bundle => bundle.guid == guid).FirstOrDefault();
		}

		// This does NOT reload the files, only
		// loads newly added angry levels
		public static void ScanForLevels()
        {
            errorText.text = "";
			string bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Levels");
            if (!Directory.Exists(bundlePath))
            {
                Debug.LogWarning("Could not find the Levels folder at " + bundlePath);
				errorText.text = "<color=red>Error: </color>Levels folder not found";
				return;
            }

			foreach (string path in Directory.GetFiles(bundlePath))
			{
				if (angryBundles.TryGetValue(path, out AngryBundleContainer levelAsset))
				{
					levelAsset.rootPanel.interactable = true;
					levelAsset.rootPanel.hidden = false;
					continue;
				}

				AngryBundleContainer level = new AngryBundleContainer(path);
				angryBundles[path] = level;
				try
				{
					level.UpdateScenes(false);
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Exception thrown while loading level bundle: {e}");
					if (!string.IsNullOrEmpty(errorText.text))
						errorText.text += '\n';
					errorText.text += $"<color=red>Error loading {Path.GetFileNameWithoutExtension(path)}</color>. Check the logs for more information";
				}
			}
		}

		public static void SortBundles()
		{
			int i = 0;
			if (bundleSortingMode.value == BundleSorting.Alphabetically)
			{
				foreach (var bundle in angryBundles.Values.OrderBy(b => b.name))
					bundle.rootPanel.siblingIndex = i++;
			}
			else if (bundleSortingMode.value == BundleSorting.Author)
			{
				foreach (var bundle in angryBundles.Values.OrderBy(b => b.author))
					bundle.rootPanel.siblingIndex = i++;
			}
			else if (bundleSortingMode.value == BundleSorting.LastPlayed)
			{
				foreach (var bundle in angryBundles.Values.OrderBy((b) => {
					if (lastPlayed.TryGetValue(b.guid, out long time))
						return time;
					return 0;
				}))
				{
					bundle.rootPanel.siblingIndex = i++;
				}
			}
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

		public static void UpdateAllUI()
		{
			foreach (AngryBundleContainer angryBundle in  angryBundles.Values)
			{
				foreach (LevelContainer level in angryBundle.levels.Values)
				{
					level.UpdateUI();
				}
			}
		}

        public static void LoadScripts()
        {
			string asmLocation = Assembly.GetExecutingAssembly().Location;
			string asmDir = Path.Combine(Path.GetDirectoryName(asmLocation), "Scripts");

			if (!Directory.Exists(asmDir))
			{
				Debug.LogWarning("Scripts folder does not exists at " + asmDir);
				return;
			}

			foreach (string scriptPath in Directory.GetFiles(asmDir))
			{
				Assembly asm = Assembly.LoadFile(scriptPath);
				Debug.Log("Loaded " + asm.FullName);
			}
		}

        // Game assets
        public static Font gameFont;
		public static Sprite notPlayedPreview;
		public static Sprite lockedPreview;

        public static bool isInCustomScene = false;
        public static RudeLevelData currentLevelData;
        public static LevelContainer currentLevelContainer;
		public static AngryBundleContainer currentBundleContainer;
		public static int selectedDifficulty;

        public static void CheckIsInCustomScene(Scene current)
        {
			foreach (AngryBundleContainer container in angryBundles.Values)
			{
				if (container.GetAllScenePaths().Contains(current.path))
				{
					isInCustomScene = true;
					currentLevelData = container.GetAllLevelData().Where(data => data.scenePath == current.path).First();
					currentBundleContainer = container;
					currentLevelContainer = container.levels[container.GetAllLevelData().Where(data => data.scenePath == current.path).First().uniqueIdentifier];
					currentLevelContainer.discovered.value = true;
					currentLevelContainer.UpdateUI();
					config.presetButtonInteractable = false;

					return;
				}
			}

			isInCustomScene = false;
			currentBundleContainer = null;
			currentLevelData = null;
			currentLevelContainer = null;
			config.presetButtonInteractable = true;
		}

        public static Harmony harmony;
        
		public static PluginConfigurator config;
		public static ConfigHeader levelUpdateNotifier;
        public static ConfigHeader errorText;
		public static ConfigDivision bundleDivision;

		public static KeyCodeField reloadFileKeybind;
		public static BoolField refreshCatalogOnBoot;
		public static BoolField levelUpdateNotifierToggle;
		public enum BundleSorting
		{
			Alphabetically,
			Author,
			LastPlayed
		}
		public static EnumField<BundleSorting> bundleSortingMode;

		private static string[] difficultyArr = new string[] { "HARMLESS", "LENIENT", "STANDARD", "VIOLENT" };

		public static string workingDir;

		private void Awake()
		{
			// Plugin startup logic
			instance = this;
			workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Addressables.InitializeAsync().WaitForCompletion();

			if (!Directory.Exists(tempFolderPath))
				Directory.CreateDirectory(tempFolderPath);

			LoadLastPlayedMap();
			LoadScripts();

			harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();

            SceneManager.activeSceneChanged += (before, after) =>
            {
                CheckIsInCustomScene(after);
				if (isInCustomScene)
					AngrySceneManager.PostSceneLoad();
			};

            gameFont = Addressables.LoadAssetAsync<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf").WaitForCompletion();
			notPlayedPreview = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked3.png").WaitForCompletion();
			lockedPreview = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked.png").WaitForCompletion();

			config = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
			config.postConfigChange += UpdateAllUI;
			config.SetIconWithURL(Path.Combine(workingDir, "plugin-icon.png"));

			levelUpdateNotifier = new ConfigHeader(config.rootPanel, "<color=lime>Level updates available!</color>", 16);
			levelUpdateNotifier.hidden = true;
			OnlineLevelsManager.onlineLevelsPanel = new ConfigPanel(config.rootPanel, "Online Levels", "b_onlineLevels", ConfigPanel.PanelFieldType.StandardWithIcon);
			OnlineLevelsManager.onlineLevelsPanel.SetIconWithURL(Path.Combine(workingDir, "online-icon.png"));
			OnlineLevelsManager.Init();

			StringListField difficultySelect = new StringListField(config.rootPanel, "Difficulty", "difficultySelect", difficultyArr, "VIOLENT");
            difficultySelect.onValueChange += (e) =>
            {
                selectedDifficulty = Array.IndexOf(difficultyArr, e.value);
                if (selectedDifficulty == -1)
                {
                    Debug.LogWarning("Invalid difficulty, setting to violent");
                    selectedDifficulty = 3;
                }
            };
            difficultySelect.TriggerValueChangeEvent();

			ConfigPanel settingsPanel = new ConfigPanel(config.rootPanel, "Settings", "p_settings", ConfigPanel.PanelFieldType.Standard);
			
			reloadFileKeybind = new KeyCodeField(settingsPanel, "Reload File", "f_reloadFile", KeyCode.None);
			settingsPanel.hidden = true;
			bundleSortingMode = new EnumField<BundleSorting>(settingsPanel, "Bundle sorting", "s_bundleSortingMode", BundleSorting.Alphabetically);
			bundleSortingMode.onValueChange += (e) => SortBundles();

			new ConfigHeader(settingsPanel, "Online");
			new ConfigHeader(settingsPanel, "Online level catalog and thumbnails are cached, if there are no updates only 64 bytes of data is downloaded per refresh", 12, TextAnchor.UpperLeft);
			refreshCatalogOnBoot = new BoolField(settingsPanel, "Refresh online catalog on boot", "s_refreshCatalogBoot", true);
			levelUpdateNotifierToggle = new BoolField(settingsPanel, "Notify on level updates", "s_levelUpdateNofify", true);
			levelUpdateNotifierToggle.onValueChange += (e) =>
			{
				OnlineLevelsManager.CheckLevelUpdateText();
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

			errorText = new ConfigHeader(config.rootPanel, "", 16, TextAnchor.UpperLeft); ;

			new ConfigHeader(config.rootPanel, "Level Bundles");
			bundleDivision = new ConfigDivision(config.rootPanel, "div_bundles");
			ScanForLevels();

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

			if (refreshCatalogOnBoot.value)
				OnlineLevelsManager.RefreshAsync();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

		float lastPress = 0;
		private void OnGUI()
		{
			if (reloadFileKeybind.value == KeyCode.None)
				return;

			if (!isInCustomScene)
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
				currentBundleContainer.UpdateScenes(false);
			}
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
	}
}
