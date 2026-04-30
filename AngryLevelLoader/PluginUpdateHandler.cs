using AngryLevelLoader.Extensions;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Notifications;
using AngryLevelLoader.Utils;
using Newtonsoft.Json;
using PluginConfig;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace AngryLevelLoader
{
    internal static class PluginUpdateHandler
    {
        public static async Task CheckPluginUpdate(bool userRequested = true)
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

            if (!userRequested)
            {
                bool pluginUpdated = InternalConfigManager.lastVersion.value != Plugin.PLUGIN_VERSION;
                bool updateReleased = new Version(Plugin.PLUGIN_VERSION) < new Version(json.latestVersion) && !InternalConfigManager.ignoreUpdates.value;
                bool newUpdateReleased = json.latestVersion != InternalConfigManager.updateLastVersion.value;

				if (!(pluginUpdated || updateReleased || newUpdateReleased))
                    return;
            }

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
            if (string.IsNullOrEmpty(InternalConfigManager.lastVersion.value) || new Version(InternalConfigManager.lastVersion.value) <= new Version("2.7.3"))
            {
				ConfigManager.defaultLeaderboardDifficulty.value = ConfigManager.DefaultLeaderboardDifficulty.Any;
            }

			// Reset ignore update on version change
			if (Plugin.PLUGIN_VERSION != InternalConfigManager.lastVersion.value)
				InternalConfigManager.ignoreUpdates.value = false;

            // Show update notification
            if (ConfigManager.checkForUpdates.value)
            {
				_ = CheckPluginUpdate(false);
			}
		}
    }
}
