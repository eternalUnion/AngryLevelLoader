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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers
{
    public static class AngrySceneManager
    {
        #region Loaded Level Data Tracker

        private static string _currentLevel = "";
        private static bool _isInCustomLevel = false;
        private static BundleContainer _currentBundleContainer = null;
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

                foreach (BundleContainer container in Plugin.GetAllBundleContainers())
                {
                    if (container.GetAllScenePaths().Contains(currentScene))
                    {
						_isInCustomLevel = true;
                        _currentLevelData = container.GetAllRudeLevelData().Where(data => data.scenePath == currentScene).First();
                        _currentBundleContainer = container;
                        _currentBundleContainer.TryGetLevelContainer(_currentLevelData.uniqueIdentifier, out _currentLevelContainer);
                        _currentLevelContainer.LevelDiscovered = true;
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

        public static BundleContainer currentBundleContainer
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

        public static BundleContainer lastBundleContainer { get; private set; } = null;

        public static LevelContainer lastLevelContainer { get; private set; } = null;

        public static RudeLevelData lastLevelData { get; private set; } = null;

        #endregion

        internal static void LevelButtonPressed(LevelContainer levelContainer)
        {
            void ContinueLoadLevel()
            {
                List<string> requiredScripts = ScriptManager.GetRequiredScriptsFromBundle(levelContainer.bundleContainer);

                List<string> scriptsToDownload = new List<string>();
                foreach (string script in requiredScripts)
                {
                    if (ScriptManager.ScriptExists(script))
                    {
                        // Download if out of date
                        ScriptInfo info = OnlineScriptsManager.ScriptCatalog == null ? null : OnlineScriptsManager.ScriptCatalog.Scripts.Where(s => s.FileName == script).FirstOrDefault();
                        if (info != null)
                        {
                            string hash = AngryCryptographyUtils.GetMD5String(File.ReadAllBytes(Path.Combine(Plugin.workingDir, "Scripts", script)));
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
                    NotificationPanel.Open(new ScriptUpdateNotification(levelContainer, scriptsToDownload));
                }
                else
                {
                    LoadLevelWithScripts(requiredScripts, levelContainer);
                }
            }

            if (levelContainer.bundleContainer.EpilepsyWarning && !InternalConfigManager.ignoreEpilepsyWarning.value)
            {
                EpilepsyWarningNotification notification = new EpilepsyWarningNotification(ContinueLoadLevel, "Play", "Play and do not ask again");
                NotificationPanel.Open(notification);
            }
            else
            {
                ContinueLoadLevel();
            }
        }

        internal static void LoadLevelWithScripts(List<string> scripts, LevelContainer levelContainer)
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
                            LoadLevel(levelContainer);
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
                            LoadLevel(levelContainer);
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
                            LoadLevel(levelContainer);
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
                LoadLevel(levelContainer);
        }

		/// <summary>
		/// Load an angry level asynchronously. This method does not load the custom scripts.
        /// Required custom scripts must be checked with <see cref="ScriptManager.GetRequiredScriptsFromBundle(BundleContainer)"/>.
        /// Locally installed custom scripts can be checked with <see cref="ScriptManager.ScriptExists(string)"/>.
        /// Custom scripts available online can be checked from <see cref="OnlineScriptsManager"/>.
        /// Note that downloading and updating custom scripts require user consent.
		/// </summary>
        /// <returns>True if the custom level was successfully loaded. False if the bundle no longer exists or level no longer exists inside the bundle.</returns>
		public static async Task<bool> LoadLevel(LevelContainer levelContainer, bool showBlocker = true)
        {
            BundleContainer bundleContainer = levelContainer.bundleContainer;
            if (!bundleContainer.Loaded)
            {
                // Must load bundle into addressables
                while (bundleContainer.Updating)
                    await Task.Yield();
                Task loadTask = bundleContainer.ReloadBundle(false, false);
                await loadTask;

                if (!bundleContainer.Loaded)
                {
                    Plugin.logger.LogError($"Tried to load level {levelContainer.LevelName}, but the bundle could not be loaded into memory!");
                    if (loadTask.Exception != null)
                        Plugin.logger.LogError(loadTask.Exception);

                    return false;
                }
            }

            if (!bundleContainer.TryGetRudeLevelData(levelContainer.levelId, out RudeLevelData rudeLevelData))
            {
                Plugin.logger.LogError($"Tried to load level {levelContainer.LevelName}, but the rude level data could not be found!");
                return false;
            }

            _isInCustomLevel = true;
            _currentBundleContainer = levelContainer.bundleContainer;
            _currentLevelContainer = levelContainer;
            _currentLevelData = rudeLevelData;
            _currentLevel = rudeLevelData.scenePath;
			ConfigManager.config.presetButtonInteractable = false;

            foreach (AngryDifficulty difficulty in AngryDifficultyManager.Difficulties)
                difficulty.UnsetDifficulty();

            // No gamemode
            if (ConfigManager.difficultyField.gamemodeListValueIndex == 0)
            {
				AngryDifficultyManager.SelectedDifficulty.SetDifficulty();
			}
            // NoMo/NoMoW = Harmless
            else if (ConfigManager.difficultyField.gamemodeListValueIndex == 1 || ConfigManager.difficultyField.gamemodeListValueIndex == 2)
            {
                AngryDifficultyManager.HARMLESS.SetDifficulty();
			}

			int levelVersion = levelContainer.bundleContainer.BundleVersion;
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
			LastPlayedMapManager.UpdateLastPlayed(levelContainer.bundleContainer);
			
            Coroutine handler = SceneHelper.LoadSceneAsync(rudeLevelData.scenePath, noBlocker: !showBlocker);
            TaskCompletionSource<bool> sceneLoadCompletion = new TaskCompletionSource<bool>();
            handler.ContinueWith(SceneHelper.Instance, () => sceneLoadCompletion.SetResult(true));
            await sceneLoadCompletion.Task;

            return true;
        }

        internal static void PostSceneLoad()
        {
			Vector3 defaultGravity = new Vector3(0, -40, 0);
			Physics.gravity = defaultGravity;
			
            SceneHelperPatches.forceDisableIsInCustomLevel = false;

            foreach (Bonus bonus in Resources.FindObjectsOfTypeAll<Bonus>().Where(bonus => bonus.gameObject.scene.path == currentLevelData.scenePath && bonus.GetComponent<IgnoreSecret>() == null))
            {
                if (bonus.gameObject.scene.path != currentLevelData.scenePath)
                    continue;

                if (currentLevelContainer.SecretDiscovered(bonus.secretNumber))
                {
                    bonus.beenFound = true;
                    bonus.BeenFound();
                }
            }
        }
    }
}
