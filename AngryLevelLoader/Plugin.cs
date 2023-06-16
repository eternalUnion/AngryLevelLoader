using BepInEx;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        public class LevelAsset
        {
            public AssetBundle sceneBundle;
            public AssetBundle assetBundle;
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
                try
                {
                    assetBundle.Unload(true);
                }
                catch (Exception) { }

                if (!File.Exists(path))
                {
                    statusText.text = "Could not find the file";
                    return;
                }

                sceneBundle = AssetBundle.LoadFromFile(path);
                if (File.Exists(path + "_assets"))
                    assetBundle = AssetBundle.LoadFromFile(path + "_assets");

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
                if (File.Exists(path + "_assets"))
                    assetBundle = AssetBundle.LoadFromFile(path + "_assets");

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
                    if (path.EndsWith("_assets"))
                        continue;

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

        private void Awake()
        {
            // Plugin startup logic
            config = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);

            ButtonField reloadButton = new ButtonField(config.rootPanel, "Scan For Levels", "refreshButton");
            reloadButton.onClick += ReloadBundles;
            new ConfigHeader(config.rootPanel, "Level Bundles");
            ReloadBundles();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
