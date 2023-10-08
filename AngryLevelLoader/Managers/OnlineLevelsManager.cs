using AngryLevelLoader.Containers;
using AngryLevelLoader.Fields;
using Newtonsoft.Json;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static AngryLevelLoader.Plugin;

namespace AngryLevelLoader.Managers
{
    #region JSON Object Types

    public class LevelInfo
    {
        public class UpdateInfo
        {
            public string Hash { get; set; }
            public string Message { get; set; }
        }

        public string Name { get; set; }
        public string Author { get; set; }
        public string Guid { get; set; }
        public int Size { get; set; }
        public string Hash { get; set; }
        public string ThumbnailHash { get; set; }

        public string ExternalLink { get; set; }
        public List<string> Parts;
        public long LastUpdate { get; set; }
        public List<UpdateInfo> Updates;
    }

    public class LevelCatalog
    {
        public List<LevelInfo> Levels;
    }

    public class ScriptInfo
    {
        public string FileName { get; set; }
        public string Hash { get; set; }
        public int Size { get; set; }
        public List<string> Updates;
    }

    public class ScriptCatalog
    {
        public List<ScriptInfo> Scripts;
    }

    #endregion

    public class LoadingCircleField : CustomConfigField
    {
        public static Sprite loadingIcon;
        private static bool init = false;
        public static void Init()
        {
            if (init)
                return;
            init = true;

            UnityWebRequest spriteReq = UnityWebRequestTexture.GetTexture("file://" + Path.Combine(workingDir, "loading-icon.png"));
            var handle = spriteReq.SendWebRequest();
            handle.completed += (e) =>
            {
                try
                {
                    if (spriteReq.isHttpError || spriteReq.isNetworkError)
                        return;

                    Texture2D texture = DownloadHandlerTexture.GetContent(spriteReq);
                    loadingIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    if (currentImage != null)
                        currentImage.sprite = loadingIcon;
                }
                finally
                {
                    spriteReq.Dispose();
                }
            };
        }

        private bool initialized = false;
        public LoadingCircleField(ConfigPanel parentPanel) : base(parentPanel, 600, 60)
        {
            Init();
            initialized = true;

            if (currentContainer != null)
                OnCreateUI(currentContainer);
        }

        internal class LoadingBarSpin : MonoBehaviour
        {
            private void Update()
            {
                transform.Rotate(Vector3.forward, Time.deltaTime * 360f);
            }
        }

        private RectTransform currentContainer;
        private GameObject currentUi;
        private static Image currentImage;
        protected override void OnCreateUI(RectTransform fieldUI)
        {
            currentContainer = fieldUI;
            if (!initialized)
                return;

            GameObject loadingCircle = currentUi = new GameObject();
            loadingCircle.transform.SetParent(fieldUI);
            RectTransform loadingRect = loadingCircle.AddComponent<RectTransform>();
            loadingRect.localScale = Vector3.one;
            loadingRect.sizeDelta = new Vector2(60, 60);
            loadingRect.anchorMin = loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.pivot = new Vector2(0.5f, 0.5f);
            loadingRect.anchoredPosition = Vector2.zero;

            loadingRect.gameObject.AddComponent<LoadingBarSpin>();
            currentImage = loadingRect.gameObject.AddComponent<Image>();
            currentImage.sprite = loadingIcon;

            if (hierarchyHidden)
                currentContainer.gameObject.SetActive(false);
        }

        public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
        {
            if (currentContainer != null)
                currentContainer.gameObject.SetActive(!hierarchyHidden);
        }
    }

    public static class ScriptCatalogLoader
    {
        public static ScriptCatalog scriptCatalog;

        public static bool downloading { get; private set; }
        private static IEnumerator StartDownloadInternal()
        {
            if (downloading)
                yield break;
            downloading = true;

            try
            {
                string newHash = "";
                UnityWebRequest hashReq = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevels, "ScriptCatalogHash.txt"));
                try
                {
                    hashReq.downloadHandler = new DownloadHandlerBuffer();
                    yield return hashReq.SendWebRequest();

                    if (hashReq.isHttpError || hashReq.isNetworkError)
                    {
                        Debug.LogError("Could not download the script catalog hash");
                        yield break;
                    }

                    newHash = hashReq.downloadHandler.text;
                }
                finally
                {
                    hashReq.Dispose();
                }

