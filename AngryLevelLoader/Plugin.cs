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

        public static PluginConfigurator config;
        public static Dictionary<string, LevelAsset> bundles = new Dictionary<string, LevelAsset>();

        public static PropertyInfo p_SceneHelper_CurrentScene = typeof(SceneHelper).GetProperty("CurrentScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        public static PropertyInfo p_SceneHelper_LastScene = typeof(SceneHelper).GetProperty("LastScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        public static void ReplaceShaders()
        {
			foreach (Renderer rnd in Resources.FindObjectsOfTypeAll(typeof(Renderer)))
			{
                if (rnd.transform.parent != null && rnd.transform.parent.name == "Virtual Camera")
                    continue;

				foreach (Material mat in rnd.materials)
				{
					if (Plugin.shaderDictionary.TryGetValue(mat.shader.name, out Shader shader))
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

		public class LevelAsset
        {
            public AssetBundle sceneBundle;
            public string path;

            public ConfigPanel panel;
            public ButtonField reloadButton;
            public ConfigHeader reloadErrorText;
            public ConfigHeader statusText;
            public ConfigDivision sceneDiv;
            public Dictionary<string, ButtonField> scenes = new Dictionary<string, ButtonField>();

            public void ReloadAsset()
            {

            }

            public void UpdateReloadButton(Scene before, Scene after)
            {
                if (SceneHelper.CurrentScene == "Main Menu")
                {
                    reloadButton.interactable = true;
                    reloadErrorText.hidden = true;
                }
                else
                {
                    reloadButton.interactable = false;
                    reloadErrorText.hidden = false;
                }
            }

            public void UpdateScenes()
            {
                sceneDiv.interactable = false;
                statusText.text = "Reloading scenes...";
                statusText.hidden = false;

                try
                {
                    sceneBundle.Unload(true);
                }
                catch (Exception) { }

                if (!File.Exists(path))
                {
                    statusText.text = "Could not find the file";
                    return;
                }

                sceneBundle = AssetBundle.LoadFromFile(path);

                // Disable all scene buttons
                foreach (KeyValuePair<string, ButtonField> pair in scenes)
                    pair.Value.hidden = true;

                foreach (string scenePath in sceneBundle.GetAllScenePaths())
                {
                    if (scenes.TryGetValue(scenePath, out ButtonField button))
                    {
                        button.hidden = false;
                    }
                    else
                    {
                        ButtonField sceneButton = new ButtonField(sceneDiv, Path.GetFileName(scenePath), panel.guid + "_" + scenePath);
                        sceneButton.onClick += () =>
                        {
                            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
                            p_SceneHelper_LastScene.SetValue(null, p_SceneHelper_CurrentScene.GetValue(null) as string);
                            p_SceneHelper_CurrentScene.SetValue(null, scenePath);

                            IEnumerable<GameObject> GetAllSceneObjects()
                            {
                                Stack<GameObject> stack = new Stack<GameObject>();
                                foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
                                    stack.Push(obj);

                                while (stack.Count != 0)
                                {
                                    GameObject obj = stack.Pop();
                                    yield return obj;

                                    foreach (Transform t in obj.transform)
                                        stack.Push(t.gameObject);
                                }
                            }

                        };

                        SceneManager.sceneLoaded += (scene, mode) =>
                        {
                            if (scene.path == scenePath)
                            {
                                ReplaceShaders();
                                LinkMixers();
                            }
						};

                        scenes[scenePath] = sceneButton;
                    }
                }

                statusText.hidden = true;
                sceneDiv.interactable = true;
            }

            public LevelAsset(string path)
            {
                this.path = path;
                sceneBundle = AssetBundle.LoadFromFile(path);

                panel = new ConfigPanel(config.rootPanel, Path.GetFileName(path), Path.GetFileName(path));
                
                reloadButton = new ButtonField(panel, "Reload File", "reloadButton");
                reloadErrorText = new ConfigHeader(panel, "Level can only be reloaded in main menu", 20, TextAnchor.MiddleLeft);
                reloadErrorText.hidden = true;

                new SpaceField(panel, 5);

                new ConfigHeader(panel, "Scenes");
                statusText = new ConfigHeader(panel, "", 16, TextAnchor.MiddleLeft);
                statusText.hidden = true;
                sceneDiv = new ConfigDivision(panel, "sceneDiv_" + panel.guid);
                UpdateScenes();

                SceneManager.activeSceneChanged += UpdateReloadButton;
            }
        }

        public static void ReloadBundles()
        {
            foreach (KeyValuePair<string, LevelAsset> pair in bundles)
                pair.Value.panel.interactable = false;

            string bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Levels");
            if (Directory.Exists(bundlePath))
            {
                foreach (string path in Directory.GetFiles(bundlePath))
                {
                    if (bundles.TryGetValue(path, out LevelAsset levelAsset))
                    {
                        levelAsset.panel.interactable = true;
                        continue;
                    }

                    LevelAsset level;

                    try
                    {
                        level = new LevelAsset(path);
                    }
                    catch(Exception e)
                    {
                        Debug.LogWarning($"Exception thrown while loading level bundle: {e}");
                        continue;
                    }

                    bundles[path] = level;
                }
            }
        }

        public static Harmony harmony;

        private void Awake()
        {
            // Plugin startup logic
            config = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
            harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
            InitShaderDictionary();

            ButtonField reloadButton = new ButtonField(config.rootPanel, "Scan For Levels", "refreshButton");
            reloadButton.onClick += ReloadBundles;
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

    [HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Shader))]
    public static class MaterialShaderPatch_Ctor0
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Shader __0)
        {
            if (__0 != null && __0.name != null && Plugin.shaderDictionary.TryGetValue(__0.name, out Shader shader))
            {
                __0 = shader;
            }

			return true;
        }
    }

	[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Material))]
	public static class MaterialShaderPatch_Ctor1
	{
		[HarmonyPostfix]
		public static void Postfix(Material __instance)
		{
            if (__instance.shader != null && Plugin.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
            {
                __instance.shader = shader;
			}
		}
	}

    [HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(string))]
    public static class MaterialShaderPatch_Ctor2
    {
	    [HarmonyPostfix]
	    public static void Postfix(Material __instance)
	    {
            if (__instance.shader != null && Plugin.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
            {
                __instance.shader = shader;
			}
		}
    }
}
