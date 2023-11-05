using AngryLevelLoader.Managers;
using AngryLevelLoader.Notifications;
using Newtonsoft.Json;
using PluginConfig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AngryLevelLoader
{
    public static class PluginUpdateHandler
    {
        public static async Task CheckPluginUpdate(bool userRequested = true)
        {
            UnityWebRequest infoReq = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevelLoader, "AngryLevelLoader/PluginInfo.json"));
            infoReq.downloadHandler = new DownloadHandlerBuffer();
            await infoReq.SendWebRequest();

            if (infoReq.isHttpError || infoReq.isNetworkError)
            {
                Plugin.logger.LogError("Could not download plugin data");
                infoReq.Dispose();
                Plugin.openButtons.SetButtonInteractable(1, true);
                return;
            }

            string text = infoReq.downloadHandler.text;
            int startIndex = text.IndexOf('{');
            if (startIndex > 0)
                text = text.Substring(startIndex);
            PluginInfoJson json = JsonConvert.DeserializeObject<PluginInfoJson>(text);
			Plugin.openButtons.SetButtonInteractable(1, true);
			infoReq.Dispose();

            if (!userRequested)
            {
                bool pluginUpdated = Plugin.lastVersion.value != Plugin.PLUGIN_VERSION;
                bool updateReleased = new Version(Plugin.PLUGIN_VERSION) < new Version(json.latestVersion) && !Plugin.ignoreUpdates.value;
                bool newUpdateReleased = json.latestVersion != Plugin.updateLastVersion.value;

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

            if (Directory.Exists(oldLevelsPath) && !Path.GetFullPath(Plugin.configDataPath.value).StartsWith(Path.GetFullPath(Plugin.workingDir)))
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
                            IOUtils.DirectoryCopy(unpackedLevel, destinationDir, true, true);
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
                    IOUtils.TryCreateDirectoryForFile(newOnlineCachePath);

                    IOUtils.DirectoryCopy(oldOnlineCachePath, newOnlineCachePath, true, true);
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
                    IOUtils.TryCreateDirectoryForFile(newLastPlayedMapPath);

                    File.Move(oldLastPlayedMapPath, newLastPlayedMapPath);
                }
                else
                {
                    File.Delete(oldLastPlayedMapPath);
                }

                Plugin.LoadLastPlayedMap();
            }

            // Reset ignore update on version change
            if (Plugin.PLUGIN_VERSION != Plugin.lastVersion.value)
                Plugin.ignoreUpdates.value = false;

            // Show update notification
            if (Plugin.checkForUpdates.value)
            {
				_ = CheckPluginUpdate(false);
			}
		}
    }
}