                string cachedCatalogPath = Path.Combine(workingDir, "OnlineCache", "ScriptCatalog.json");
                if (File.Exists(cachedCatalogPath))
                {
                    string catalog = File.ReadAllText(cachedCatalogPath);
                    string hash = CryptographyUtils.GetMD5String(catalog);
                    if (hash == newHash)
                    {
                        Debug.Log("Cached script catalog up to date");
                        scriptCatalog = JsonConvert.DeserializeObject<ScriptCatalog>(catalog);
                        yield break;
                    }
                }

                UnityWebRequest updatedCatalogRequest = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevels, "ScriptCatalog.json"));
                try
                {
                    updatedCatalogRequest.downloadHandler = new DownloadHandlerBuffer();
                    yield return updatedCatalogRequest.SendWebRequest();

                    if (updatedCatalogRequest.isHttpError || updatedCatalogRequest.isNetworkError)
                    {
                        Debug.LogError("Could not download the script catalog");
                        yield break;
                    }

                    scriptCatalog = JsonConvert.DeserializeObject<ScriptCatalog>(updatedCatalogRequest.downloadHandler.text);
                    File.WriteAllText(cachedCatalogPath, updatedCatalogRequest.downloadHandler.text);
                    string currentHash = CryptographyUtils.GetMD5String(updatedCatalogRequest.downloadHandler.text);

                    if (currentHash != newHash)
                    {
                        Debug.LogWarning($"New script catalog hash value does not match online catalog hash value, github page not cached yet (current hash is {currentHash}. online hash is {newHash})");
                    }
                }
                finally
                {
                    updatedCatalogRequest.Dispose();
                }
            }
            finally
            {
                downloading = false;
            }
        }

        public static void StartDownload()
        {
            if (downloading)
                return;
            OnlineLevelsManager.instance.StartCoroutine(StartDownloadInternal());
        }

        public static bool ScriptExistsInCatalog(string script)
        {
            if (scriptCatalog == null)
                return false;
            return scriptCatalog.Scripts.Where(s => s.FileName == script).Any();
        }

        public static bool TryGetScriptInfo(string script, out ScriptInfo info)
        {
            info = scriptCatalog == null ? null : scriptCatalog.Scripts.Where(s => s.FileName == script).FirstOrDefault();
            return info != null;
        }

    }

    public class OnlineLevelsManager : MonoBehaviour
    {
        public static OnlineLevelsManager instance;
        public static ConfigPanel onlineLevelsPanel;
        public static ConfigDivision onlineLevelContainer;
        public static LoadingCircleField loadingCircle;

        public enum Repo
        {
            AngryLevelLoader,
            AngryLevels
        }

        public static string GetGithubURL(Repo repo, string path)
        {
            string branch = "release";
            if (useDevelopmentBranch.value)
                branch = "dev";

            string repoName = "AngryLevels";
            switch (repo)
            {
                case Repo.AngryLevels:
                    repoName = "AngryLevels";
                    break;

                case Repo.AngryLevelLoader:
                    repoName = "AngryLevelLoader";
                    break;
            }

            return $"https://raw.githubusercontent.com/eternalUnion/{repoName}/{branch}/{path}";
        }

        // Filters
        public enum SortFilter
        {
            Name,
            Author,
            LastUpdate,
            ReleaseDate
        }

        public static BoolField showInstalledLevels;
        public static BoolField showUpdateAvailableLevels;
        public static BoolField showNotInstalledLevels;
        public static StringField authorFilter;
        public static EnumField<SortFilter> sortFilter;

        public static void Init()
        {
            if (instance == null)
            {
                instance = new GameObject().AddComponent<OnlineLevelsManager>();
                DontDestroyOnLoad(instance);
            }

            string cachedCatalogPath = Path.Combine(workingDir, "OnlineCache", "LevelCatalog.json");
            if (File.Exists(cachedCatalogPath))
                catalog = JsonConvert.DeserializeObject<LevelCatalog>(File.ReadAllText(cachedCatalogPath));

            var filterPanel = new ConfigPanel(onlineLevelsPanel, "Filters", "online_filters");
            filterPanel.hidden = true;

            new ConfigHeader(filterPanel, "State Filters");
            showInstalledLevels = new BoolField(filterPanel, "Installed", "online_installedLevels", true);
            showNotInstalledLevels = new BoolField(filterPanel, "Not installed", "online_notInstalledLevels", true);
            showUpdateAvailableLevels = new BoolField(filterPanel, "Update available", "online_updateAvailableLevels", true);
            sortFilter = new EnumField<SortFilter>(filterPanel, "Sort type", "sf_o_sortType", SortFilter.LastUpdate);
            sortFilter.onValueChange += (e) =>
            {
                sortFilter.value = e.value;
                SortAll();
            };
            new ConfigHeader(filterPanel, "Variable Filters");
            authorFilter = new StringField(filterPanel, "Author", "sf_o_authorFilter", "", true);

            var toolbar = new ButtonArrayField(onlineLevelsPanel, "online_toolbar", 2, new float[] { 0.5f, 0.5f }, new string[] { "Refresh", "Filters" });
            toolbar.OnClickEventHandler(0).onClick += RefreshAsync;
            toolbar.OnClickEventHandler(1).onClick += () => filterPanel.OpenPanel();

            loadingCircle = new LoadingCircleField(onlineLevelsPanel);
            loadingCircle.hidden = true;
            onlineLevelContainer = new ConfigDivision(onlineLevelsPanel, "p_onlineLevelsDiv");

            LoadThumbnailHashes();
        }

        public static void SortAll()
        {
            int i = 0;
            if (sortFilter.value == SortFilter.Name)
            {
                foreach (var bundle in onlineLevels.Values.OrderBy(b => b.bundleName))
                    bundle.siblingIndex = i++;
            }
            else if (sortFilter.value == SortFilter.Author)
            {
                foreach (var bundle in onlineLevels.Values.OrderBy(b => b.author))
                    bundle.siblingIndex = i++;
            }
            else if (sortFilter.value == SortFilter.LastUpdate)
            {
                foreach (var bundle in onlineLevels.Values.OrderByDescending(b => b.lastUpdate))
                {
                    bundle.siblingIndex = i++;
                }
            }
            else if (sortFilter.value == SortFilter.ReleaseDate)
            {
                for (int k = 0; k < catalog.Levels.Count; k++)
                {
                    if (onlineLevels.TryGetValue(catalog.Levels[k].Guid, out var level))
                        level.siblingIndex = i++;
                }
            }
        }

        private static bool downloadingCatalog = false;
        public static LevelCatalog catalog;
        public static Dictionary<string, OnlineLevelField> onlineLevels = new Dictionary<string, OnlineLevelField>();
        public static Dictionary<string, string> thumbnailHashes = new Dictionary<string, string>();

        public static void LoadThumbnailHashes()
        {
            thumbnailHashes.Clear();

            string thumbnailHashLocation = Path.Combine(workingDir, "OnlineCache", "thumbnailCacheHashes.txt");
            if (File.Exists(thumbnailHashLocation))
            {
                using (StreamReader hashReader = new StreamReader(File.Open(thumbnailHashLocation, FileMode.Open, FileAccess.Read)))
                {
                    while (!hashReader.EndOfStream)
                    {
                        string guid = hashReader.ReadLine();
                        if (hashReader.EndOfStream)
                        {
                            Debug.LogWarning("Invalid end of thumbnail cache hash file");
                            break;
                        }

                        string hash = hashReader.ReadLine();
                        thumbnailHashes[guid] = hash;
                    }
                }
            }
        }

        public static void SaveThumbnailHashes()
        {
            string thumbnailHashDir = Path.Combine(workingDir, "OnlineCache");
            string thumbnailHashLocation = Path.Combine(thumbnailHashDir, "thumbnailCacheHashes.txt");
            if (!Directory.Exists(thumbnailHashDir))
                Directory.CreateDirectory(thumbnailHashDir);

            using (FileStream fs = File.Open(thumbnailHashLocation, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.Begin);
                fs.SetLength(0);

                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach (var pair in thumbnailHashes)
                    {
                        sw.WriteLine(pair.Key);
                        sw.WriteLine(pair.Value);
                    }
                }
            }
        }

        public static void RefreshAsync()
        {
            ScriptCatalogLoader.StartDownload();

            if (downloadingCatalog)
                return;

            foreach (var field in onlineLevels.Values)
                field.hidden = true;

            onlineLevelContainer.hidden = true;
            loadingCircle.hidden = false;
            instance.StartCoroutine(instance.m_CheckCatalogVersion());
        }

        private IEnumerator m_CheckCatalogVersion()
        {
            LevelCatalog prevCatalog = catalog;
            catalog = null;
            downloadingCatalog = true;

            string newCatalogHash = "";
            string cachedCatalogPath = Path.Combine(workingDir, "OnlineCache", "LevelCatalog.json");

            try
            {
                UnityWebRequest catalogVersionRequest = new UnityWebRequest(GetGithubURL(Repo.AngryLevels, "LevelCatalogHash.txt"));
                catalogVersionRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return catalogVersionRequest.SendWebRequest();

                if (catalogVersionRequest.isNetworkError || catalogVersionRequest.isHttpError)
                {
                    Debug.LogError("Could not download catalog version");
                    downloadingCatalog = false;
                    catalog = null;
                    yield break;
                }
                else
                {
                    newCatalogHash = catalogVersionRequest.downloadHandler.text;
                }
            }
            finally
            {
                downloadingCatalog = false;
            }

            if (File.Exists(cachedCatalogPath))
            {
                string cachedCatalog = File.ReadAllText(cachedCatalogPath);
                string catalogHash = CryptographyUtils.GetMD5String(cachedCatalog);
                catalog = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);

                if (catalogHash == newCatalogHash)
                {
                    // Wait for script catalog
                    while (ScriptCatalogLoader.downloading)
                    {
                        yield return null;
                    }

                    Debug.Log("Current online level catalog is up to date, loading from cache");
                    PostCatalogLoad();
                    yield break;
                }

                catalog = null;
            }

            Debug.Log("Current online level catalog is out of date, downloading from web");
            downloadingCatalog = true;
            instance.StartCoroutine(m_DownloadCatalog(newCatalogHash, prevCatalog));
        }

        private IEnumerator m_DownloadCatalog(string newHash, LevelCatalog prevCatalog)
        {
            downloadingCatalog = true;
            catalog = null;

            try
            {
                string catalogDir = Path.Combine(workingDir, "OnlineCache");
                if (!Directory.Exists(catalogDir))
                    Directory.CreateDirectory(catalogDir);
                string catalogPath = Path.Combine(catalogDir, "LevelCatalog.json");

                UnityWebRequest catalogRequest = new UnityWebRequest(GetGithubURL(Repo.AngryLevels, "LevelCatalog.json"));
                catalogRequest.downloadHandler = new DownloadHandlerFile(catalogPath);
                yield return catalogRequest.SendWebRequest();

                if (catalogRequest.isNetworkError || catalogRequest.isHttpError)
                {
                    Debug.LogError("Could not download catalog");
                    downloadingCatalog = false;
                    yield break;
                }
                else
                {
                    while (ScriptCatalogLoader.downloading)
                    {
                        yield return null;
                    }

                    string cachedCatalog = File.ReadAllText(catalogPath);
                    string catalogHash = CryptographyUtils.GetMD5String(cachedCatalog);
                    catalog = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);

                    if (catalogHash != newHash)
                    {
                        Debug.LogWarning($"Catalog hash does not match, github did not cache the new catalog yet (current hash is {catalogHash}. online hash is {newHash})");
                    }

                    if (newLevelNotifierToggle.value && prevCatalog != null)
                    {
                        List<string> newLevels = catalog.Levels.Where(level => prevCatalog.Levels.Where(l => l.Guid == level.Guid).FirstOrDefault() == null).Select(level => level.Name).ToList();

                        if (newLevels.Count != 0)
                        {
                            if (!string.IsNullOrEmpty(newLevelNotifierLevels.value))
                                newLevels.AddRange(newLevelNotifierLevels.value.Split('`'));
                            newLevels = newLevels.Distinct().ToList();
                            newLevelNotifierLevels.value = string.Join("`", newLevels);

                            newLevelNotifier.text = string.Join("\n", newLevels.Where(level => !string.IsNullOrEmpty(level)).Select(name => $"<color=lime>New level: {name}</color>"));
                            newLevelNotifier.hidden = false;
                            newLevelToggle.value = true;
                        }
                    }
                    else
                    {
                        newLevelNotifier.hidden = true;
                    }
                }
            }
            finally
            {
                downloadingCatalog = false;
                PostCatalogLoad();
            }
        }

        public static void UpdateUI()
        {
            foreach (var levelField in onlineLevels.Values)
                levelField.UpdateUI();

            // Insertion sort not working properly for now
            SortAll();
        }

        private static void PostCatalogLoad()
        {
            loadingCircle.hidden = true;
            onlineLevelContainer.hidden = false;
            if (catalog == null)
                return;

            bool dirtyThumbnailCacheHashFile = false;
            foreach (LevelInfo info in catalog.Levels)
            {
                OnlineLevelField field;
                bool justCreated = false;
                if (!onlineLevels.TryGetValue(info.Guid, out field))
                {
                    justCreated = true;
                    field = new OnlineLevelField(onlineLevelContainer, info.Guid);
                    onlineLevels[info.Guid] = field;
                }

                // Update info text
                field.bundleName = info.Name;
                field.author = info.Author;
                field.bundleFileSize = info.Size;
                field.bundleBuildHash = info.Hash;
                field.lastUpdate = info.LastUpdate;

                // Update ui
                field.UpdateUI();

                // Update thumbnail if not cahced or out of date
                string imageCacheDir = Path.Combine(workingDir, "OnlineCache", "ThumbnailCache");
                if (!Directory.Exists(imageCacheDir))
                    Directory.CreateDirectory(imageCacheDir);
                string imageCachePath = Path.Combine(imageCacheDir, $"{info.Guid}.png");

                bool downloadThumbnail = false;
                bool thumbnailExists = File.Exists(imageCachePath);
                if (!thumbnailExists)
                {
                    downloadThumbnail = true;
                }
                else if (!thumbnailHashes.TryGetValue(info.Guid, out string currentHash) || currentHash != info.ThumbnailHash)
                {
                    downloadThumbnail = true;
                }

                if (downloadThumbnail)
                {
                    // Normally download handler can overwrite a file, but
                    // if the download is interrupted before the file can
                    // be downloaded, old file can persist until next
                    // thumbnail update
                    if (thumbnailExists)
                        File.Delete(imageCachePath);
                    thumbnailHashes[info.Guid] = info.ThumbnailHash;
                    dirtyThumbnailCacheHashFile = true;

                    UnityWebRequest req = new UnityWebRequest(GetGithubURL(Repo.AngryLevels, $"Levels/{info.Guid}/thumbnail.png"));
                    req.downloadHandler = new DownloadHandlerFile(imageCachePath);
                    var handle = req.SendWebRequest();

                    handle.completed += (e) =>
                    {
                        try
                        {
                            if (req.isHttpError || req.isNetworkError)
                                return;
                            field.DownloadPreviewImage("file://" + imageCachePath, true);
                        }
                        finally
                        {
                            req.Dispose();
                        }
                    };
                }
                else
                {
                    field.DownloadPreviewImage("file://" + imageCachePath, false);
                }

                // Sort if just created
                if (justCreated)
                    field.UpdateOrder();

                // Show the field if matches the filter
                field.hidden = false;
                if (field.status == OnlineLevelField.OnlineLevelStatus.notInstalled)
                {
                    if (!showNotInstalledLevels.value)
                        field.hidden = true;
                }
                else if (field.status == OnlineLevelField.OnlineLevelStatus.installed)
                {
                    if (!showInstalledLevels.value)
                        field.hidden = true;
                }
                else if (field.status == OnlineLevelField.OnlineLevelStatus.updateAvailable)
                {
                    if (!showUpdateAvailableLevels.value)
                        field.hidden = true;
                }

                if (field.hidden)
                    continue;

                if (!string.IsNullOrEmpty(authorFilter.value) && field.author.ToLower() != authorFilter.value.ToLower())
                    field.hidden = true;
            }

            if (dirtyThumbnailCacheHashFile)
                SaveThumbnailHashes();

            CheckLevelUpdateText();

            SortAll();
        }

        public static void CheckLevelUpdateText()
        {
            if (!levelUpdateNotifierToggle.value)
            {
                levelUpdateNotifier.hidden = true;
                return;
            }

            levelUpdateNotifier.text = "";
            levelUpdateNotifier.hidden = true;
            foreach (OnlineLevelField field in onlineLevels.Values)
            {
                if (field.status == OnlineLevelField.OnlineLevelStatus.updateAvailable)
                {
                    if (levelUpdateIgnoreCustomBuilds.value)
                    {
                        AngryBundleContainer container = GetAngryBundleByGuid(field.bundleGuid);
                        if (container != null)
                        {
                            LevelInfo info = catalog.Levels.Where(level => level.Guid == field.bundleGuid).First();
                            if (info.Updates != null && !info.Updates.Select(u => u.Hash).Contains(container.bundleData.buildHash))
                                continue;
                        }
                    }

                    if (levelUpdateNotifier.text != "")
                        levelUpdateNotifier.text += '\n';
                    levelUpdateNotifier.text += $"<color=cyan>Update available for {field.bundleName}</color>";
                    levelUpdateNotifier.hidden = false;
                }
            }
        }
    }
}
