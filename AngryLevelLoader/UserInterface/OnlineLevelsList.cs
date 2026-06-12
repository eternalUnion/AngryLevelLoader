using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Fields;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using AngryLevelLoader.Utils;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.UserInterface
{
	/// <summary>
	/// Handler for the online levels panel.
	/// </summary>
	public static class OnlineLevelsList
    {
        internal static ConfigPanel onlineLevelsPanel;
		internal static ConfigDivision onlineLevelContainer;
		internal static LoadingCircleField loadingCircle;

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
            OnlineCatalogManagerV1.LoadCachedCatalog();
            OnlineScriptsManager.LoadCachedCatalog();

			searchBar = new SearchBarField(onlineLevelsPanel);
			sortFilter = new EnumField<SortFilter>(onlineLevelsPanel, "Sort type", "sf_o_sortType", SortFilter.LastUpdate);
            sortFilter.hidden = true;
            sortFilter.SetEnumDisplayName(SortFilter.LastUpdate, "Last Update");
            sortFilter.SetEnumDisplayName(SortFilter.ReleaseDate, "Release Date");
            sortFilter.postValueChangeEvent += (val) =>
            {
                SortAll();
            };
            new OnlineSortField(onlineLevelsPanel);
			
            showInstalledLevels = new BoolField(onlineLevelsPanel, "Installed", "online_installedLevels", true);
            showInstalledLevels.postValueChangeEvent += (val) => UpdateVisibility();
			showInstalledLevels.hidden = true;
			
            showNotInstalledLevels = new BoolField(onlineLevelsPanel, "Not installed", "online_notInstalledLevels", true);
			showNotInstalledLevels.postValueChangeEvent += (val) => UpdateVisibility();
            showNotInstalledLevels.hidden = true;
			
            showUpdateAvailableLevels = new BoolField(onlineLevelsPanel, "Update available", "online_updateAvailableLevels", true);
			showUpdateAvailableLevels.postValueChangeEvent += (val) => UpdateVisibility();
            showUpdateAvailableLevels.hidden = true;
            
            new OnlineStatusFilterField(onlineLevelsPanel);

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

                UpdateVisibility();

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
			searchBar.onEndEdit += (wasCanceled) =>
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

		private static Regex richText = new Regex(@"<[^>]*>");

		private static void SortAll()
        {
            int i = 0;
            if (sortFilter.value == SortFilter.Name)
            {
                foreach (var bundle in onlineLevels.Values.OrderBy(b => richText.Replace(b.OnlineBundle.Name, string.Empty)))
                    bundle.siblingIndex = i++;
            }
            else if (sortFilter.value == SortFilter.Author)
            {
                foreach (var bundle in onlineLevels.Values.OrderBy(b => richText.Replace(b.OnlineBundle.Author, string.Empty)))
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

        private static void UpdateVisibility()
        {
			foreach (var bundle in onlineLevels.Values)
            {
                bool visible = false;
                switch (bundle.Status)
                {
                    case OnlineLevelField.OnlineLevelStatus.Installed:
                        visible = bundle.SearchMatch && showInstalledLevels.value;
                        break;

                    case OnlineLevelField.OnlineLevelStatus.NotInstalled:
                        visible = bundle.SearchMatch && showNotInstalledLevels.value;
                        break;

                    case OnlineLevelField.OnlineLevelStatus.UpdateAvailable:
                        visible = bundle.SearchMatch && showUpdateAvailableLevels.value;
                        break;
                }

                bundle.hidden = !visible;
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
        public static Task RefreshAsync(CancellationToken cancellationToken = default)
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
					field.VoteCount = bundleVoteInfo.Value.upvotes;
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
			}

            CheckNewLevelText(previousCatalog);
            CheckLevelUpdateText();

            SortAll();
            UpdateVisibility();

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

        private static bool _alreadyNotified = false;

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
				    
                    // Show notification

                    if (!_alreadyNotified)
                    {
                        _alreadyNotified = true;
						string header = newLevels.Count > 1 ? "New levels available!" : "New level available!";
                        string body = newLevels.Count > 1 ? $"{newLevels.Count} new online levels available!" : "A new online level is available!";
                        NotificationManager.SendNotificationInMainMenu(header, body);
                    }
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

                if (field.OnlineBundle.Locked)
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
