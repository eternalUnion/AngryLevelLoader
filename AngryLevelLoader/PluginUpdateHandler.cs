using Newtonsoft.Json;
using PluginConfig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AngryLevelLoader
{
    public static class PluginUpdateHandler
    {
        public static IEnumerator CheckPluginUpdate(bool userRequested = true)
        {
            UnityWebRequest infoReq = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevelLoader, "AngryLevelLoader/PluginInfo.json"));
            infoReq.downloadHandler = new DownloadHandlerBuffer();

            var handle = infoReq.SendWebRequest();
            yield return handle;

            if (infoReq.isHttpError || infoReq.isNetworkError)
            {
                Debug.LogError("Could not download plugin data");
                infoReq.Dispose();
                Plugin.changelog.interactable = true;
                yield break;
            }

            string text = infoReq.downloadHandler.text;
            int startIndex = text.IndexOf('{');
            if (startIndex > 0)
                text = text.Substring(startIndex);
            PluginInfoJson json = JsonConvert.DeserializeObject<PluginInfoJson>(text);
            Plugin.changelog.interactable = true;
            infoReq.Dispose();

            if (!userRequested)
            {
                if (!(Plugin.lastVersion.value != Plugin.PLUGIN_VERSION || (Plugin.PLUGIN_VERSION != json.latestVersion && !Plugin.ignoreUpdates.value)))
                    yield break;
            }

            PluginUpdateNotification notification = new PluginUpdateNotification(json);
            NotificationPanel.Open(notification);
        }

        public static void Check()
        {
            // Levels folders are moved to data folder on version 2.3.0
            string oldLevelsPath = Path.Combine(Plugin.workingDir, "Levels");

            if (Directory.Exists(oldLevelsPath))
            {
                Debug.LogWarning("Version 2.3.0 migration: Moving levels from working dir to data folder");

                foreach (string levelFile in Directory.GetFiles(oldLevelsPath))
                {
                    string destinationFile = Path.Combine(Plugin.levelsPath, Path.GetFileName(levelFile));
                    if (File.Exists(destinationFile))
                        File.Delete(levelFile);
                    else
                    {
                        Debug.Log($"{levelFile} => {destinationFile}");
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
                            Debug.Log($"{unpackedLevel} => {destinationDir}");
                            IOUtils.DirectoryCopy(unpackedLevel, destinationDir, true, true);
                        }
                    }
                    Directory.Delete(oldUnpackedFolder, true);
                }
            }

            // Reset ignore update on version change
            if (Plugin.PLUGIN_VERSION != Plugin.lastVersion.value)
                Plugin.ignoreUpdates.value = false;

            // Show update notification
            if (Plugin.checkForUpdates.value)
                OnlineLevelsManager.instance.StartCoroutine(CheckPluginUpdate(false));
        }
    }
}
