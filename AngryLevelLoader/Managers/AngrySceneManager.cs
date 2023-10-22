using AngryLevelLoader.Containers;
using AngryLevelLoader.Notifications;
using PluginConfig;
using RudeLevelScript;
using RudeLevelScripts.Essentials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers
{
    public static class AngrySceneManager
    {
        #region Loaded Level Data Tracker

        private static string _currentLevel = "";
        private static bool _isInCustomLevel = false;
        private static AngryBundleContainer _currentBundleContainer = null;
        private static LevelContainer _currentLevelContainer = null;
        private static RudeLevelData _currentLevelData = null;

        internal static PropertyInfo SceneHelper_CurrentScene = typeof(SceneHelper).GetProperty(nameof(SceneHelper.CurrentScene));
        private static void CheckCurrentDataStatus()
        {
            string currentScene = SceneManager.GetActiveScene().path;
            if (currentScene != _currentLevel)
            {
                _currentLevel = currentScene;

                wasInCustomLevel = _isInCustomLevel;
                lastBundleContainer = _currentBundleContainer;
                lastLevelContainer = _currentLevelContainer;
                lastLevelData = _currentLevelData;

                foreach (AngryBundleContainer container in Plugin.angryBundles.Values)
                {
                    if (container.GetAllScenePaths().Contains(currentScene))
                    {
                        _isInCustomLevel = true;
                        _currentLevelData = container.GetAllLevelData().Where(data => data.scenePath == currentScene).First();
                        _currentBundleContainer = container;
                        _currentLevelContainer = container.levels[container.GetAllLevelData().Where(data => data.scenePath == currentScene).First().uniqueIdentifier];
                        _currentLevelContainer.discovered.value = true;
                        _currentLevelContainer.UpdateUI();
                        SceneHelper_CurrentScene.SetValue(null, _currentLevelData.uniqueIdentifier);
                        Plugin.config.presetButtonInteractable = false;
                        Plugin.difficultySelect.interactable = false;

                        return;
                    }
                }

                _isInCustomLevel = false;
                _currentBundleContainer = null;
                _currentLevelData = null;
                _currentLevelContainer = null;
                Plugin.config.presetButtonInteractable = true;
				Plugin.difficultySelect.interactable = true;
			}
        }

        public static bool isInCustomLevel
        {
            get
            {
                CheckCurrentDataStatus();
                return _isInCustomLevel;
            }
        }

        public static AngryBundleContainer currentBundleContainer
        {
            get
            {
                CheckCurrentDataStatus();
                return _currentBundleContainer;
            }
        }

        public static LevelContainer currentLevelContainer
        {
            get
            {
                CheckCurrentDataStatus();
                return _currentLevelContainer;
            }
        }

        public static RudeLevelData currentLevelData
        {
            get
            {
                CheckCurrentDataStatus();
                return _currentLevelData;
            }
        }

        public static bool wasInCustomLevel { get; private set; } = false;

        public static AngryBundleContainer lastBundleContainer { get; private set; } = null;

        public static LevelContainer lastLevelContainer { get; private set; } = null;

        public static RudeLevelData lastLevelData { get; private set; } = null;

        #endregion

        public static void LevelButtonPressed(AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
        {
            List<string> requiredScripts = ScriptManager.GetRequiredScriptsFromBundle(bundleContainer);

            List<string> scriptsToDownload = new List<string>();
            foreach (string script in requiredScripts)
            {
                if (ScriptManager.ScriptExists(script))
                {
                    // Download if out of date
                    ScriptInfo info = ScriptCatalogLoader.scriptCatalog == null ? null : ScriptCatalogLoader.scriptCatalog.Scripts.Where(s => s.FileName == script).FirstOrDefault();
                    if (info != null)
                    {
                        string hash = CryptographyUtils.GetMD5String(File.ReadAllBytes(Path.Combine(Plugin.workingDir, "Scripts", script)));
                        if (hash != info.Hash)
                        {
                            if (Plugin.scriptUpdateIgnoreCustom.value)
                            {
                                if (info.Updates != null && !info.Updates.Contains(hash))
                                    continue;
                            }

                            scriptsToDownload.Add(script);
                        }
                    }
                }
                else
                {
                    // Download if not found locally
                    scriptsToDownload.Add(script);
                }
            }

            if (scriptsToDownload.Count != 0)
            {
                NotificationPanel.Open(new ScriptUpdateNotification(scriptsToDownload, requiredScripts, bundleContainer, levelContainer, levelData, levelName));
            }
            else
            {
                LoadLevelWithScripts(requiredScripts, bundleContainer, levelContainer, levelData, levelName);
            }
        }

        public static void LoadLevelWithScripts(List<string> scripts, AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
        {
            Stack<ScriptWarningNotification> notifications = new Stack<ScriptWarningNotification>();
            Plugin.scriptCertificateIgnore = Plugin.scriptCertificateIgnoreField.value.Split('\n').ToList();
            foreach (string script in scripts)
            {
                if (ScriptManager.ScriptLoaded(script))
                    continue;

                ScriptWarningNotification notification = null;

                if (!ScriptManager.ScriptExists(script))
                {
                    notification = new ScriptWarningNotification("<color=yellow>Missing Script</color>", $"Script {script} is missing and may cause issues in the level", "Cancel", "Continue", (inst) =>
                    {
                        inst.Close();
                        foreach (var not in notifications)
                            not.Close();
                    }, (inst) =>
                    {
                        inst.Close();
                        notifications.Pop();

                        if (notifications.Count == 0)
                        {
                            LoadLevel(bundleContainer, levelContainer, levelData, levelName);
                        }
                    });
                }
                else
                {
                    var result = ScriptManager.AttemptLoadScriptWithCertificate(script);

                    if (result == ScriptManager.LoadScriptResult.Loaded)
                        continue;

                    if (Plugin.scriptCertificateIgnore.Contains(script))
                    {
                        ScriptManager.ForceLoadScript(script);
                        continue;
                    }

                    notification = new ScriptWarningNotification("<color=red>Unverified Script</color>", $"Script {script} {(result == ScriptManager.LoadScriptResult.NoCertificate ? "has no certificate" : "has invalid certificate")}, loading scripts from unknown sources could be dangerous", "Cancel", "Load", (inst) =>
                    {
                        inst.Close();
                        foreach (var not in notifications)
                            not.Close();
                    }, (inst) =>
                    {
                        inst.Close();
                        notifications.Pop();

                        ScriptManager.ForceLoadScript(script);

                        if (notifications.Count == 0)
                        {
                            LoadLevel(bundleContainer, levelContainer, levelData, levelName);
                        }
                    },
                    "Don't Ask Again For This Script",
                    (inst) =>
                    {
                        Plugin.scriptCertificateIgnore.Add(script);
                        Plugin.scriptCertificateIgnoreField.value = string.Join("\n", Plugin.scriptCertificateIgnore);

                        inst.Close();
                        notifications.Pop();

                        ScriptManager.ForceLoadScript(script);

                        if (notifications.Count == 0)
                        {
                            LoadLevel(bundleContainer, levelContainer, levelData, levelName);
                        }
                    });
                }

                if (notification != null)
                {
                    notifications.Push(notification);
                    NotificationPanel.Open(notification);
                }
            }

            if (notifications.Count == 0)
                LoadLevel(bundleContainer, levelContainer, levelData, levelName);
        }

        #region DifficultyHandle
        public static void SetToUltrapainDifficulty()
        {
            MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", 5);
            Ultrapain.Plugin.ultrapainDifficulty = true;
            Ultrapain.Plugin.realUltrapainDifficulty = true;
        }

        public static void UnsetUltrapainDifficulty()
        {
            Ultrapain.Plugin.realUltrapainDifficulty = false;
        }

        public static void SetToHeavenOrHellDifficulty()
        {
            MyCoolMod.Plugin.isHeavenOrHell = true;
            MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", 3);
        }

        public static void UnsetHeavenOrHellDifficulty()
        {
            MyCoolMod.Plugin.isHeavenOrHell = false;
        }
        #endregion

        public static void LoadLevel(AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelPath)
        {
            _isInCustomLevel = true;
            _currentBundleContainer = bundleContainer;
            _currentLevelContainer = levelContainer;
            _currentLevelData = levelData;
            _currentLevel = levelPath;

            Plugin.config.presetButtonInteractable = false;
            
            if (Plugin.ultrapainLoaded)
            {
                UnsetUltrapainDifficulty();
            }
            if (Plugin.heavenOrHellLoaded)
            {
                UnsetHeavenOrHellDifficulty();
            }

            if (Plugin.selectedDifficulty == 4)
            {
                SetToUltrapainDifficulty();
            }
            else if (Plugin.selectedDifficulty == 5)
            {
                SetToHeavenOrHellDifficulty();
            }
            else
            {
                MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", Plugin.selectedDifficulty);
            }

            SceneHelper.LoadScene(levelPath);
            Plugin.UpdateLastPlayed(bundleContainer);
        }

        public static void PostSceneLoad()
        {
            currentLevelContainer.AssureSecretsSize();

            string secretString = currentLevelContainer.secrets.value;
            foreach (Bonus bonus in Resources.FindObjectsOfTypeAll<Bonus>().Where(bonus => bonus.gameObject.scene.path == currentLevelData.scenePath))
            {
                if (bonus.gameObject.scene.path != currentLevelData.scenePath)
                    continue;

                if (bonus.secretNumber >= 0 && bonus.secretNumber < secretString.Length && secretString[bonus.secretNumber] == 'T')
                {
                    bonus.beenFound = true;
                    bonus.BeenFound();
                }
            }
        }
    }
}
