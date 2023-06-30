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
        public const string PLUGIN_VERSION = "1.0.0";

        public static void ReplaceShaders()
        {
			foreach (Renderer rnd in Resources.FindObjectsOfTypeAll(typeof(Renderer)))
			{
                if (rnd.transform.parent != null && rnd.transform.parent.name == "Virtual Camera")
                    continue;

				foreach (Material mat in rnd.materials)
				{
					if (shaderDictionary.TryGetValue(mat.shader.name, out Shader shader))
					{
						mat.shader = shader;
					}
				}
			}
		}

		public static void LinkMixers()
        {
            if (AudioMixerController.instance == null)
                return;

            AudioMixer[] realMixers = new AudioMixer[5]
            {
				AudioMixerController.instance.allSound,
				AudioMixerController.instance.musicSound,
				AudioMixerController.instance.goreSound,
				AudioMixerController.instance.doorSound,
				AudioMixerController.instance.unfreezeableSound
			};

            AudioMixer[] allMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();

			Dictionary<AudioMixerGroup, AudioMixerGroup> groupConversionMap = new Dictionary<AudioMixerGroup, AudioMixerGroup>();
			foreach (AudioMixer mixer in allMixers.Where(_mixer => _mixer.name.EndsWith("_rude")).AsEnumerable())
			{
                AudioMixerGroup rudeGroup = mixer.FindMatchingGroups("")[0];

                string realMixerName = mixer.name.Substring(0, mixer.name.Length - 5);
                AudioMixer realMixer = realMixers.Where(mixer => mixer.name == realMixerName).First();
                AudioMixerGroup realGroup = realMixer.FindMatchingGroups("")[0];

                groupConversionMap[rudeGroup] = realGroup;
                Debug.Log($"{mixer.name} => {realMixer.name}");
            }

			foreach (AudioSource source in Resources.FindObjectsOfTypeAll<AudioSource>())
            {
                if (source.outputAudioMixerGroup != null && groupConversionMap.TryGetValue(source.outputAudioMixerGroup, out AudioMixerGroup realGroup))
                {
                    source.outputAudioMixerGroup = realGroup;
                }
            }
        }

        public static Dictionary<string, AngryBundleContainer> angryBundles = new Dictionary<string, AngryBundleContainer>();
        private static Dictionary<string, AngryBundleContainer> failedBundles = new Dictionary<string, AngryBundleContainer>();
		public static List<RudeLevelData> currentDatas = new List<RudeLevelData>();
		public static void ReloadBundles()
        {
            errorText.text = "";
            foreach (KeyValuePair<string, AngryBundleContainer> pair in angryBundles)
                pair.Value.rootPanel.interactable = false;

            string bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Levels");
            if (!Directory.Exists(bundlePath))
            {
                Debug.LogWarning("Could not find the Levels folder at " + bundlePath);
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
						failedBundle.UpdateScenes();
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
					level.UpdateScenes();
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Exception thrown while loading level bundle: {e}");
					if (!string.IsNullOrEmpty(errorText.text))
						errorText.text += '\n';
					errorText.text += $"<color=red>Error loading {Path.GetFileNameWithoutExtension(path)}</color>";

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

        public static IEnumerable GetAllData()
        {
            foreach (AngryBundleContainer container in angryBundles.Values)
            {
                if (!container.rootPanel.interactable)
                    continue;

                foreach (RudeLevelData data in container.GetAllLevelData())
                    yield return data;
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
				Debug.Log("Loaded " + Assembly.LoadFile(scriptPath).FullName);
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
					currentLevelContainer = container.levels[current.path];
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
			LoadScripts();

			config = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
			config.postConfigChange += UpdateAllUI;
			harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
            InitShaderDictionary();

            SceneManager.activeSceneChanged += (before, after) =>
            {
                CheckIsInCustomScene(after);
			};

            gameFont = LoadObject<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf");
			notPlayedPreview = LoadObject<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked3.png");
			lockedPreview = LoadObject<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked.png");

            ButtonField openLevels = new ButtonField(config.rootPanel, "Open Levels Folder", "b_openLevelsFolder");
            openLevels.onClick += () =>
            {
                Application.OpenURL(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Levels"));
            };
            ButtonField reloadButton = new ButtonField(config.rootPanel, "Scan For Levels", "refreshButton");
            reloadButton.onClick += ReloadBundles;
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
            ReloadBundles();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

		public static ResourceLocationMap resourceMap = null;
        private static void InitResourceMap()
        {
			if (resourceMap == null)
			{
				Addressables.InitializeAsync().WaitForCompletion();
				resourceMap = Addressables.ResourceLocators.First() as ResourceLocationMap;
			}
		}

		public static T LoadObject<T>(string path)
		{
            InitResourceMap();

			Debug.Log($"Loading {path}");
			KeyValuePair<object, IList<IResourceLocation>> obj;

			try
			{
				obj = resourceMap.Locations.Where(
					(KeyValuePair<object, IList<IResourceLocation>> pair) =>
					{
						return (pair.Key as string) == path;
						//return (pair.Key as string).Equals(path, StringComparison.OrdinalIgnoreCase);
					}).First();
			}
			catch (Exception) { return default(T); }

			return Addressables.LoadAsset<T>(obj.Value.First()).WaitForCompletion();
		}

		public static Dictionary<string, Shader> shaderDictionary = new Dictionary<string, Shader>();
        private void InitShaderDictionary()
        {
            InitResourceMap();
            foreach (KeyValuePair<object, IList<IResourceLocation>> pair in resourceMap.Locations)
            {
                string path = pair.Key as string;
                if (!path.EndsWith(".shader"))
                    continue;

                Shader shader = LoadObject<Shader>(path);
                shaderDictionary[shader.name] = shader;
            }

            shaderDictionary.Remove("ULTRAKILL/PostProcessV2");
        }
    }

    public static class RudeInterface
    {
		public static char INCOMPLETE_LEVEL_CHAR = '-';
		public static char GetLevelRank(string levelId)
        {
			foreach (AngryBundleContainer container in Plugin.angryBundles.Values)
			{
				foreach (RudeLevelData data in container.GetAllLevelData())
				{
					if (data.uniqueIdentifier == levelId)
					{
						return container.levels[data.scenePath].finalRank.value[0];
					}
				}
			}

			return INCOMPLETE_LEVEL_CHAR;
		}
	
        public static bool GetLevelChallenge(string levelId)
		{
			foreach (AngryBundleContainer container in Plugin.angryBundles.Values)
			{
				foreach (RudeLevelData data in container.GetAllLevelData())
				{
					if (data.uniqueIdentifier == levelId)
					{
						return data.levelChallengeEnabled && container.levels[data.scenePath].challenge.value;
					}
				}
			}

			return false;
		}

		public static bool GetLevelSecret(string levelId, int secretIndex)
		{
			if (secretIndex < 0)
				return false;

			foreach (AngryBundleContainer container in Plugin.angryBundles.Values)
			{
				foreach (RudeLevelData data in container.GetAllLevelData())
				{
					if (data.uniqueIdentifier == levelId)
					{
						if (secretIndex >= data.secretCount)
							return false;

						LevelContainer level = container.levels[data.scenePath];
						level.AssureSecretsSize();
						return level.secrets.value[secretIndex] == 'T';
					}
				}
			}

			return false;
		}
	}
}
