using AngryLevelLoader.Containers;
using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Managers.LegacyPatches;
using AngryLevelLoader.Notifications;
using AngryLevelLoader.Patches;
using Logic;
using PluginConfig;
using RudeLevelScript;
using RudeLevelScripts.Essentials;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                        SceneHelper.CurrentScene = _currentLevelData.uniqueIdentifier;
						ConfigManager.config.presetButtonInteractable = false;
						ConfigManager.difficultyField.interactable = false;

                        return;
                    }
                }

                _isInCustomLevel = false;
                _currentBundleContainer = null;
                _currentLevelData = null;
                _currentLevelContainer = null;
				ConfigManager.config.presetButtonInteractable = true;
				ConfigManager.difficultyField.interactable = true;
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
            void ContinueLoadLevel()
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
                                if (ConfigManager.scriptUpdateIgnoreCustom.value)
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

            if (bundleContainer.bundleData.epilepsyWarning && !InternalConfigManager.ignoreEpilepsyWarning.value)
            {
                EpilepsyWarningNotification notification = new EpilepsyWarningNotification(ContinueLoadLevel, "Play", "Play and do not ask again");
                NotificationPanel.Open(notification);
            }
            else
            {
                ContinueLoadLevel();
            }
        }

        public static void LoadLevelWithScripts(List<string> scripts, AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
        {
            Stack<ScriptWarningNotification> notifications = new Stack<ScriptWarningNotification>();
			ConfigManager.scriptCertificateIgnore = ConfigManager.scriptCertificateIgnoreField.value.Split('\n').ToList();
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

                    if (ConfigManager.scriptCertificateIgnore.Contains(script))
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
						ConfigManager.scriptCertificateIgnore.Add(script);
						ConfigManager.scriptCertificateIgnoreField.value = string.Join("\n", ConfigManager.scriptCertificateIgnore);

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
            MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", 6);
            Ultrapain.Plugin.ultrapainDifficulty = true;
            Ultrapain.Plugin.realUltrapainDifficulty = true;
        }

        public static void UnsetUltrapainDifficulty()
        {
            Ultrapain.Plugin.realUltrapainDifficulty = false;
        }

        public static void SetToBananasDifficulty()
        {
			MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", 5);
		}

        public static void UnsetBananasDifficulty()
        {
			// It is sufficient for the difficulty to not be 5
		}

		public static void SetToBillionDifficulty(bool hardMode)
        {
            BillionDifficulty.Plugin.IsBrilliantBillion.SetValue(hardMode);
			MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", 19);
		}

        public static void UnsetBillionDifficulty()
        {
            // It is sufficient for the difficulty to not be 19
        }
        #endregion

        public static void LoadLevel(AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelPath, bool showBlocker = true)
        {
            _isInCustomLevel = true;
            _currentBundleContainer = bundleContainer;
            _currentLevelContainer = levelContainer;
            _currentLevelData = levelData;
            _currentLevel = levelPath;
			ConfigManager.config.presetButtonInteractable = false;
            
            if (Plugin.ultrapainLoaded)
            {
                UnsetUltrapainDifficulty();
            }
            if (Plugin.bananasDifficultyLoaded)
            {
                UnsetBananasDifficulty();
            }
            if (Plugin.billionDifficultyLoaded)
            {
                UnsetBillionDifficulty();
            }

            if (ConfigManager.difficultyField.gamemodeListValueIndex == 0)
            {
                if (Plugin.selectedDifficulty == 100)
                {
                    SetToUltrapainDifficulty();
                }
                else if (Plugin.selectedDifficulty == 101)
                {
                    SetToBananasDifficulty();
                }
                else if (Plugin.selectedDifficulty == 102)
                {
                    SetToBillionDifficulty(false);
                }
                else if (Plugin.selectedDifficulty == 103)
                {
                    SetToBillionDifficulty(true);
                }
                else
                {
                    MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", Plugin.selectedDifficulty);
                }
            }
            // NoMo/NoMoW = Harmless
            else if (ConfigManager.difficultyField.gamemodeListValueIndex == 1 || ConfigManager.difficultyField.gamemodeListValueIndex == 2)
            {
				MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", 0);
			}

			int levelVersion = bundleContainer.bundleData.bundleVersion;
            if (levelVersion == 6)
            {
                LegacyPatchManager.SetLegacyPatchState(LegacyPatchState.V6);
            }
            else if (levelVersion == 7)
            {
				LegacyPatchManager.SetLegacyPatchState(LegacyPatchState.V7);
			}
            else
            {
                LegacyPatchManager.SetLegacyPatchState(LegacyPatchState.None);
            }


            //Clear the map vars before loading the level.
            AngryMapVarManager.Instance.ResetStores();
			LastPlayedMapManager.UpdateLastPlayed(bundleContainer);
			SceneHelper.LoadScene(levelPath, noBlocker: !showBlocker);
        }

        public static void PostSceneLoad()
        {
            Physics.gravity = Plugin.defaultGravity;

			SceneHelperPatches.forceDisableIsInCustomLevel = false;
			currentLevelContainer.AssureSecretsSize();

            string secretString = currentLevelContainer.secrets.value;
            foreach (Bonus bonus in Resources.FindObjectsOfTypeAll<Bonus>().Where(bonus => bonus.gameObject.scene.path == currentLevelData.scenePath && bonus.GetComponent<IgnoreSecret>() == null))
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

        public static bool TryFindLevel(string id, out LevelContainer level)
        {
            level = null;

            foreach (AngryBundleContainer container in Plugin.angryBundles.Values)
            {
                foreach (LevelContainer levelContainer in container.levels.Values)
                {
                    if (levelContainer.data.uniqueIdentifier == id)
                    {
                        level = levelContainer;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
