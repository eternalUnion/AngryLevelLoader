using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Fields;
using AngryLevelLoader.Managers.ServerManager;
using Newtonsoft.Json;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static AngryLevelLoader.Managers.OnlineLevelsUI;

namespace AngryLevelLoader.Managers
{
    /// <summary>
    /// Handles downloading and caching the online levels catalog.
    /// </summary>
    public static class OnlineCatalogManager
    {
        /// <summary>
        /// Current online levels catalog. Can be null if the catalog was not downloaded.
        /// </summary>
		public static LevelCatalog Catalog { get; private set; } = null;

        private static CachedTask<LevelCatalog> currentDownloadTask = new CachedTask<LevelCatalog>(DownloadCatalogWithHashCheck);
        /// <summary>
        /// True if there is a download task for the catalog.
        /// </summary>
        public static bool Downloading => currentDownloadTask.Running;

        /// <summary>
        /// Start downloading the catalog from GitHub. Before downloading the full catalog, only the
        /// MD5 hash of the catalog is downloaded and checked against the locally cached catalog. If
        /// the hashes match, cached catalog is returned without downloading a new copy. Otherwise,
        /// the latest catalog is downloaded and saved to the disk.
        /// 
        /// If there is already a download in progress, current download task is returned. Cancelling
        /// the task does not cancel tasks made by other callers.
        /// </summary>
        /// <returns>Up to date online level catalog. Can be null if there was a download error.</returns>
        public static Task<LevelCatalog> DownloadCatalogAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return currentDownloadTask.GetTask(cancellationToken);
        }

		private static async Task<LevelCatalog> DownloadCatalogWithHashCheck()
		{
			string newCatalogHash = "";
			string cachedCatalogPath = AngryPaths.LevelCatalogCachePath;
			IOUtils.TryCreateDirectoryForFile(cachedCatalogPath);

			UnityWebRequest catalogVersionRequest = new UnityWebRequest(GetGithubURL(Repo.AngryLevels, "V2/LevelCatalogHash.txt"));
			catalogVersionRequest.downloadHandler = new DownloadHandlerBuffer();
			await catalogVersionRequest.SendWebRequest();

			if (catalogVersionRequest.result != UnityWebRequest.Result.Success)
			{
				Plugin.logger.LogError("Could not download catalog version");
				Catalog = null;
				return Catalog;
			}
			else
			{
				newCatalogHash = catalogVersionRequest.downloadHandler.text;
			}

			if (File.Exists(cachedCatalogPath))
			{
				string cachedCatalog = File.ReadAllText(cachedCatalogPath);
				string catalogHash = CryptographyUtils.GetMD5String(cachedCatalog);

				try
				{
					LevelCatalog cachedCatalogObj = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);

					if (catalogHash == newCatalogHash)
					{
						Plugin.logger.LogInfo("Current online level catalog is up to date, loading from cache");
                        Catalog = cachedCatalogObj;
						return Catalog;
					}
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load cached level catalog, but it is corrupted");
				}
			}

