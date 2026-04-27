using AngryLevelLoader.Containers;
using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using AngryLevelLoader.Notifications;
using AngryUiComponents;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace AngryLevelLoader.Fields
{
	class DisableWhenHidden : MonoBehaviour
	{
		void OnDisable()
		{
			gameObject.SetActive(false);
		}
	}

    /// <summary>
    /// UI entry for an online bundle in the online levels panel.
    /// Also controls downloading, updating and voting the bundle.
    /// </summary>
	public class OnlineLevelField : CustomConfigField
    {
        // Assets

        private const string ASSET_PATH = "AngryLevelLoader/Fields/OnlineLevelField.prefab";

        private static Sprite arrow;
        private static Sprite arrowFilled;

        static OnlineLevelField()
        {
            arrow = AssetManager.arrow;
            arrowFilled = AssetManager.arrowFilled;
        }

        // Data

        private BundleInfo _onlineBundle;
        /// <summary>
        /// Entry from the online level catalog associated with this field.
        /// </summary>
        public BundleInfo OnlineBundle
        {
            get => _onlineBundle;
            internal set
            {
                if (value == null)
                    throw new ArgumentException("Parameter is null");

                if (_onlineBundle != null && _onlineBundle.Guid != value.Guid)
                    throw new ArgumentException("Guid mismatch when updating online bundle info");

                _onlineBundle = value;
                UpdateStatus();

				if (currentUi != null)
                {
				    bool locked = OnlineBundle.Locked;
				    currentUi.install.interactable = !locked;
				    currentUi.update.interactable = !locked;
				    currentUi.votes.gameObject.SetActive(!locked);
                }
			}
        }

		private string GetFileSizeString()
		{
			const int kilobyteSize = 1024;
			const int megabyteSize = 1024 * 1024;

			int bundleFileSize = OnlineBundle.Size;

			if (bundleFileSize >= megabyteSize)
				return $"{((float)bundleFileSize / megabyteSize).ToString("0.00")} MB";
			if (bundleFileSize >= kilobyteSize)
				return $"{((float)bundleFileSize / kilobyteSize).ToString("0.00")} KB";
			return $"{bundleFileSize} B";
		}

		private BundleContainer _bundle = null;
        /// <summary>
        /// Locally installed bundle that has the same guid as OnlineBundle.
        /// Can be null if the bundle is not locally installed.
        /// </summary>
        public BundleContainer Bundle
        {
            get
            {
                if (_bundle == null)
                    Plugin.TryGetAngryBundleByGuid(OnlineBundle.Guid, out _bundle);

                return _bundle;
            }
        }

		// UI properties

        private Texture2D _previewImage;
        /// <summary>
        /// Online 4:3 thumbnail of the bundle.
        /// </summary>
        public Texture2D PreviewImage
        {
            get => _previewImage;
            internal set
            {
                _previewImage = value;
                if (currentUi != null)
                    currentUi.thumbnail.texture = OnlineBundle.Locked ? AssetManager.lockedPreview.texture : _previewImage;
            }
        }

		public enum VoteStat
		{
			Upvoted,
			Downvoted,
			Cleared,
			Disabled
		}

		private VoteStat _voteStatus = VoteStat.Disabled;
        /// <summary>
        /// Current player's vote for this bundle.
        /// </summary>
		public VoteStat VoteStatus
		{
			get => _voteStatus;
			internal set
			{
				_voteStatus = value;
				if (currentUi == null)
					return;

				if (value == VoteStat.Disabled)
				{
					currentUi.upvoteButton.interactable = false;
					currentUi.downvoteButton.interactable = false;
					currentUi.votes.color = Color.gray;

					currentUi.upvoteImage.sprite = arrowFilled;
					currentUi.downvoteImage.sprite = arrowFilled;
				}
				else
				{
					currentUi.upvoteButton.interactable = true;
					currentUi.downvoteButton.interactable = true;
					currentUi.votes.color = Color.white;

					currentUi.upvoteImage.sprite = (value == VoteStat.Upvoted) ? arrowFilled : arrow;
					currentUi.downvoteImage.sprite = (value == VoteStat.Downvoted) ? arrowFilled : arrow;
				}
			}
		}

		private int _voteCount = 0;
        /// <summary>
        /// Net number of votes for this bundle.
        /// </summary>
		public int VoteCount
		{
			get => _voteCount;
			internal set
			{
				_voteCount = value;
				if (currentUi == null)
					return;

				currentUi.votes.text = value.ToString();
			}
		}

        public enum OnlineLevelStatus
        {
            Installed,
            NotInstalled,
            UpdateAvailable
        }

        private OnlineLevelStatus _status = OnlineLevelStatus.NotInstalled;
        /// <summary>
        /// Version state of the bundle.
        /// </summary>
        public OnlineLevelStatus Status
        {
            get => _status;
            private set
            {
                _status = value;
				SearchKeywords = SearchKeywords;
			}
        }

		internal void UpdateStatus()
		{
			if (Bundle == null || string.IsNullOrEmpty(Bundle.pathToAngryBundle) || !File.Exists(Bundle.pathToAngryBundle))
				Status = OnlineLevelStatus.NotInstalled;
			else if (Bundle.BuildHash != OnlineBundle.Hash)
				Status = OnlineLevelStatus.UpdateAvailable;
			else
				Status = OnlineLevelStatus.Installed;
		}

		public enum ErrorStat
		{
			NoError,
			NetworkError,
			ValidationError
		}

		private ErrorStat _errorStatus = ErrorStat.NoError;
        /// <summary>
        /// Error from the last download attempt.
        /// </summary>
        public ErrorStat ErrorStatus
        {
            get => _errorStatus;
            private set
            {
                _errorStatus = value;

				// Recalculate info text
				OnlineBundle = OnlineBundle;
			}
        }

        private string GetStatusString()
        {
            if (_errorStatus != ErrorStat.NoError)
            {
                if (_errorStatus == ErrorStat.NetworkError)
                    return $"<color=red><b>Network error</b></color>";
                else if (_errorStatus == ErrorStat.ValidationError)
                    return $"<color=red><b>Validation error</b></color>";
            }

            if (_status == OnlineLevelStatus.NotInstalled)
                return $"<color=red>Not installed</color>";
            else if (_status == OnlineLevelStatus.UpdateAvailable)
                return $"<color=#00FFFF>Update available</color>";
            else
                return $"<color=#00FF00>Installed</color>";
        }

        internal bool SearchMatch { get; private set; } = true;

        private static Regex richText = new Regex(@"<[^>]*>");
        private string[] _searchKeywords = new string[0];
		internal string[] SearchKeywords
        {
            get => _searchKeywords;
            set
            {
                _searchKeywords = value;
                SearchMatch = true;

				string bundleName = richText.Replace(OnlineBundle.Name, string.Empty);
				string authorName = richText.Replace(OnlineBundle.Author, string.Empty);

				WordHighlighter formattedName = new WordHighlighter(bundleName);
				WordHighlighter formattedAuthor = new WordHighlighter(authorName);

				bundleName = bundleName.ToLower();
				authorName = authorName.ToLower();

				foreach (string keyword in value)
				{
					bool matches = false;

					int currentIndex = bundleName.IndexOf(keyword);
					while (currentIndex != -1)
					{
						matches = true;

						formattedName.Highlight(currentIndex, currentIndex + keyword.Length - 1);
						currentIndex = bundleName.IndexOf(keyword, currentIndex + keyword.Length);
					}

					currentIndex = authorName.IndexOf(keyword);
					while (currentIndex != -1)
					{
						matches = true;

						formattedAuthor.Highlight(currentIndex, currentIndex + keyword.Length - 1);
						currentIndex = authorName.IndexOf(keyword, currentIndex + keyword.Length);
					}

					if (!matches)
                    {
                        SearchMatch = false;
                        break;
					}
				}

                hidden = !SearchMatch;

                if (currentUi != null)
                {
                    if (OnlineBundle.Locked)
				        currentUi.infoText.text = $"{formattedName.GenerateFormattedText("<color=yellow><b>", "</b></color>")} <color=red>(OUTDATED/LOCKED)</color>\n<color=#909090>Author: {formattedAuthor.GenerateFormattedText("<color=yellow><b>", "</b></color>")}\nSize: {GetFileSizeString()}</color>\n{GetStatusString()}";
                    else
						currentUi.infoText.text = $"{formattedName.GenerateFormattedText("<color=yellow><b>", "</b></color>")}\n<color=#909090>Author: {formattedAuthor.GenerateFormattedText("<color=yellow><b>", "</b></color>")}\nSize: {GetFileSizeString()}</color>\n{GetStatusString()}";
				}
			}
        }

        private bool inited = false;
        internal OnlineLevelField(ConfigPanel parentPanel, BundleInfo onlineBundle) : base(parentPanel, 600, 170)
        {
            inited = true;
            OnlineBundle = onlineBundle;
            UpdateStatus();

            if (currentContainer != null)
                OnCreateUI(currentContainer);
        }

		// UI

		private AngryOnlineLevelFieldComponent currentUi;
		private RectTransform currentContainer = null;

		private bool InstallActive => !OnlineBundle.Locked && !Downloading && Status == OnlineLevelStatus.NotInstalled;
        private bool UpdateActive => !OnlineBundle.Locked && !Downloading && Status == OnlineLevelStatus.UpdateAvailable;
		private UnityEvent onCancel = new UnityEvent();

        /// <inheritdoc/>
		public override void OnCreateUI(RectTransform fieldUI)
        {
            currentContainer = fieldUI;
            if (!inited)
                return;

            currentUi = Addressables.InstantiateAsync(ASSET_PATH, fieldUI.transform).WaitForCompletion().GetComponent<AngryOnlineLevelFieldComponent>();
            RectTransform currentUiRect = currentUi.GetComponent<RectTransform>();
            fieldUI.sizeDelta = currentUiRect.sizeDelta;
            currentUiRect.pivot = new Vector2(0, 1);
            currentUiRect.anchorMin = new Vector2(0, 1);
            currentUiRect.anchorMax = new Vector2(0, 1);
            currentUiRect.anchoredPosition = new Vector2(0, 0);

            currentUi.install.onClick.AddListener(() =>
            {
                if (OnlineBundle.EpilepsyWarning && !InternalConfigManager.ignoreEpilepsyWarning.value)
                {
                    EpilepsyWarningNotification notification = new EpilepsyWarningNotification(Download, "Download", "Download and do not ask again");
                    NotificationPanel.Open(notification);
                }
                else
                {
                    Download();
                }
			});
            currentUi.install.gameObject.AddComponent<DisableWhenHidden>();
            AngryUIUtils.AddMouseEvents(currentUi.gameObject, currentUi.install,
                (e) =>
                {
                    if (InstallActive)
                        currentUi.install.gameObject.SetActive(true);
                },
                (e) =>
                {
                    currentUi.install.gameObject.SetActive(false);
                });

            currentUi.upvoteButton.onClick.AddListener(() =>
            {
                AngryVotes.VoteOperation op = (VoteStatus == VoteStat.Upvoted) ? AngryVotes.VoteOperation.CLEAR : AngryVotes.VoteOperation.UPVOTE;
                VoteStatus = VoteStat.Disabled;

                AngryVotes.VoteTask(OnlineBundle.Guid, op).ContinueWith((resTask) =>
                {
                    var res = resTask.Result;

                    if (res.completedSuccessfully && res.status == AngryVotes.VoteStatus.VOTE_OK)
                    {
                        if (res.operation == AngryVotes.VoteOperation.UPVOTE)
                            VoteStatus = VoteStat.Upvoted;
                        else if (res.operation == AngryVotes.VoteOperation.DOWNVOTE)
                            VoteStatus = VoteStat.Downvoted;
                        else
                            VoteStatus = VoteStat.Cleared;

                        VoteCount = res.response.upvotes - res.response.downvotes;
                    }
                    else
                    {
                        Plugin.logger.LogError($"Could not vote! Message: {res.message}. Status: {res.status}.");

                        VoteStatus = VoteStat.Disabled;
                        VoteCount = 0;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            });
            currentUi.upvoteButton.gameObject.AddComponent<DisableWhenHidden>();
            currentUi.upvoteButton.gameObject.SetActive(false);
			AngryUIUtils.AddMouseEvents(currentUi.gameObject, currentUi.upvoteButton,
                (e) => currentUi.upvoteButton.gameObject.SetActive(!OnlineBundle.Locked),
                (e) => currentUi.upvoteButton.gameObject.SetActive(false)
                );

			currentUi.downvoteButton.onClick.AddListener(() =>
			{
				AngryVotes.VoteOperation op = (VoteStatus == VoteStat.Downvoted) ? AngryVotes.VoteOperation.CLEAR : AngryVotes.VoteOperation.DOWNVOTE;
				VoteStatus = VoteStat.Disabled;

				AngryVotes.VoteTask(OnlineBundle.Guid, op).ContinueWith((resTask) =>
				{
                    var res = resTask.Result;

					if (res.completedSuccessfully && res.status == AngryVotes.VoteStatus.VOTE_OK)
					{
						if (res.operation == AngryVotes.VoteOperation.UPVOTE)
							VoteStatus = VoteStat.Upvoted;
						else if (res.operation == AngryVotes.VoteOperation.DOWNVOTE)
							VoteStatus = VoteStat.Downvoted;
						else
							VoteStatus = VoteStat.Cleared;

						VoteCount = res.response.upvotes - res.response.downvotes;
					}
					else
					{
						Plugin.logger.LogError($"Could not vote! Message: {res.message}. Status: {res.status}.");

						VoteStatus = VoteStat.Disabled;
						VoteCount = 0;
					}
				}, TaskScheduler.FromCurrentSynchronizationContext());
			});
			currentUi.downvoteButton.gameObject.AddComponent<DisableWhenHidden>();
			currentUi.downvoteButton.gameObject.SetActive(false);
			AngryUIUtils.AddMouseEvents(currentUi.gameObject, currentUi.downvoteButton,
				(e) => currentUi.downvoteButton.gameObject.SetActive(!OnlineBundle.Locked),
				(e) => currentUi.downvoteButton.gameObject.SetActive(false)
				);

            currentUi.changelog.onClick.AddListener(() =>
            {
                LevelUpdateNotification notification = new LevelUpdateNotification();
                notification.currentHash = (Bundle == null || Status == OnlineLevelStatus.NotInstalled) ? "" : Bundle.BuildHash;
                notification.onlineInfo = OnlineBundle;
                notification.callback = this;
                NotificationPanel.Open(notification);
            });
            currentUi.changelog.gameObject.AddComponent<DisableWhenHidden>();
            currentUi.changelog.gameObject.SetActive(false);
            AngryUIUtils.AddMouseEvents(currentUi.gameObject, currentUi.changelog,
                (e) =>
                {
                    if (!Downloading)
                        currentUi.changelog.gameObject.SetActive(true);
                },
                (e) =>
                {
                    currentUi.changelog.gameObject.SetActive(false);
                });

            currentUi.update.onClick.AddListener(() =>
            {
                if (OnlineBundle.Updates == null)
                {
                    Download();
                }
                else
                {
                    if (Bundle == null || string.IsNullOrEmpty(Bundle.pathToAngryBundle) || !File.Exists(Bundle.pathToAngryBundle))
                    {
                        Download();
                        return;
                    }

                    LevelUpdateNotification notification = new LevelUpdateNotification();
                    notification.currentHash = Bundle.BuildHash;
                    notification.onlineInfo = OnlineBundle;
                    notification.callback = this;
                    NotificationPanel.Open(notification);
                }
            });
            currentUi.update.gameObject.AddComponent<DisableWhenHidden>();
			AngryUIUtils.AddMouseEvents(currentUi.gameObject, currentUi.update,
				(e) =>
				{
					if (UpdateActive)
						currentUi.update.gameObject.SetActive(true);
				},
				(e) =>
				{
					currentUi.update.gameObject.SetActive(false);
				});

			currentUi.progressText.resizeTextForBestFit = true;
            currentUi.progressText.resizeTextMaxSize = currentUi.progressText.fontSize;

            currentUi.cancel.onClick.AddListener(() =>
            {
                if (onCancel != null)
                    onCancel.Invoke();
            });

            if (hierarchyHidden)
				currentContainer.gameObject.SetActive(false);

            // Update UI by calling property setters
            PreviewImage = PreviewImage;
            OnlineBundle = OnlineBundle;
			VoteCount = VoteCount;
            VoteStatus = VoteStatus;
		}

		/// <inheritdoc/>
		public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
        {
            if (currentContainer != null)
				currentContainer.gameObject.SetActive(!hierarchyHidden);
        }

        private Task downloadTask = null;
        /// <summary>
        /// True if there is a download in progress.
        /// </summary>
        public bool Downloading
        {
            get => downloadTask != null && !downloadTask.IsCompleted;
        }

        /// <summary>
        /// If there is a download in progress, the property is set to a value between 0 and 1, indicating
        /// amount of data downloaded. If there is no download in progress, the property is set to -1.
        /// </summary>
        public float DownloadProgress { get; private set; } = -1f;

        /// <summary>
        /// Start downloading the bundle associated with this field. If there is a download in progress, this
        /// method will have no effect. Download progress can be tracted with properties.
        /// </summary>
        public void Download()
        {
            if (Downloading)
                return;

            if (currentUi != null)
            {
				currentUi.downloadContainer.gameObject.SetActive(true);
				currentUi.progressBar.localScale = new Vector3(0, 1, 1);
			}

            downloadTask = DownloadTask().ContinueWith((res) =>
            {
                DownloadProgress = -1f;

				UpdateStatus();
                if (currentUi != null)
				    currentUi.downloadContainer.gameObject.SetActive(false);

				OnlineLevelsUI.CheckLevelUpdateText();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

		/// <summary>
		/// Forcefully stop the current download. If there is no download in progress, this method
		/// will have no effect.
		/// </summary>
		/// <returns>True if the download was stopped. False if there was no download in progress.</returns>
		public bool AbortDownload()
        {
            if (!Downloading)
                return false;

            if (onCancel == null)
                return false;

            onCancel.Invoke();
            return true;
        }

        private async Task DownloadTask()
        {
            BundleInfo bundle = OnlineBundle;
            ErrorStatus = ErrorStat.NoError;
            DownloadProgress = 0f;

			if (currentUi != null)
            {
                currentUi.changelog.gameObject.SetActive(false);
                currentUi.install.gameObject.SetActive(false);
                currentUi.update.gameObject.SetActive(false);
            }

            List<string> downloadedParts = new List<string>();
            string fileMegabytes = (bundle.Size / (float)(1024 * 1024)).ToString("0.0");
            ulong downloadedBytes = 0;

            string tempDownloadDir = Path.Combine(Plugin.dataPath, "TempDownloads");
            if (!Directory.Exists(tempDownloadDir))
                Directory.CreateDirectory(tempDownloadDir);

            int partCount = bundle.Parts.Count;
            for (int i = 0; i < partCount; i++)
            {
                string tempDownloadPath = Path.Combine(tempDownloadDir, $"{bundle.Guid}.angry{i}");
                if (File.Exists(tempDownloadPath))
                    File.Delete(tempDownloadPath);
                downloadedParts.Add(tempDownloadPath);

                UnityWebRequest req = new UnityWebRequest(bundle.Parts[i]);
                req.downloadHandler = new DownloadHandlerFile(tempDownloadPath);
                var handle = req.SendWebRequest();

                CancellationTokenSource abortToken = new CancellationTokenSource();
                onCancel = new UnityEvent();
                onCancel.AddListener(() =>
                {
                    if (!Downloading)
                        return;

                    req.Abort();
                    abortToken.Cancel();
                });

                while (!handle.isDone)
                {
                    if (currentUi != null)
                    {
                        DownloadProgress = Mathf.Clamp01((float)(req.downloadedBytes + downloadedBytes) / bundle.Size);
						currentUi.progressBar.transform.localScale = new Vector3(DownloadProgress, 1, 1);
                        string downloadedFileMegabytes = ((req.downloadedBytes + downloadedBytes) / (float)(1024 * 1024)).ToString("0.0");
                        currentUi.progressText.text = $"{downloadedFileMegabytes}/{fileMegabytes}\nMB\n(Part {i + 1}/{partCount})";
                    }

                    await Task.Delay(500, abortToken.Token);
                }

                onCancel = new UnityEvent();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (!abortToken.Token.IsCancellationRequested)
                        ErrorStatus = ErrorStat.NetworkError;

                    foreach (string part in downloadedParts)
                        if (File.Exists(part))
                            File.Delete(part);

                    return;
                }

                downloadedBytes += req.downloadedBytes;
                req.Dispose();
            }

            string combinedFilePath = Path.Combine(tempDownloadDir, AngryIOUtils.GetUniqueFileName(tempDownloadDir, "combined_file"));
            using (FileStream str = File.Open(combinedFilePath, FileMode.OpenOrCreate, FileAccess.Write))
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
			if (AngryFileUtils.TryGetAngryBundleData(combinedFilePath, out AngryBundleData data, out Exception e))
			{
				if (data.bundleGuid != bundle.Guid)
					valid = false;
				else if (data.buildHash != bundle.Hash)
					Plugin.logger.LogWarning($"Downloaded bundle has hash {data.buildHash} but most recent one is {bundle.Hash}");
			}
			else
			{
				Plugin.logger.LogError($"Threw error while validating downloaded file\n{e}");
				valid = false;
			}

			if (!valid)
			{
				File.Delete(combinedFilePath);
				ErrorStatus = ErrorStat.ValidationError;
				return;
			}

			string destinationFolder = Plugin.levelsPath;
			if (!Directory.Exists(destinationFolder))
				Directory.CreateDirectory(destinationFolder);
			string destinationFile = Path.Combine(destinationFolder, AngryIOUtils.GetUniqueFileName(destinationFolder, AngryIOUtils.GetPathSafeName(bundle.Name) + ".angry"));
			if (Bundle != null && !string.IsNullOrEmpty(Bundle.pathToAngryBundle) && File.Exists(Bundle.pathToAngryBundle))
				destinationFile = Bundle.pathToAngryBundle;

            if (File.Exists(destinationFile))
            {
				// Plugin.watcherChangedPathIgnoreList.Add(Path.GetFullPath(destinationFile));

				using (FileStream destStream = File.Open(destinationFile, FileMode.OpenOrCreate, FileAccess.Write))
                {
					destStream.Seek(0, SeekOrigin.Begin);
                    destStream.SetLength(0);

					using (FileStream srcStream = File.Open(combinedFilePath, FileMode.Open, FileAccess.Read))
                    {
                        srcStream.CopyTo(destStream);
                    }
                }

                File.Delete(combinedFilePath);
            }
            else
            {
                File.Move(combinedFilePath, destinationFile);
            }

			if (Bundle == null || !AngryIOUtils.PathEquals(Bundle.pathToAngryBundle, destinationFile))
            {
                // Plugin.ProcessPath(destinationFile);
                Plugin.ScanForLevels();
            }
            else
            {
				LastPlayedMapManager.UpdateLastUpdate(Bundle);

                if (!(AngrySceneManager.isInCustomLevel && AngrySceneManager.currentBundleContainer == Bundle))
                {
					_ = Bundle.ReloadBundle(false, false);
                }

                // ELSE THERE WILL BE A PROMPT FROM FILE SYSTEM WATCHER
            }
		}

        // Update order for this field only, assuming every other field is ordered correctly
        internal void UpdateOrder()
        {
            int order = 0;
            OnlineLevelField[] allBundles = OnlineLevelsUI.onlineLevels.Values.OrderBy(level => level.siblingIndex).ToArray();

            if (OnlineLevelsUI.sortFilter.value == OnlineLevelsUI.SortFilter.Name)
            {
                while (order < allBundles.Length)
                {
                    if (order == siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (string.Compare(OnlineBundle.Name, allBundles[order].OnlineBundle.Name) == -1)
                        break;

                    order += 1;
                }
            }
            else if (OnlineLevelsUI.sortFilter.value == OnlineLevelsUI.SortFilter.Author)
            {
                while (order < allBundles.Length)
                {
                    if (order == siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (string.Compare(OnlineBundle.Author, allBundles[order].OnlineBundle.Author) == -1)
                        break;

                    order += 1;
                }
            }
            else if (OnlineLevelsUI.sortFilter.value == OnlineLevelsUI.SortFilter.LastUpdate)
            {
                while (order < allBundles.Length)
                {
                    if (order == siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (OnlineBundle.LastUpdate > allBundles[order].OnlineBundle.LastUpdate)
                        break;

                    order += 1;
                }
            }
			else if (OnlineLevelsUI.sortFilter.value == OnlineLevelsUI.SortFilter.Votes)
			{
				while (order < allBundles.Length)
				{
					if (order == siblingIndex)
					{
						order += 1;
						continue;
					}

					if (VoteCount > allBundles[order].VoteCount)
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
