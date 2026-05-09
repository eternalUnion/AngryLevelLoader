using AngryLevelLoader.Extensions;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Notifications;
using AngryLevelLoader.Utils;
using Newtonsoft.Json;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader
{
    internal static class PluginUpdateHandler
    {
        private static async Task PauseAndShowChangelog(PluginInfoJson json)
        {
			GameObject canvasObj = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvasObj == null)
			{
				Plugin.logger.LogWarning("Angry tried to find canvas, but failed");
                return;
			}

			// Find the options menu
			Transform optionsMenu = canvasObj.transform.Find("OptionsMenu");
			if (optionsMenu == null)
			{
				Plugin.logger.LogError("Angry tried to find the options menu but failed!");
				return;
			}

			if (SceneHelper.CurrentScene != "Main Menu" && !OptionsManager.Instance.paused)
            {
                OptionsManager.Instance.Pause();
                await Task.Yield();
            }

            OptionsManager.Instance.OpenOptions();
			await Task.Yield();

			PluginUpdateNotification notification = new PluginUpdateNotification(json);
			NotificationPanel.Open(notification);
		}

        public static async Task CheckForUpdate()
        {
			UnityWebRequest infoReq = new UnityWebRequest(AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevelLoader, "AngryLevelLoader/PluginInfo.json"));
			infoReq.downloadHandler = new DownloadHandlerBuffer();
			await infoReq.SendWebRequest();

			if (infoReq.result != UnityWebRequest.Result.Success)
			{
				Plugin.logger.LogError("Could not download plugin data");
				infoReq.Dispose();
				ConfigManager.openButtons.SetButtonInteractable(1, true);
				return;
			}

			string text = infoReq.downloadHandler.text;
			int startIndex = text.IndexOf('{');
			if (startIndex > 0)
				text = text.Substring(startIndex);

			PluginInfoJson json = JsonConvert.DeserializeObject<PluginInfoJson>(text);

            if (Version.TryParse(json.latestVersion, out Version jsonVer))
            {
                if (jsonVer <= new Version(Plugin.PLUGIN_VERSION))
                    return;
                if (Version.TryParse(InternalConfigManager.ignoreUpdateVersion.value, out Version configVer) && jsonVer == configVer)
                    return;
            }

            await AngryAsyncUtils.WaitUntilSceneLoaded("Main Menu");
            Plugin.logger.LogWarning("Update available, notifying user...");

            // Show update notification
			const string changelog_action = "changelog";
            const string remind_action = "remind";
			const string ignore_action = "ignore";

			Dictionary<string, string> actions = new()
            {
                { changelog_action, "View Changelog" },
                { remind_action, "Remind Me Later" },
                { ignore_action, "Ignore" },
            };

			uint notification_id = Notiffy.API.NotificationSystem.NotifySend("AngryLevelLoader updated!", $"Current: {Plugin.PLUGIN_VERSION}, Latest: {json.latestVersion}", actions: actions, iconFilePath: Path.Combine(Plugin.workingDir, "plugin-icon.png"));
            Notiffy.API.NotificationSystem.ActionInvoked += OnAction;
            Notiffy.API.NotificationSystem.NotificationDeleted += OnDeleted;

            void OnAction(uint id, string actionIdentifier)
            {
                if (id != notification_id)
                    return;

                if (actionIdentifier == ignore_action)
                {
                    InternalConfigManager.ignoreUpdateVersion.value = json.latestVersion;
				}
                else if (actionIdentifier == changelog_action)
                {
                    _ = PauseAndShowChangelog(json);
				}

				Notiffy.API.NotificationSystem.ActionInvoked -= OnAction;
				Notiffy.API.NotificationSystem.NotificationDeleted -= OnDeleted;
			}

			void OnDeleted(uint id)
			{
				if (id != notification_id)
					return;

				Notiffy.API.NotificationSystem.ActionInvoked -= OnAction;
				Notiffy.API.NotificationSystem.NotificationDeleted -= OnDeleted;
			}
		}

        public static async Task ShowChangelog()
        {
            UnityWebRequest infoReq = new UnityWebRequest(AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevelLoader, "AngryLevelLoader/PluginInfo.json"));
            infoReq.downloadHandler = new DownloadHandlerBuffer();
            await infoReq.SendWebRequest();

            if (infoReq.result != UnityWebRequest.Result.Success)
            {
                Plugin.logger.LogError("Could not download plugin data");
                infoReq.Dispose();
				ConfigManager.openButtons.SetButtonInteractable(1, true);
                return;
            }

            string text = infoReq.downloadHandler.text;
            int startIndex = text.IndexOf('{');
            if (startIndex > 0)
                text = text.Substring(startIndex);
            PluginInfoJson json = JsonConvert.DeserializeObject<PluginInfoJson>(text);
			ConfigManager.openButtons.SetButtonInteractable(1, true);
			infoReq.Dispose();

            PluginUpdateNotification notification = new PluginUpdateNotification(json);
            NotificationPanel.Open(notification);
        }

        public static void Check()
        {
            // Levels folders are moved to data folder on version 2.3.0
            string oldLevelsPath = Path.Combine(Plugin.workingDir, "Levels");

            if (Directory.Exists(oldLevelsPath) && !Path.GetFullPath(InternalConfigManager.configDataPath.value).StartsWith(Path.GetFullPath(Plugin.workingDir)))
            {
                Plugin.logger.LogWarning("Version 2.3.0 migration: Moving levels from working dir to data folder");

                foreach (string levelFile in Directory.GetFiles(oldLevelsPath))
                {
                    string destinationFile = Path.Combine(Plugin.levelsPath, Path.GetFileName(levelFile));
                    if (File.Exists(destinationFile))
                        File.Delete(levelFile);
                    else
                    {
                        Plugin.logger.LogInfo($"{levelFile} => {destinationFile}");
                        File.Move(levelFile, destinationFile);
                    }
                }
                Directory.Delete(oldLevelsPath, true);

                string oldUnpackedFolder = Path.Combine(Plugin.workingDir, "LevelsUnpacked");
                if (Directory.Exists (oldUnpackedFolder))
                {
                    foreach (string unpackedLevel in Directory.GetDirectories(oldUnpackedFolder))
                    {
                        string destinationDir = Path.Combine(Plugin.tempFolderPath, Path.GetFileName(unpackedLevel));
                        if (Directory.Exists(destinationDir))
                            Directory.Delete(unpackedLevel, true);
                        else
                        {
                            Plugin.logger.LogInfo($"{unpackedLevel} => {destinationDir}");
                            AngryIOUtils.DirectoryCopy(unpackedLevel, destinationDir, true, true);
                        }
                    }
                    Directory.Delete(oldUnpackedFolder, true);
                }
            }

            // Online cache moved to config on 2.5.x
            string oldOnlineCachePath = Path.Combine(Plugin.workingDir, "OnlineCache");
            if (Directory.Exists(oldOnlineCachePath))
            {
                Plugin.logger.LogWarning("Moving online cache folder to config (update 2.5.x)");

                string newOnlineCachePath = AngryPaths.OnlineCacheFolderPath;
                if (!Directory.Exists(newOnlineCachePath))
                {
                    AngryIOUtils.TryCreateDirectoryForFile(newOnlineCachePath);

                    AngryIOUtils.DirectoryCopy(oldOnlineCachePath, newOnlineCachePath, true, true);
                }
                else
                {
                    Directory.Delete(oldOnlineCachePath, true);
                }
            }

            // Last played map moved to config
            string oldLastPlayedMapPath = Path.Combine(Plugin.workingDir, "lastPlayedMap.txt");
            if (File.Exists(oldLastPlayedMapPath))
            {
                Plugin.logger.LogWarning("Moving last played map to config (update 2.5.x)");

                string newLastPlayedMapPath = AngryPaths.LastPlayedMapPath;
                if (!File.Exists(newLastPlayedMapPath))
                {
                    AngryIOUtils.TryCreateDirectoryForFile(newLastPlayedMapPath);

                    File.Move(oldLastPlayedMapPath, newLastPlayedMapPath);
                }
                else
                {
                    File.Delete(oldLastPlayedMapPath);
                }
				LastPlayedMapManager.LoadLastPlayedMap();
            }

			// 2.8.0: Added any difficulty leaderboard
            if (Version.TryParse(InternalConfigManager.lastVersion.value, out Version configVer) && configVer <= new Version("2.7.3"))
            {
				ConfigManager.defaultLeaderboardDifficulty.value = ConfigManager.DefaultLeaderboardDifficulty.Any;
            }

            // Download plugin info from GitHub and compare to current version
            _ = CheckForUpdate();

            InternalConfigManager.lastVersion.value = Plugin.PLUGIN_VERSION;
        }
    }
}
