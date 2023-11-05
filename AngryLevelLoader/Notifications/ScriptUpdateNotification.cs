using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryUiComponents;
using PluginConfig;
using RudeLevelScript;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AngryLevelLoader.Notifications
{
    public class ScriptUpdateNotification : NotificationPanel.Notification
    {
        private const string ASSET_PATH_PANEL = "AngryLevelLoader/Notifications/ScriptUpdateNotification.prefab";
        private const string ASSET_PATH_SCRIPT_INFO = "AngryLevelLoader/Notifications/ScriptUpdatePrefabs/ScriptUpdateInfo.prefab";

        private List<ScriptUpdateProgressField> fields = new List<ScriptUpdateProgressField>();

        class ScriptUpdateProgressField
        {
            public string scriptName;
            public string fileSizeText = "";
            public ScriptUpdateNotification caller;

            public enum ScriptStatus
            {
                Download,
                NotFound,
                Update
            }

            public ScriptStatus scriptStatus;

            public bool downloaded = false;
            public bool downloadError = false;

            public Text currentTextComp;
            public void SetStatusText(bool downloadedFromTask = false)
            {
                if (currentTextComp == null)
                    return;

                string currentText = scriptName + '\n';

                if (downloading && !downloadedFromTask)
                {
                    currentText += $"Downloading... {(int)(currentDllRequest.downloadProgress * 100)} %";
                }
                else
                {
                    if (downloaded)
                    {
                        if (ScriptManager.ScriptLoaded(scriptName))
                            currentText += "<color=red>RESTART REQUIRED</color>";
                        else
                            currentText += "<color=lime>Installed!</color>";
                    }
                    else
                    {
                        if (scriptStatus == ScriptStatus.NotFound)
                            currentText += "<color=red>Not Available Online</color>";
                        else if (scriptStatus == ScriptStatus.Update)
                            currentText += $"<color=cyan>Update Available</color> ({fileSizeText})";
                        else if (scriptStatus == ScriptStatus.Download)
                            currentText += $"<color=orange>Available online</color> ({fileSizeText})";

                        if (downloadError)
                            currentText += "<color=red>Download error</color>";
                    }
                }

                currentTextComp.text = currentText;
            }

            public void OnUI(Transform content)
            {
                currentTextComp = Addressables.InstantiateAsync(ASSET_PATH_SCRIPT_INFO, content).WaitForCompletion().GetComponentInChildren<Text>();

                if (string.IsNullOrEmpty(fileSizeText))
                {
                    if (ScriptCatalogLoader.TryGetScriptInfo(scriptName, out ScriptInfo info))
                    {
                        string prefix = "B";
                        float size = info.Size;
                        if (size >= 1024)
                        {
                            size /= 1024;
                            prefix = "KB";
                        }
                        if (size >= 1024)
                        {
                            size /= 1024;
                            prefix = "MB";
                        }

                        fileSizeText = $"{size.ToString("0.0")} {prefix}";
                    }
                    else
                    {
                        fileSizeText = "? MB";
                    }
                }

                SetStatusText();
            }

            private Task downloadTask = null;
            public bool downloading 
            {
                get => downloadTask != null && !downloadTask.IsCompleted;
            }

            public void StartDownload()
            {
                if (downloading || downloaded || scriptStatus == ScriptStatus.NotFound)
                    return;

				downloadTask = DownloadTask();
            }

            UnityWebRequest currentDllRequest;
            UnityWebRequest currentCertRequest;
            private async Task DownloadTask()
            {
                downloadError = false;

                try
                {
                    currentDllRequest = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevels, $"Scripts/{scriptName}"));
                    currentCertRequest = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevels, $"Scripts/{scriptName}.cert"));

                    string tempPath = Path.Combine(Plugin.workingDir, "TempDownloads");
                    if (!Directory.Exists(tempPath))
                        Directory.CreateDirectory(tempPath);

                    string tempDllPath = Path.Combine(tempPath, scriptName);
                    string tempCertPath = Path.Combine(tempPath, scriptName + ".cert");

                    currentDllRequest.downloadHandler = new DownloadHandlerFile(tempDllPath);
                    currentCertRequest.downloadHandler = new DownloadHandlerFile(tempCertPath);

                    _ = currentDllRequest.SendWebRequest();
					_ = currentCertRequest.SendWebRequest();

                    while (true)
                    {
                        if (currentDllRequest.isDone && currentCertRequest.isDone)
                            break;

                        SetStatusText();

                        await Task.Delay(500);
                    }

                    if (currentDllRequest.isNetworkError || currentDllRequest.isHttpError
                        || currentCertRequest.isNetworkError || currentCertRequest.isHttpError)
                    {
                        downloadError = true;
                    }
                    else
                    {
                        downloaded = true;

                        File.Copy(tempDllPath, Path.Combine(Plugin.workingDir, "Scripts", scriptName), true);
                        File.Copy(tempCertPath, Path.Combine(Plugin.workingDir, "Scripts", scriptName + ".cert"), true);
                    }

                    if (File.Exists(tempDllPath))
                        File.Delete(tempDllPath);
                    if (File.Exists(tempCertPath))
                        File.Delete(tempCertPath);

                    currentDllRequest.Dispose();
                    currentCertRequest.Dispose();
                }
                finally
                {
                    currentDllRequest = null;
                    currentCertRequest = null;

                    if (caller != null)
                        caller.CheckContinueButtonInteractable();

                    SetStatusText(true);
                }
            }

            public void StopDownload()
            {
                if (!downloading)
                    return;

                if (currentDllRequest != null)
                    currentDllRequest.Abort();
                if (currentCertRequest != null)
                    currentCertRequest.Abort();
            }

            public bool isDone
            {
                get
                {
                    return downloaded || scriptStatus == ScriptStatus.NotFound;
                }
            }
        }

        private AngryScriptUpdateNotificationComponent ui;

        public List<string> scripts;
        public AngryBundleContainer bundleContainer;
        public LevelContainer levelContainer;
        public RudeLevelData levelData;
        public string levelName;

        public ScriptUpdateNotification(IEnumerable<string> scriptsToDownload, List<string> scripts, AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
        {
            this.scripts = scripts;
            this.bundleContainer = bundleContainer;
            this.levelContainer = levelContainer;
            this.levelData = levelData;
            this.levelName = levelName;

            if (scriptsToDownload != null)
            {
                foreach (string script in scriptsToDownload)
                {
                    ScriptUpdateProgressField field = new ScriptUpdateProgressField();
                    field.scriptName = script;
                    field.caller = this;
                    if (ScriptCatalogLoader.TryGetScriptInfo(script, out var scriptInfo))
                    {
                        if (ScriptManager.ScriptExists(script))
                            field.scriptStatus = ScriptUpdateProgressField.ScriptStatus.Update;
                        else
                            field.scriptStatus = ScriptUpdateProgressField.ScriptStatus.Download;
                    }
                    else
                        field.scriptStatus = ScriptUpdateProgressField.ScriptStatus.NotFound;

                    fields.Add(field);
                }
            }
        }

        public void CheckContinueButtonInteractable()
        {
            if (ui == null)
                return;

            bool interactable = true;
            foreach (var field in fields)
            {
                if (field.downloading)
                {
                    interactable = false;
                    break;
                }
            }

            if (ui != null)
                ui.continueButton.interactable = interactable;
        }

        public override void OnUI(RectTransform panel)
        {
            ui = Addressables.InstantiateAsync(ASSET_PATH_PANEL, panel).WaitForCompletion().GetComponent<AngryScriptUpdateNotificationComponent>();

            ui.cancel.onClick.AddListener(() =>
            {
                foreach (var field in fields)
                {
                    try
                    {
                        field.StopDownload();
                    }
                    catch (Exception e)
                    {
                        Plugin.logger.LogError($"Exception thrown while stopping download of script update field\n{e}");
                    }
                }

                Close();
            });

            foreach (var field in fields)
                field.OnUI(ui.content);

            ui.update.onClick.AddListener(() =>
            {
                foreach (var field in fields)
                    field.StartDownload();

                if (ui != null)
                    ui.continueButton.interactable = false;

                CheckContinueButtonInteractable();
			});

            foreach (var field in fields)
            {
                if (field.downloading)
                {
                    ui.continueButton.interactable = false;
                    break;
                }
            }

            ui.continueButton.onClick.AddListener(() =>
            {
                Close();
                AngrySceneManager.LoadLevelWithScripts(scripts, bundleContainer, levelContainer, levelData, levelName);
            });
        }

        public static void Test()
        {
            NotificationPanel.Open(new ScriptUpdateNotification(new List<string>() { "eternalUnion.PhysicsExtensions.dll", "playerStats.dll" }, null, null, null, null, ""));
        }
    }
}
