using AngryLevelLoader.Containers;
using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Notifications;
using AngryUiComponents;
using Newtonsoft.Json;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AngryLevelLoader.Fields
{
    public class OnlineLevelField : CustomConfigField
    {
        private const string ASSET_PATH = "AngryLevelLoader/Fields/OnlineLevelField.prefab";

        public readonly string bundleGuid;
        public string bundleBuildHash;
        public long lastUpdate;

        private AngryBundleContainer _bundle = null;
        public AngryBundleContainer bundle
        {
            get
            {
                if (_bundle == null)
                    _bundle = Plugin.GetAngryBundleByGuid(bundleGuid);

                return _bundle;
            }
        }

        private RectTransform currentContainer = null;

        private UnityEvent onCancel = new UnityEvent();

        private Texture2D _previewImage;
        public Texture2D previewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;
                if (currentUi != null)
                    currentUi.thumbnail.texture = _previewImage;
            }
        }

        public void DownloadPreviewImage(string URL, bool force)
        {
            if (_previewImage != null && !force)
                return;

            UnityWebRequest req = UnityWebRequestTexture.GetTexture(URL);
            var handle = req.SendWebRequest();
            handle.completed += (e) =>
            {
                try
                {
                    if (req.isHttpError || req.isNetworkError)
                        return;

                    previewImage = DownloadHandlerTexture.GetContent(req);
                }
                finally
                {
                    req.Dispose();
                }
            };
        }

        private string _bundleName;
        public string bundleName
        {
            get => _bundleName;
            set
            {
                _bundleName = value;
                UpdateInfoText();
            }
        }

        private string _author;
        public string author
        {
            get => _author;
            set
            {
                _author = value;
                UpdateInfoText();
            }
        }

        private int _bundleFileSize;
        public int bundleFileSize
        {
            get => _bundleFileSize;
            set
            {
                _bundleFileSize = value;
                UpdateInfoText();
            }
        }

        private string GetFileSizeString()
        {
            const int kilobyteSize = 1024;
            const int megabyteSize = 1024 * 1024;

            if (bundleFileSize >= megabyteSize)
                return $"{((float)_bundleFileSize / megabyteSize).ToString("0.00")} MB";
            if (bundleFileSize >= kilobyteSize)
                return $"{((float)_bundleFileSize / kilobyteSize).ToString("0.00")} KB";
            return $"{_bundleFileSize} B";
        }

        public enum OnlineLevelStatus
        {
            installed,
            notInstalled,
            updateAvailable
        }

        public enum ErrorStatus
        {
            NoError,
            NetworkError,
            ValidationError
        }

        private OnlineLevelStatus _status = OnlineLevelStatus.notInstalled;
        public OnlineLevelStatus status
        {
            get => _status;
            set
            {
                _status = value;
                UpdateInfoText();
            }
        }

        private ErrorStatus _errorStatus = ErrorStatus.NoError;
        public ErrorStatus errorStatus
        {
            get => _errorStatus;
            set
            {
                _errorStatus = value;
                UpdateInfoText();
            }
        }

        private string GetStatusString()
        {
            if (_errorStatus != ErrorStatus.NoError)
            {
                if (_errorStatus == ErrorStatus.NetworkError)
                    return $"<color=red><b>Network error</b></color>";
                else if (_errorStatus == ErrorStatus.ValidationError)
                    return $"<color=red><b>Validation error</b></color>";
            }

            if (_status == OnlineLevelStatus.notInstalled)
                return $"<color=red>Not installed</color>";
            else if (_status == OnlineLevelStatus.updateAvailable)
                return $"<color=cyan>Update available</color>";
            else
                return $"<color=lime>Installed</color>";
        }

        public void UpdateInfoText()
        {
            if (currentUi == null)
                return;
            currentUi.infoText.text = $"{_bundleName}\n<color=#909090>Author: {_author}\nSize: {GetFileSizeString()}</color>\n{GetStatusString()}";
        }

        private bool installActive = false;
        private AngryOnlineLevelFieldComponent currentUi;

        internal class DisableWhenHidden : MonoBehaviour
        {
            void OnDisable()
            {
                gameObject.SetActive(false);
            }
        }

        private bool inited = false;
        public OnlineLevelField(ConfigPanel parentPanel, string guid) : base(parentPanel, 600, 170)
        {
            bundleGuid = guid;

            inited = true;
            if (currentContainer != null)
                OnCreateUI(currentContainer);
        }

        protected override void OnCreateUI(RectTransform fieldUI)
        {
            currentContainer = fieldUI;
            if (!inited)
                return;

            currentUi = Addressables.InstantiateAsync(ASSET_PATH, currentContainer.transform.parent).WaitForCompletion().GetComponent<AngryOnlineLevelFieldComponent>();
            UnityEngine.Object.Destroy(fieldUI.gameObject);

            currentUi.thumbnail.texture = _previewImage;
            UpdateInfoText();

            currentUi.install.onClick.AddListener(StartDownload);
            currentUi.install.gameObject.AddComponent<DisableWhenHidden>();
            UIUtils.AddMouseEvents(currentUi.gameObject, currentUi.install,
                (e) =>
                {
                    if (installActive)
                        currentUi.install.gameObject.SetActive(true);
                },
                (e) =>
                {
                    currentUi.install.gameObject.SetActive(false);
                });

            currentUi.changelog.onClick.AddListener(() =>
            {
                LevelInfo onlineBundle = OnlineLevelsManager.catalog.Levels.Where(level => level.Guid == bundleGuid).First();
                LevelUpdateNotification notification = new LevelUpdateNotification();
                notification.currentHash = (bundle == null || status == OnlineLevelStatus.notInstalled) ? "" : bundle.bundleData.buildHash;
                notification.onlineInfo = onlineBundle;
                notification.callback = this;
                NotificationPanel.Open(notification);
            });
            currentUi.changelog.gameObject.AddComponent<DisableWhenHidden>();
            UIUtils.AddMouseEvents(currentUi.gameObject, currentUi.changelog,
                (e) =>
                {
                    if (!downloading)
                        currentUi.changelog.gameObject.SetActive(true);
                },
                (e) =>
                {
                    currentUi.changelog.gameObject.SetActive(false);
                });

            currentUi.update.onClick.AddListener(() =>
            {
                LevelInfo onlineBundle = OnlineLevelsManager.catalog.Levels.Where(level => level.Guid == bundleGuid).First();

                if (onlineBundle.Updates == null)
                {
                    StartDownload();
                }
                else
                {
                    if (bundle == null || string.IsNullOrEmpty(bundle.pathToAngryBundle) || !File.Exists(bundle.pathToAngryBundle))
                    {
                        StartDownload();
                        return;
                    }

                    LevelUpdateNotification notification = new LevelUpdateNotification();
                    notification.currentHash = bundle.bundleData.buildHash;
                    notification.onlineInfo = onlineBundle;
                    notification.callback = this;
                    NotificationPanel.Open(notification);
                }
            });

            currentUi.progressText.resizeTextForBestFit = true;
            currentUi.progressText.resizeTextMaxSize = currentUi.progressText.fontSize;

            currentUi.cancel.onClick.AddListener(() =>
            {
                if (onCancel != null)
                    onCancel.Invoke();
            });

            if (hierarchyHidden)
                currentUi.gameObject.SetActive(false);

            UpdateUI();
        }

        public void UpdateState()
        {
            if (bundle == null || string.IsNullOrEmpty(bundle.pathToAngryBundle) || !File.Exists(bundle.pathToAngryBundle))
                status = OnlineLevelStatus.notInstalled;
            else if (bundle.bundleData.buildHash != bundleBuildHash)
                status = OnlineLevelStatus.updateAvailable;
            else
                status = OnlineLevelStatus.installed;
        }

        public void UpdateUI()
        {
            UpdateState();
            if (currentUi == null)
                return;

            currentUi.downloadContainer.gameObject.SetActive(downloading);

            if (!downloading)
            {
                if (status == OnlineLevelStatus.notInstalled)
                {
                    installActive = true;
                    currentUi.install.gameObject.SetActive(false);
                    currentUi.update.gameObject.SetActive(false);
                }
                else if (status == OnlineLevelStatus.updateAvailable)
                {
                    installActive = false;
                    currentUi.install.gameObject.SetActive(false);
                    currentUi.update.gameObject.SetActive(true);
                }
                else
                {
                    installActive = false;
                    currentUi.install.gameObject.SetActive(false);
                    currentUi.update.gameObject.SetActive(false);
                }
            }
            else
            {
                installActive = false;
                currentUi.install.gameObject.SetActive(false);
                currentUi.update.gameObject.SetActive(false);
            }
        }

        public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
        {
            if (currentUi != null)
                currentUi.gameObject.SetActive(!hierarchyHidden);
        }

        public void StartDownload()
        {
            if (downloading)
                return;

            OnlineLevelsManager.instance.StartCoroutine(DownloadCoroutine());
        }

        public bool downloading = false;
        public IEnumerator DownloadCoroutine()
        {
            if (downloading)
                yield break;

            downloading = true;
            installActive = false;

            try
            {
                yield return DownloadAsync();
            }
            finally
            {
                downloading = false;

                UpdateUI();
                OnlineLevelsManager.CheckLevelUpdateText();
            }
        }
        
        public IEnumerator DownloadAsync()
        {
            errorStatus = ErrorStatus.NoError;

            if (currentUi != null)
            {
                currentUi.changelog.gameObject.SetActive(false);
                currentUi.install.gameObject.SetActive(false);
                currentUi.update.gameObject.SetActive(false);
                currentUi.downloadContainer.gameObject.SetActive(true);
                currentUi.progressBar.localScale = new Vector3(0, 1, 1);
            }

            LevelInfo level = OnlineLevelsManager.catalog.Levels.Where(level => level.Guid == bundleGuid).First();

            List<string> downloadedParts = new List<string>();
            string fileMegabytes = (bundleFileSize / (float)(1024 * 1024)).ToString("0.0");
            ulong downloadedBytes = 0;

            string tempDownloadDir = Path.Combine(Plugin.dataPath, "TempDownloads");
            if (!Directory.Exists(tempDownloadDir))
                Directory.CreateDirectory(tempDownloadDir);

            int partCount = level.Parts.Count;
            for (int i = 0; i < partCount; i++)
            {
                string tempDownloadPath = Path.Combine(tempDownloadDir, $"{bundleGuid}.angry{i}");
                if (File.Exists(tempDownloadPath))
                    File.Delete(tempDownloadPath);
                downloadedParts.Add(tempDownloadPath);

                UnityWebRequest req = new UnityWebRequest(level.Parts[i]);
                req.downloadHandler = new DownloadHandlerFile(tempDownloadPath);
                var handle = req.SendWebRequest();

                bool aborted = false;
                onCancel = new UnityEvent();
                onCancel.AddListener(() =>
                {
                    if (!downloading)
                        return;

                    aborted = true;
                    req.Abort();
                    downloading = false;
                    UpdateUI();
                });

                while (!handle.isDone)
                {
                    if (currentUi != null)
                    {
                        currentUi.progressBar.transform.localScale = new Vector3(Mathf.Clamp01((float)(req.downloadedBytes + downloadedBytes) / bundleFileSize), 1, 1);
                        string downloadedFileMegabytes = ((req.downloadedBytes + downloadedBytes) / (float)(1024 * 1024)).ToString("0.0");
                        currentUi.progressText.text = $"{downloadedFileMegabytes}/{fileMegabytes}\nMB\n(Part {i + 1}/{partCount})";
                    }

                    yield return new WaitForSecondsRealtime(0.5f);
                }

                onCancel = new UnityEvent();

                if (req.isHttpError || req.isNetworkError)
                {
                    if (!aborted)
                        errorStatus = ErrorStatus.NetworkError;

                    foreach (string part in downloadedParts)
                        if (File.Exists(part))
                            File.Delete(part);

                    yield break;
                }

                downloadedBytes += req.downloadedBytes;
                req.Dispose();
            }

            string destinationFolder = Plugin.levelsPath;
            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);
            string destinationFile = Path.Combine(destinationFolder, IOUtils.GetUniqueFileName(destinationFolder, IOUtils.GetPathSafeName(bundleName) + ".angry"));
            if (bundle != null && !string.IsNullOrEmpty(bundle.pathToAngryBundle) && File.Exists(bundle.pathToAngryBundle))
                destinationFile = bundle.pathToAngryBundle;

            using (FileStream str = File.Open(destinationFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                str.Position = 0;
                str.SetLength(0);

                foreach (string part in downloadedParts)
                {
                    using (FileStream fpart = File.Open(part, FileMode.Open, FileAccess.Read))
                    {
                        fpart.CopyTo(str);
                    }

                    File.Delete(part);
                }
            }

            // Make sure the file is not messed up
            bool valid = true;
            if (AngryFileUtils.TryGetAngryBundleData(destinationFile, out AngryBundleData data, out Exception e))
            {
                if (data.bundleGuid != bundleGuid)
                    valid = false;
                else if (data.buildHash != level.Hash)
                    Debug.LogWarning($"Downloaded bundle has hash {data.buildHash} but most recent one is {level.Hash}");
            }
            else
            {
                Debug.LogError($"Threw error while validating downloaded file\n{e}");
                valid = false;
            }

            if (!valid)
            {
                File.Delete(destinationFile);
                errorStatus = ErrorStatus.ValidationError;
                yield break;
            }

            if (bundle == null || bundle.pathToAngryBundle != destinationFile)
                Plugin.ProcessPath(destinationFile);
            else
                bundle.UpdateScenes(false, false);
        }

        // Update order for this field only, assuming every other field is ordered correctly
        public void UpdateOrder()
        {
            int order = 0;
            OnlineLevelField[] allBundles = OnlineLevelsManager.onlineLevels.Values.OrderBy(level => level.siblingIndex).ToArray();

            if (OnlineLevelsManager.sortFilter.value == OnlineLevelsManager.SortFilter.Name)
            {
                while (order < allBundles.Length)
                {
                    if (order == siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (string.Compare(bundleName, allBundles[order].bundleName) == -1)
                        break;

                    order += 1;
                }
            }
            else if (OnlineLevelsManager.sortFilter.value == OnlineLevelsManager.SortFilter.Author)
            {
                while (order < allBundles.Length)
                {
                    if (order == siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (string.Compare(author, allBundles[order].author) == -1)
                        break;

                    order += 1;
                }
            }
            else if (OnlineLevelsManager.sortFilter.value == OnlineLevelsManager.SortFilter.LastUpdate)
            {
                while (order < allBundles.Length)
                {
                    if (order == siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (lastUpdate > allBundles[order].lastUpdate)
                        break;

                    order += 1;
                }
            }

            if (order < 0)
                order = 0;
            else if (order >= allBundles.Length)
                order = allBundles.Length - 1;

            siblingIndex = order;
        }
    }
}
