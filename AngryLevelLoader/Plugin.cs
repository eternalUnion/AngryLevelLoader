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
    [BepInDependency(PluginConfig.PluginConfiguratorController.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_NAME = "AngryLevelLoader";
        public const string PLUGIN_GUID = "com.eternalUnion.angryLevelLoader";
        public const string PLUGIN_VERSION = "2.0.0";
		public static string tempFolderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Temp");

		public static Dictionary<string, RudeLevelData> idDictionary = new Dictionary<string, RudeLevelData>();

		public static Dictionary<string, AngryBundleContainer> angryBundles = new Dictionary<string, AngryBundleContainer>();
		public static Dictionary<string, AngryBundleContainer> failedBundles = new Dictionary<string, AngryBundleContainer>();

		// Scan for levels. This does NOT reload the files, only
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
				else if (failedBundles.TryGetValue(path, out AngryBundleContainer failedBundle))
				{
					try
					{
						failedBundle.UpdateScenes(false);
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Exception thrown while loading level bundle: {e}");
						failedBundle.statusText.text = "Error: " + e;
                        errorText.text += $"<color=red>Error loading {Path.GetFileNameWithoutExtension(path)}</color>\n";
						continue;
					}

					failedBundle.rootPanel.interactable = true;
					failedBundle.rootPanel.hidden = false;
					failedBundles.Remove(path);
					angryBundles[path] = failedBundle;
					continue;
				}

				AngryBundleContainer level = null;

				try
				{
					level = new AngryBundleContainer(path);
					level.UpdateScenes(false);
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Exception thrown while loading level bundle: {e}");
					if (!string.IsNullOrEmpty(errorText.text))
						errorText.text += '\n';
					errorText.text += $"<color=red>Error loading {Path.GetFileNameWithoutExtension(path)}</color>. Check the logs for more information";

					if (level != null)
					{
						level.rootPanel.hidden = true;
                        level.statusText.text = "Error: " + e;
						failedBundles[path] = level;
					}
					continue;
				}

				angryBundles[path] = level;
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
        public static int selectedDifficulty;

        public static void CheckIsInCustomScene(Scene current)
        {
			foreach (AngryBundleContainer container in angryBundles.Values)
			{
				if (container.GetAllScenePaths().Contains(current.path))
				{
					isInCustomScene = true;
					currentLevelData = container.GetAllLevelData().Where(data => data.scenePath == current.path).First();
					currentLevelContainer = container.levels[container.GetAllLevelData().Where(data => data.scenePath == current.path).First().uniqueIdentifier];
					currentLevelContainer.discovered.value = true;
					currentLevelContainer.UpdateUI();

					return;
				}
			}

			isInCustomScene = false;
			currentLevelData = null;
			currentLevelContainer = null;
		}

        public static Harmony harmony;
        public static PluginConfigurator config;
        public static ConfigHeader errorText;
        private static string[] difficultyArr = new string[] { "HARMLESS", "LENIENT", "STANDARD", "VIOLENT" };

		private void Awake()
        {
			// Plugin startup logic
			Addressables.InitializeAsync().WaitForCompletion();

			if (!Directory.Exists(tempFolderPath))
				Directory.CreateDirectory(tempFolderPath);

			LoadScripts();

			config = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
			config.postConfigChange += UpdateAllUI;
			harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();

            SceneManager.activeSceneChanged += (before, after) =>
            {
                CheckIsInCustomScene(after);
			};

            gameFont = Addressables.LoadAssetAsync<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf").WaitForCompletion();
			notPlayedPreview = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked3.png").WaitForCompletion();
			lockedPreview = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked.png").WaitForCompletion();

            ButtonField openLevels = new ButtonField(config.rootPanel, "Open Levels Folder", "b_openLevelsFolder");
            openLevels.onClick += () =>
            {
                Application.OpenURL(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Levels"));
            };
            ButtonField reloadButton = new ButtonField(config.rootPanel, "Scan For Levels", "refreshButton");
            reloadButton.onClick += ScanForLevels;
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
            errorText = new ConfigHeader(config.rootPanel, "", 16, TextAnchor.UpperLeft); ;

			new ConfigHeader(config.rootPanel, "Level Bundles");
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

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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


	public static class AddressableTest
	{
		public static void TestAddressable()
		{
			Addressables.LoadContentCatalogAsync(Path.Combine(Application.dataPath, "Custom", "catalog.json"), true).WaitForCompletion();

			SceneHelper.LoadScene("Assets/Custom/base.unity.unity", false);
		}

		public static void ForceLoadAddressables()
		{
			Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Effects/Charge Effect.prefab").WaitForCompletion();
			Addressables.LoadAssetAsync<AudioMixer>("AllAudio").WaitForCompletion();
			Addressables.LoadAssetAsync<AudioMixer>("MusicAudio").WaitForCompletion();
			Addressables.LoadAssetAsync<AudioMixer>("GoreAudio").WaitForCompletion();
			Addressables.LoadAssetAsync<AudioMixer>("UnfreezableAudio").WaitForCompletion();
			Addressables.LoadAssetAsync<AudioMixer>("DoorAudio").WaitForCompletion();
			Addressables.LoadAssetAsync<AudioMixer>("DoorAudio").WaitForCompletion();
		}

		public static void TestBundles()
		{
			string bundlePath = Path.Combine(Application.dataPath, "Custom");

			foreach (string bundle in Directory.GetFiles(bundlePath).Where(file => file.EndsWith(".bundle")))
			{
				AssetBundle.LoadFromFile(bundle);
			}

			SceneManager.LoadScene("Assets/Custom/base.unity");
		}
	}
}