			Plugin.logger.LogInfo("Current online level catalog is out of date, downloading from web");
            Catalog = await DownloadCatalog(newCatalogHash);
			return Catalog;
		}

		private static async Task<LevelCatalog> DownloadCatalog(string newHash)
		{
			string catalogPath = AngryPaths.LevelCatalogCachePath;
			IOUtils.TryCreateDirectoryForFile(catalogPath);

			UnityWebRequest catalogRequest = new UnityWebRequest(GetGithubURL(Repo.AngryLevels, "V2/LevelCatalog.json"));
			catalogRequest.downloadHandler = new DownloadHandlerFile(catalogPath);
			await catalogRequest.SendWebRequest();

			if (catalogRequest.result != UnityWebRequest.Result.Success)
			{
				Plugin.logger.LogError("Could not download catalog");
				return null;
			}
			else
			{
				string cachedCatalog = File.ReadAllText(catalogPath);
				string catalogHash = CryptographyUtils.GetMD5String(cachedCatalog);

                LevelCatalog catalog = null;

				try
				{
					catalog = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load level catalog, but it is corrupted");
					return null;
				}

				if (catalogHash != newHash)
				{
					Plugin.logger.LogWarning($"Catalog hash does not match, github did not cache the new catalog yet (current hash is {catalogHash}. online hash is {newHash})");
				}

                return catalog;
			}
		}
	
        internal static void LoadCachedCatalog()
        {
			string cachedCatalogPath = AngryPaths.LevelCatalogCachePath;
			if (File.Exists(cachedCatalogPath))
			{
				try
				{
					Catalog = JsonConvert.DeserializeObject<LevelCatalog>(File.ReadAllText(cachedCatalogPath));
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load cached level catalog, but it is corrupted");
				}
			}
		}
    }

    /// <summary>
    /// Handler for the online levels panel.
    /// </summary>
	public static class OnlineLevelsUI
    {
        internal static ConfigPanel onlineLevelsPanel;
		internal static ConfigDivision onlineLevelContainer;
		internal static LoadingCircleField loadingCircle;

		internal enum Repo
        {
            AngryLevelLoader,
            AngryLevels
        }

        internal static string GetGithubURL(Repo repo, string path)
        {
            string branch = "release";
            if (ConfigManager.useDevelopmentBranch.value)
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
            Votes,
            LastUpdate,
            ReleaseDate
        }

        internal static BoolField showInstalledLevels;
		internal static BoolField showUpdateAvailableLevels;
		internal static BoolField showNotInstalledLevels;
		internal static EnumField<SortFilter> sortFilter;

        internal static SearchBarField searchBar;
        private static string[] searchKeywords = new string[0];
        internal static ConfigHeader searchInfo;

		internal static Dictionary<string, OnlineLevelField> onlineLevels = new Dictionary<string, OnlineLevelField>();

        /// <summary>
        /// Get the online level field for the given bundle guid. The fields may not have
        /// been created yet if RefreshAsync() was not called.
        /// </summary>
        /// <param name="bundleGuid">Id of the online level. Can be obtained from the online catalog.</param>
        /// <param name="onlineLevel">Relevant handler for the given guid. null if no such bundle exists or RefreshAsync() was not called before.</param>
        public static bool TryGetOnlineLevel(string bundleGuid, out OnlineLevelField onlineLevel)
        {
            return onlineLevels.TryGetValue(bundleGuid, out onlineLevel);
        }

		private static bool _inited = false;
        internal static void Init()
        {
            if (_inited)
                return;
            _inited = true;

            string cachedCatalogPath = AngryPaths.LevelCatalogCachePath;
            OnlineCatalogManager.LoadCachedCatalog();
            OnlineScriptsManager.LoadCachedCatalog();

            var filterPanel = new ConfigPanel(onlineLevelsPanel, "Filters", "online_filters");
            filterPanel.hidden = true;

            new ConfigHeader(filterPanel, "State Filters");
            showInstalledLevels = new BoolField(filterPanel, "Installed", "online_installedLevels", true);
            showNotInstalledLevels = new BoolField(filterPanel, "Not installed", "online_notInstalledLevels", true);
            showUpdateAvailableLevels = new BoolField(filterPanel, "Update available", "online_updateAvailableLevels", true);
            sortFilter = new EnumField<SortFilter>(filterPanel, "Sort type", "sf_o_sortType", SortFilter.LastUpdate);
            sortFilter.SetEnumDisplayName(SortFilter.LastUpdate, "Last Update");
            sortFilter.SetEnumDisplayName(SortFilter.ReleaseDate, "Release Date");
            sortFilter.onValueChange += (e) =>
            {
                sortFilter.value = e.value;
                SortAll();
            };
            var toolbar = new ButtonArrayField(onlineLevelsPanel, "online_toolbar", 2, new float[] { 0.5f, 0.5f }, new string[] { "Refresh", "Filters" });
            toolbar.OnClickEventHandler(0).onClick += () => RefreshAsync();
            toolbar.OnClickEventHandler(1).onClick += () => filterPanel.OpenPanel();

            searchBar = new SearchBarField(onlineLevelsPanel);

            loadingCircle = new LoadingCircleField(onlineLevelsPanel);
            loadingCircle.hidden = true;
            onlineLevelContainer = new ConfigDivision(onlineLevelsPanel, "p_onlineLevelsDiv");

            searchInfo = new ConfigHeader(onlineLevelContainer, "", 18);
            searchInfo.textColor = Color.gray;
            searchInfo.hidden = true;

            searchBar.onValueChange = (newVal) =>
            {
                string[] newKeywords = newVal.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.ToLower()).ToArray();
                if (newKeywords.Length == searchKeywords.Length && newKeywords.SequenceEqual(searchKeywords))
                    return;

                searchKeywords = newKeywords;
                foreach (OnlineLevelField onlineLevel in onlineLevels.Values)
                    onlineLevel.SearchKeywords = searchKeywords;

				if (searchKeywords.Length == 0)
                {
					searchInfo.hidden = true;
                }
				else
				{
					searchInfo.hidden = false;
					searchInfo.text = $"Showing {onlineLevels.Values.Where(e => !e.hidden).Count()} of {OnlineCatalogManager.Catalog.Levels.Count} bundles";
				}
			};
			searchBar.onEndEdit += (bool wasCanceled) =>
			{
				if (!wasCanceled)
					return;

				if (!string.IsNullOrWhiteSpace(searchBar.value))
				{
					searchBar.value = "";
					return;
				}
				ConfigManager.config.rootPanel.OpenPanel();
			};

			searchBar.onReset = () => searchBar.value = "";
        }

        private static void SortAll()
        {
            int i = 0;
            if (sortFilter.value == SortFilter.Name)
            {
                foreach (var bundle in onlineLevels.Values.OrderBy(b => b.OnlineBundle.Name))
                    bundle.siblingIndex = i++;
            }
            else if (sortFilter.value == SortFilter.Author)
            {
                foreach (var bundle in onlineLevels.Values.OrderBy(b => b.OnlineBundle.Author))
                    bundle.siblingIndex = i++;
            }
            else if (sortFilter.value == SortFilter.Votes)
            {
				foreach (var bundle in onlineLevels.Values.OrderByDescending(b => b.VoteCount))
					bundle.siblingIndex = i++;
			}
            else if (sortFilter.value == SortFilter.LastUpdate)
            {
                foreach (var bundle in onlineLevels.Values.OrderByDescending(b => b.OnlineBundle.LastUpdate))
                    bundle.siblingIndex = i++;
            }
            else if (sortFilter.value == SortFilter.ReleaseDate)
            {
                LevelCatalog catalog = OnlineCatalogManager.Catalog;
                if (catalog != null)
                {
                    for (int k = 0; k < catalog.Levels.Count; k++)
                    {
                        if (onlineLevels.TryGetValue(catalog.Levels[k].Guid, out var level))
                            level.siblingIndex = i++;
                    }
                }
            }
        }

        private static CachedTask currentRefreshTask = new CachedTask(RefreshTask);
        /// <summary>
        /// Set to true if the online levels are being refreshed by fetching the latest catalog.
        /// </summary>
        public static bool Refreshing => currentRefreshTask.Running;
        
        /// <summary>
        /// Download the latest catalog and update the UI.
        /// </summary>
        public static Task RefreshAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return currentRefreshTask.GetTask();
		}

        private static CachedTask<AngryVotes.GetAllVotesResult> currentGetVotesTask = new CachedTask<AngryVotes.GetAllVotesResult>(async () => await AngryVotes.GetAllVotesTask());
        private static CancellationTokenSource currentVoteCancellationToken = null;

		private static CachedTask<AngryUser.UserInfoResult> currentUserInfoRequestTask = new CachedTask<AngryUser.UserInfoResult>(async () => await AngryUser.GetUserInfo());
		private static CancellationTokenSource currentUserInfoRequestToken = null;

		private static async Task RefreshTask()
        {
            // Hide all ui
			foreach (var field in onlineLevels.Values)
			{
				field.hidden = true;
				field.VoteStatus = OnlineLevelField.VoteStat.Disabled;
			}

			onlineLevelContainer.hidden = true;
			loadingCircle.hidden = false;

            // Hold the old catalog
            LevelCatalog previousCatalog = OnlineCatalogManager.Catalog;
			// Download newest catalog
			LevelCatalog levelCatalog = await OnlineCatalogManager.DownloadCatalogAsync();
			// Ensure script catalog is also up to date
			await OnlineScriptsManager.DownloadCatalogAsync();
			// Refresh UI
			PostCatalogLoad(levelCatalog, previousCatalog);

            // Start fetching all votes in the background
            if (currentVoteCancellationToken != null)
            {
                currentVoteCancellationToken.Cancel();
                currentVoteCancellationToken.Dispose();
			}

            currentVoteCancellationToken = new CancellationTokenSource();
			Task<AngryVotes.GetAllVotesResult> votesTask = currentGetVotesTask.GetTask(currentVoteCancellationToken.Token);

			// Start fetching user info in the background
			if (currentUserInfoRequestToken != null)
			{
				currentUserInfoRequestToken.Cancel();
				currentUserInfoRequestToken.Dispose();
			}

			currentUserInfoRequestToken = new CancellationTokenSource();
			Task<AngryUser.UserInfoResult> userInfoTask = currentUserInfoRequestTask.GetTask(currentUserInfoRequestToken.Token);

            // Display vote for each level
			_ = votesTask.ContinueWith((task) =>
            {
                if (task.Result == null)
                    return;

                AngryVotes.GetAllVotesResult result = task.Result;
                if (!result.completedSuccessfully || result.status != AngryVotes.GetAllVotesStatus.GET_ALL_VOTES_OK)
                {
                    Plugin.logger.LogError($"Failed to get all votes: {(result.networkError ? "network error, check connection" : result.httpError ? "http error, could be caused by internal server error" : $"{result.response.status} : {result.response.message}")}");
                    return;
                }

                ProcessVotes(result);

                // Display player made votes
				_ = userInfoTask.ContinueWith((task) =>
				{
					if (task.Result == null)
						return;

                    AngryUser.UserInfoResult result = task.Result;
					if (!result.completedSuccessfully || result.status != AngryUser.UserInfoStatus.OK)
                    {
						Plugin.logger.LogError($"Failed to get user info : {(result.networkError ? "network error, check connection" : result.httpError ? "http error, could be caused by internal server error" : $"{result.response.status} : {result.response.message}")}");
						return;
                    }

					ProcessUserVotes(result);
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

        private static void ProcessVotes(AngryVotes.GetAllVotesResult votes)
        {
			foreach (var bundleVoteInfo in votes.response.bundles)
			{
				if (onlineLevels.TryGetValue(bundleVoteInfo.Key, out OnlineLevelField field))
					field.VoteCount = bundleVoteInfo.Value.upvotes - bundleVoteInfo.Value.downvotes;
			}

			if (sortFilter.value == SortFilter.Votes)
				SortAll();
		}

        private static void ProcessUserVotes(AngryUser.UserInfoResult userInfoReq)
        {
			AngryUser.UserInfoData data = userInfoReq.response.info;
			foreach (var field in onlineLevels)
			{
				if (data.upvotedBundles.Contains(field.Key))
					field.Value.VoteStatus = OnlineLevelField.VoteStat.Upvoted;
				else if (data.downvotedBundles.Contains(field.Key))
					field.Value.VoteStatus = OnlineLevelField.VoteStat.Downvoted;
				else
					field.Value.VoteStatus = OnlineLevelField.VoteStat.Cleared;
			}
		}

        internal static void UpdateUI()
        {
            foreach (var field in onlineLevels.Values)
            {
                field.UpdateStatus();
				if (field.Status == OnlineLevelField.OnlineLevelStatus.NotInstalled)
				{
					if (!showNotInstalledLevels.value)
						field.hidden = true;
				}
				else if (field.Status == OnlineLevelField.OnlineLevelStatus.Installed)
				{
					if (!showInstalledLevels.value)
						field.hidden = true;
				}
				else if (field.Status == OnlineLevelField.OnlineLevelStatus.UpdateAvailable)
				{
					if (!showUpdateAvailableLevels.value)
						field.hidden = true;
				}
			}

            // Insertion sort not working properly for now
            SortAll();
        }

        private static void PostCatalogLoad(LevelCatalog catalog, LevelCatalog previousCatalog)
        {
            loadingCircle.hidden = true;
            onlineLevelContainer.hidden = false;
            if (catalog == null)
            {
                onlineLevelContainer.hidden = true;
                return;
            }

            foreach (BundleInfo info in catalog.Levels)
            {
                OnlineLevelField field;
                bool justCreated = false;
                if (!onlineLevels.TryGetValue(info.Guid, out field))
                {
                    justCreated = true;
                    field = new OnlineLevelField(onlineLevelContainer, info);
                    field.SearchKeywords = searchKeywords;

                    onlineLevels[info.Guid] = field;
                }

                // Update ui
                if (!justCreated)
                {
                    field.OnlineBundle = info;
                    field.UpdateStatus();
                }

                // Update thumbnail if not cached or out of date
                AngryOnlineThumbnailCache.GetThumbnail(info.Guid).ContinueWith((task) =>
                {
                    if (task.Result != null)
                        field.PreviewImage = task.Result;
                }, TaskScheduler.FromCurrentSynchronizationContext());

                // Sort if just created
                if (justCreated)
                    field.UpdateOrder();

                // Show the field if matches the filter
                field.hidden = !field.SearchMatch;
                if (!field.hidden)
                {
                    if (field.Status == OnlineLevelField.OnlineLevelStatus.NotInstalled)
                    {
                        if (!showNotInstalledLevels.value)
                            field.hidden = true;
                    }
                    else if (field.Status == OnlineLevelField.OnlineLevelStatus.Installed)
                    {
                        if (!showInstalledLevels.value)
                            field.hidden = true;
                    }
                    else if (field.Status == OnlineLevelField.OnlineLevelStatus.UpdateAvailable)
                    {
                        if (!showUpdateAvailableLevels.value)
                            field.hidden = true;
                    }
                }
			}

            CheckNewLevelText(previousCatalog);
            CheckLevelUpdateText();

            SortAll();

            if (searchKeywords.Length == 0)
            {
                searchInfo.hidden = true;
            }
            else
            {
                searchInfo.hidden = false;
                searchInfo.text = $"Showing {onlineLevels.Values.Where(e => !e.hidden).Count()} of {catalog.Levels.Count} bundles";
            }
        }

        internal static void CheckNewLevelText(LevelCatalog previousCatalog)
        {
            if (ConfigManager.newLevelNotifierToggle.value && previousCatalog != null)
            {
                List<string> newLevels = OnlineCatalogManager.Catalog.Levels.Where(level => previousCatalog.Levels.Where(l => l.Guid == level.Guid).FirstOrDefault() == null).Select(level => level.Name).ToList();

                if (newLevels.Count != 0)
                {
                    if (!string.IsNullOrEmpty(ConfigManager.newLevelNotifierLevels.value))
                        newLevels.AddRange(ConfigManager.newLevelNotifierLevels.value.Split('`'));
                    newLevels = newLevels.Distinct().ToList();
                    ConfigManager.newLevelNotifierLevels.value = string.Join("`", newLevels);
                    ConfigManager.newLevelNotifier.text = string.Join("\n", newLevels.Where(level => !string.IsNullOrEmpty(level)).Select(name => $"<color=#00FF00>New level: {name}</color>"));
                    ConfigManager.newLevelNotifier.hidden = false;
                    ConfigManager.newLevelToggle.value = true;
                }
            }
            else
            {
                ConfigManager.newLevelNotifier.hidden = true;
            }
        }

		internal static void CheckLevelUpdateText()
        {
            if (!ConfigManager.levelUpdateNotifierToggle.value)
            {
				ConfigManager.levelUpdateNotifier.hidden = true;
                return;
            }
			ConfigManager.levelUpdateNotifier.text = "";
			ConfigManager.levelUpdateNotifier.hidden = true;
            foreach (OnlineLevelField field in onlineLevels.Values)
            {
                if (field.Status != OnlineLevelField.OnlineLevelStatus.UpdateAvailable)
                    continue;
                
                if (ConfigManager.levelUpdateIgnoreCustomBuilds.value)
                {
					if (field.Bundle != null && field.OnlineBundle.Updates != null && !field.OnlineBundle.Updates.Any(u => u.Hash == field.Bundle.BuildHash))
                        continue;
                }

                if (ConfigManager.levelUpdateNotifier.text != "")
					ConfigManager.levelUpdateNotifier.text += '\n';
				ConfigManager.levelUpdateNotifier.text += $"<color=#00FFFF>Update available for {field.OnlineBundle.Name}</color>";
				ConfigManager.levelUpdateNotifier.hidden = false;
            }
        }
    }
}
