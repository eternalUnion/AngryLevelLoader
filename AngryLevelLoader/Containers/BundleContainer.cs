using PluginConfig.API.Decorators;
using PluginConfig.API.Functionals;
using PluginConfig.API;
using RudeLevelScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO.Compression;
using Newtonsoft.Json;
using AngryLevelLoader.Fields;
using PluginConfig.API.Fields;
using AngryLevelLoader.Managers;
using AngryLevelLoader.DataTypes;
using PluginConfig;
using AngryLevelLoader.Notifications;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AngryLevelLoader.UserInterface;
using AngryLevelLoader.Utils;
using AngryLevelLoader.Extensions;

namespace AngryLevelLoader.Containers
{
    /// <summary>
    /// Represents a bundle in the level list of the main panel
    /// </summary>
    public class BundleContainer
    {
        public const int MIN_ANGRY_FILE_VERSION = 6;
		public const int MAX_ANGRY_FILE_VERSION = 7;

		// Addressables data

		private IResourceLocator locator = null;
		private Dictionary<string, AsyncOperationHandle<RudeLevelData>> dataDictionary = new Dictionary<string, AsyncOperationHandle<RudeLevelData>>();
		private string pathToTempFolder;
		internal string pathToAngryBundle;

		// Properties for bundle status

		/// <summary>
		/// Indicates whether the bundle is loaded into the addressables or not.
        /// On boot, bundles are partially loaded to increase performance. In that case,
        /// this field returns false while LazyLoaded returns true. To fully load a
        /// bundle, call <see cref="ReloadBundle(bool, bool)"/> with lazyLoad set to false.
		/// </summary>
		public bool Loaded => locator != null;

        /// <summary>
        /// Indicates whether only the essential data is loaded for UI only. Lazy loaded
        /// bundles cannot be played without being fully loaded first.
        /// </summary>
        public bool LazyLoaded => bundleData != null;

		/// <summary>
		/// Bundles may be deleted, which invalidates this container until the file is added or downloaded again.
		/// </summary>
		public bool HasValidAngryFile => !string.IsNullOrEmpty(pathToAngryBundle) && File.Exists(pathToAngryBundle);

        /// <summary>
        /// Some older angry files are not supported because of game updates. Newer angry files also cannot be loaded
        /// since the structure is unknown. This field always returns false if the bundle is not lazy loaded.
        /// </summary>
        public bool AngryFileSupported => (bundleData == null) ? false : bundleData.bundleVersion >= MIN_ANGRY_FILE_VERSION && bundleData.bundleVersion <= MAX_ANGRY_FILE_VERSION;

		internal bool IgnoreFileChange { get; set; } = false;
		/// <summary>
		/// If a bundle is updated while it is being actively played, the loaded bundle must be unloaded
		/// before loading the new bundle. This field is set if the file was updated and bundle must
		/// be reloaded.
		/// </summary>
		public bool FileChangeDetected { get; private set; } = false;

		internal bool TryGetRudeLevelData(string levelId, out RudeLevelData rudeLevelData)
		{
			if (dataDictionary.TryGetValue(levelId, out var handler))
			{
				rudeLevelData = handler.Result;
				return true;
			}

			rudeLevelData = null;
			return false;
		}

		internal IEnumerable<RudeLevelData> GetAllRudeLevelData()
		{
			return dataDictionary.Values.Select(handler => handler.Result);
		}

		internal IEnumerable<string> GetAllScenePaths()
		{
			return GetAllRudeLevelData().Select(data => data.scenePath);
		}

		// Bundle data

		public readonly string bundleGuid;
		private AngryBundleData bundleData;
		private Dictionary<string, LevelContainer> levels = new Dictionary<string, LevelContainer>();

		/// <summary>
		/// Get all loaded level containers loaded by the bundle.
		/// The levels are not loaded until the bundle is fully loaded,
		/// unless <see cref="LazyUILoadingSupported"/> is set in which
		/// case lazy loading is sufficient.
		/// </summary>
		public IEnumerable<LevelContainer> GetAllLevelContainers()
		{
			return levels.Values;
		}

		/// <summary>
		/// Attempt to get a level container from its unique identifier.
		/// </summary>
        public bool TryGetLevelContainer(string levelId, out LevelContainer levelContainer)
        {
            return levels.TryGetValue(levelId, out levelContainer);
        }

		// Helper fields for the bundle data

		/// <summary>
		/// Angry version of the file. Returns 0 if the bundle is not lazy loaded.<br></br>
		/// v0: (file not loaded)<br></br>
		/// v1: Back to cybergrind update (legacy file format)<br></br>
		/// v2: Back to cybergrind update (modern file format)<br></br>
		/// v3: Violence update<br></br>
		/// v4: Full arsenal update<br></br>
		/// v5: Full arsenal patch update<br></br>
		/// v6: Revamp update (supported)<br></br>
		/// v7: Fraud update (supported)<br></br>
		/// </summary>
		public int BundleVersion => (bundleData == null) ? 0 : bundleData.bundleVersion;

        public string BundleName => (bundleData == null) ? "<invalid>" : bundleData.bundleName;

        public string BundleAuthor => (bundleData == null) ? "<invalid>" : bundleData.bundleAuthor;

        public bool EpilepsyWarning => (bundleData == null) ? false : bundleData.epilepsyWarning;

		/// <summary>
		/// Build hash is used to distinguish different versions of the same angry file.
		/// For example, local hash can be compared to the online catalog.
		/// Returns empty string if the bundle is invalid.
		/// </summary>
		public string BuildHash => (bundleData == null) ? string.Empty : bundleData.buildHash;

        // UI

		internal bool Favourite
		{
			get => favourite.value;
			set
			{
				if (favourite.value == value)
					return;

				favourite.value = value;
				AngryBundleList.SortBundles();
			}
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

				rootPanel.displayName = BundleName;
				rootPanel.displayName += $"\n<color=#909090>by {BundleAuthor}</color>";
				rootPanel.hidden = false;

				if (_searchKeywords.Length > 0)
                {
					string bundleName = richText.Replace(BundleName, string.Empty);
					string authorName = richText.Replace(BundleAuthor, string.Empty);

					WordHighlighter formattedName = new WordHighlighter(bundleName);
					WordHighlighter formattedAuthor = new WordHighlighter(authorName);

					bundleName = bundleName.ToLower();
					authorName = authorName.ToLower();

					foreach (string keyword in _searchKeywords)
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
							rootPanel.hidden = true;
							return;
                        }
					}

					rootPanel.displayName = formattedName.GenerateFormattedText("<color=yellow><b>", "</b></color>");
					rootPanel.displayName += "\n<color=#909090>by " + formattedAuthor.GenerateFormattedText("<color=yellow><b>", "</b></color>") + "</color>";
				}
            }
        }

        internal Sprite Icon => rootPanel.icon;

        internal bool Hidden { get => rootPanel.hidden; set => rootPanel.hidden = value; }

        internal int SiblingIndex { get => rootPanel.siblingIndex; set => rootPanel.siblingIndex = value; }

		private BoolField favourite;
        private ConfigPanelForBundles rootPanel;
        private LoadingCircleField loadingCircle;
        private ConfigHeader statusText;
        private ConfigDivision sceneDiv;
        private IntField finalRankScore;

		internal bool RecalculateFinalRank()
		{
			int totalRankScore = 0;
			int currentRankScore = 0;

			if (LazyUILoadingSupported)
			{
				if (!LazyLoaded)
					return false;

				foreach (var level in bundleData.levels)
				{
					if (level.isSecretLevel)
						continue;

					totalRankScore += 6;

					if (levels.TryGetValue(level.uniqueIdentifier, out LevelContainer container))
					{
						currentRankScore += Math.Max(0, AngryRankUtils.GetRankScore(container.FinalRank));
					}
				}
			}
			else
			{
				if (!Loaded)
					return false;

				foreach (var level in GetAllRudeLevelData())
				{
					if (level.isSecretLevel)
						continue;

					totalRankScore += 6;

					if (levels.TryGetValue(level.uniqueIdentifier, out LevelContainer container))
					{
						currentRankScore += Math.Max(0, AngryRankUtils.GetRankScore(container.FinalRank));
					}
				}
			}
			
			finalRankScore.value = totalRankScore <= 0 ? 0 : (int)(((float)currentRankScore / totalRankScore) * 6f);
			return true;
		}

		internal void UpdateAllUI()
		{
			foreach ((string levelId, LevelContainer levelContainer) in levels)
			{
                bool locked = false;
                foreach (string requiredLevelId in levelContainer.LevelData.requiredCompletedLevelIdsForUnlock)
                {
					if (levels.TryGetValue(requiredLevelId, out LevelContainer requiredLevel))
					{
						if (requiredLevel.FinalRank == '-')
						{
							locked = true;
							break;
						}
					}
					else
					{
						Plugin.logger.LogWarning($"Could not find level unlock requirement id for {levelId}, requested id was {requiredLevelId}");
						locked = true;
						break;
					}
				}

                levelContainer.Locked = locked;
			}

			RecalculateFinalRank();
		}

        // Bundle load/unload/refresh

		private async Task Unload()
        {
            // Release data handle
            foreach (AsyncOperationHandle<RudeLevelData> data in dataDictionary.Values)
            {
                Addressables.Release(data);
            }
            dataDictionary.Clear();

            // Unload the content catalog
            if (locator != null)
            {
                Addressables.RemoveResourceLocator(locator);
                while (!Caching.ready)
                    await Task.Yield();
                await AssetManager.CleanBundleCache();
			}

            locator = null;
			FileChangeDetected = false;
		}

		/// <summary>
		/// If this property is set, current angry file supports loading level containers
		/// without fully loading the bundle.
		/// </summary>
		public bool LazyUILoadingSupported => BundleVersion > 7;

		/// <summary>
		/// Read .angry file and load the levels in memory
		/// </summary>
		/// <param name="forceReload">If set to false and a previously unzipped version exists, do not re-unzip the file</param>
		/// <returns>Success</returns>
		private async Task<bool> ReloadData(bool forceReload, bool lazyLoad)
        {
            // Open the angry zip archive
            if (!AngryFileUtils.TryGetAngryBundleData(pathToAngryBundle, out AngryBundleData latestData, out Exception error))
            {
                statusText.text = "<color=red>Invalid angry file!</color>";
				if (error != null)
				{
					statusText.text += $"\n{error.GetType().Name}: {error.Message}\n{error.StackTrace}";
				}
                statusText.hidden = false;
                return false;
            }

			await Unload();

			bool unzip = true;
            bool fileChanged = false;

			pathToTempFolder = Path.Combine(Plugin.tempFolderPath, latestData.bundleGuid);
            
            rootPanel.displayName = string.IsNullOrEmpty(latestData.bundleName) ? Path.GetFileNameWithoutExtension(pathToAngryBundle) : latestData.bundleName;
            rootPanel.headerText = $"--{rootPanel.displayName}--";
            rootPanel.displayName += $"\n<color=#909090>by {latestData.bundleAuthor}</color>";

            // If force reload is set to false, check if the build hashes match
            // between unzipped bundle and the current angry file. Avoids unnecessary unzips
            if (Directory.Exists(pathToTempFolder) && File.Exists(Path.Combine(pathToTempFolder, "data.json")) && File.Exists(Path.Combine(pathToTempFolder, "catalog.json")))
            {
                AngryBundleData previousData = JsonConvert.DeserializeObject<AngryBundleData>(File.ReadAllText(Path.Combine(pathToTempFolder, "data.json")));
                if (previousData.buildHash == latestData.buildHash)
                {
                    if (!forceReload)
                        unzip = false;
                }
                else
                {
                    fileChanged = true;
                }
            }
            else
            {
                fileChanged = true;
            }

            if (unzip)
            {
	            int retries = 50;
	            while (Directory.Exists(pathToTempFolder) && retries-- > 0)
	            {
		            Directory.Delete(pathToTempFolder, true);
		            await Task.Delay(100);
	            }
	            if (retries <= 0)
				{
					statusText.text = "<color=red>Failed to clear temporary folder, try reloading again</color>";
					statusText.hidden = false;
					return false;
				}

                Directory.CreateDirectory(pathToTempFolder);

                using (ZipArchive zip = new ZipArchive(File.Open(pathToAngryBundle, FileMode.Open, FileAccess.Read)))
                    zip.ExtractToDirectory(pathToTempFolder);
            }

			if (fileChanged)
				LastPlayedMapManager.UpdateLastUpdate(this);

			bundleData = JsonConvert.DeserializeObject<AngryBundleData>(File.ReadAllText(Path.Combine(pathToTempFolder, "data.json")));
			rootPanel.SetIconWithURL("file://" + Path.Combine(pathToTempFolder, "icon.png"));
            rootPanel.forceHidden = !AngryFileSupported;
			rootPanel.headerText = $"--{BundleName}--";
            // This sets the bundle name on the main panel
			SearchKeywords = SearchKeywords;

			// If the bundle is made for an older version and the panel is open, go back to the levels panel
			if (!AngryFileSupported
				&& rootPanel.currentPanel != null
				&& PluginConfiguratorController.activePanel == rootPanel.currentPanel.gameObject)
			{
				ConfigManager.config.rootPanel.OpenPanel();
			}

            // Cannot load unsupported bundles
            if (!AngryFileSupported)
                return false;

			// We don't need to load the bunde assets if all we need is the bundle interface
			if (lazyLoad)
                return true;

			// Load the catalog
			var addressableHandle = Addressables.LoadContentCatalogAsync(Path.Combine(pathToTempFolder, "catalog.json"), false);
			await addressableHandle;
			locator = addressableHandle.Result;

            // Load the level data
            statusText.text = "";
            statusText.hidden = true;
            foreach (string path in bundleData.levelDataPaths)
            {
                AsyncOperationHandle<RudeLevelData> handle = Addressables.LoadAssetAsync<RudeLevelData>(path);
                await handle;
                RudeLevelData data = handle.Result;

                if (data == null)
                {
                    handle.Release();
                    continue;
                }

                dataDictionary[data.uniqueIdentifier] = handle;
            }

            if (bundleData.bundleVersion < 7)
            {
				statusText.hidden = false;
				if (!string.IsNullOrEmpty(statusText.text))
                    statusText.text += '\n';
                
                if (bundleData.bundleVersion == 6)
                {
					statusText.text += $"<color=yellow>Warning: </color>Bundle was made for the Revamp update of Ultrakill. Expect issues.";
				}
                else
                {
					statusText.text += $"<color=yellow>Warning: </color>Bundle was made for an older version of Ultrakill. Expect issues.";
				}
            }

            return true;
        }

        private async Task<bool> ReloadBundleTask(bool forceReload, bool lazyLoad)
        {
            if (!File.Exists(pathToAngryBundle))
            {
                statusText.text = "<color=red>Could not find the file</color>";
                return false;
            }

            if (AngryFileUtils.TryGetAngryBundleData(pathToAngryBundle, out AngryBundleData fileData, out _) && fileData.bundleGuid != bundleData.bundleGuid)
            {
                statusText.text = "<color=red>Target file has a different guid</color>";
                return false;
            }
            
            bool inTempScene = false;
            string previousPath = SceneManager.GetActiveScene().path;
            string previousName = SceneManager.GetActiveScene().name;
            string previousId = AngrySceneManager.isInCustomLevel ? AngrySceneManager.currentLevelData.uniqueIdentifier : "";
            if (GetAllScenePaths().Contains(previousPath))
            {
				string tempSceneToLoad = (ConfigManager.reloadAlwaysGoToMainMenu.value) ? "Main Menu" : "AngryLevelLoader/Blank";

				TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
                SceneHelper.LoadSceneAsync(tempSceneToLoad).ContinueWith(SceneHelper.Instance, () => completionSource.SetResult(true));
                await completionSource.Task;
				await Task.Yield();

				if (AngrySceneManager.isInCustomLevel)
				{
					Plugin.logger.LogError("Failed to switch to blank scene, have no other option other than returning to main menu");
					completionSource = new TaskCompletionSource<bool>();
					SceneHelper.LoadSceneAsync("Main Menu").ContinueWith(SceneHelper.Instance, () => completionSource.SetResult(true));
					await completionSource.Task;
					SceneHelper.ShowLoadingBlocker();
					await Task.Yield();
				}
                
                inTempScene = true;
			}

			// Reload data from file
			bool reloadDataSuccess = await ReloadData(forceReload, lazyLoad);

			// Disable all level interfaces
			foreach (KeyValuePair<string, LevelContainer> pair in levels)
                pair.Value.ForceHidden = true;
			
			if (!reloadDataSuccess)
			{
				if (inTempScene)
				{
					TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
					SceneHelper.LoadSceneAsync("Main Menu").ContinueWith(SceneHelper.Instance, () => completionSource.SetResult(true));
					await completionSource.Task;
				}

				return false;
			}

			// Update online field if there are any
			if (OnlineLevelsList.onlineLevels.TryGetValue(bundleGuid, out OnlineLevelField field))
			{
				field.UpdateStatus();
				OnlineLevelsList.CheckLevelUpdateText();
			}

			// Modern bundles support loading the UI without fully loading the bundle into addressables
			if (LazyUILoadingSupported)
			{
				sceneDiv.hidden = true;

				// Create levels from metadata
				int currentIndex = 0;
				foreach (AngryLevelData levelData in bundleData.levels.OrderBy(d => d.prefferedLevelOrder))
				{
					if (levels.TryGetValue(levelData.uniqueIdentifier, out LevelContainer container))
					{
						container.UpdateData(levelData);
					}
					else
					{
						levels[levelData.uniqueIdentifier] = container = new LevelContainer(sceneDiv, this, levelData);
					}

					if (lazyLoad || !dataDictionary.TryGetValue(levelData.uniqueIdentifier, out var rudeLevelData))
					{
						container.LoadPreviewImageFromUrl($"file://{Path.Combine(pathToTempFolder, "LevelThumbnails", AngryCryptographyUtils.GetMD5String(levelData.uniqueIdentifier))}.png");
					}
					else
					{
						container.PreviewImage = rudeLevelData.Result.levelPreviewImage;
					}

					container.SiblingIndex = currentIndex++;
					container.ForceHidden = false;
				}

				sceneDiv.hidden = false;

				UpdateAllUI();
			}

			// If only loading UI elements are required, return early
			if (lazyLoad)
            {
                if (inTempScene)
					SceneHelper.LoadScene("Main Menu", true);
                
				return true;
            }

			// If lazy UI loading is not supported (in older bundles), load data from rude level data object
			if (!LazyUILoadingSupported)
			{
				sceneDiv.hidden = true;

				// Create levels from metadata
				int currentIndex = 0;
				foreach (RudeLevelData levelData in GetAllRudeLevelData().OrderBy(d => d.prefferedLevelOrder))
				{
					if (levels.TryGetValue(levelData.uniqueIdentifier, out LevelContainer container))
					{
						container.UpdateData(AngryLevelData.FromRudeLevelData(levelData));
					}
					else
					{
						levels[levelData.uniqueIdentifier] = container = new LevelContainer(sceneDiv, this, AngryLevelData.FromRudeLevelData(levelData));
					}

					container.PreviewImage = levelData.levelPreviewImage;
					container.SiblingIndex = currentIndex++;
					container.ForceHidden = false;
				}

				sceneDiv.hidden = false;

				UpdateAllUI();
			}

			if (inTempScene)
            {
                RudeLevelData lastLevelData = GetAllRudeLevelData().Where(l => l.uniqueIdentifier == previousId).FirstOrDefault();

				if (lastLevelData == null)
				{
					SceneHelper.LoadScene("Main Menu", true);
				}
                else
                {
                    SceneHelper.LoadScene(lastLevelData.scenePath, true);
                }
			}
            
            return true;
        }

        private Task<bool> updateTask = null;
		/// <summary>
		/// Returns true if the bundle is currently being reloaded.
		/// Bundle cannot be reloaded while this property is set.
		/// </summary>
        public bool Updating
        {
            get => updateTask != null && !updateTask.IsCompleted;
        }

		/// <summary>
		/// Reloads the angry file and adds the new scenes. This method has no effect if <see cref="Updating"/> is set.
		/// </summary>
		/// <param name="forceReload">If set to false, previously unzipped files can be used instead of deleting and re-unzipping</param>
		/// <param name="lazyLoad">If set to true, only the data required for the user interface will be loaded. Lazily loaded bundles cannot be played until fully loaded.</param>
		/// <returns>True if the bundle was successfully reloaded. False if an error occured while reloading the bundle.</returns>
		public Task<bool> ReloadBundle(bool forceReload, bool lazyLoad)
        {
            if (Updating)
                return updateTask;

			statusText.hidden = true;
			statusText.text = "";

			updateTask = ReloadBundleTask(forceReload, lazyLoad);
            
			return updateTask;
		}

        internal async Task DeleteBundle()
        {
            if (File.Exists(pathToAngryBundle))
                File.Delete(pathToAngryBundle);

            if (Directory.Exists(pathToTempFolder))
                Directory.Delete(pathToTempFolder, true);

            if (OnlineLevelsList.onlineLevels.TryGetValue(bundleData.bundleGuid, out var onlineField))
            {
                onlineField.UpdateStatus();
            }

            pathToAngryBundle = "";
            pathToTempFolder = "";
            bundleData = null;
            rootPanel.forceHidden = true;

            await Unload();
		}

        internal void OpenDeletePanel()
        {
            NotificationPanel.Open(new DeleteBundleNotification(this));
        }

        internal void FileChanged()
        {
            IgnoreFileChange = false;

			if (AngryFileUtils.TryGetAngryBundleData(pathToAngryBundle, out AngryBundleData updatedData, out Exception e))
            {
                // Different guid, would break the container
                if (updatedData.bundleGuid != bundleGuid)
                {
                    Plugin.logger.LogError($"File {Path.GetFileName(pathToAngryBundle)} was changed, but the new file's guid does not match its container! Unlinking");
                    FileChangeDetected = false;
                    pathToAngryBundle = "";
                    return;
				}

                FileChangeDetected = updatedData.buildHash != bundleData.buildHash;
                if (FileChangeDetected)
				{
					LastPlayedMapManager.UpdateLastUpdate(this);
					if (AngrySceneManager.isInCustomLevel && AngrySceneManager.currentBundleContainer == this)
						AngryUI.ShowReloadBundlePrompt();
				}
			}
        }

        internal BundleContainer(string path, AngryBundleData data)
        {
            Plugin.logger.LogInfo($"Creating bundle container for {path}");
            pathToAngryBundle = path;
            bundleData = data;
            bundleGuid = bundleData.bundleGuid;

			favourite = new BoolField(InternalConfigManager.internalConfig.rootPanel, bundleGuid + "_fav", bundleGuid + "_fav", false);
            rootPanel = new ConfigPanelForBundles(this, ConfigManager.bundleDivision, data.bundleName, data.bundleGuid);
			rootPanel.Favourite = Favourite;
			rootPanel.forceHidden = true;
			rootPanel.onPannelOpenEvent += (e) =>
			{
				if (!LazyUILoadingSupported && !Loaded && !(updateTask != null && updateTask.IsCompleted && !updateTask.Result))
					ReloadBundle(false, false);
			};
            
            finalRankScore = new IntField(rootPanel, "final bundle rank", rootPanel.guid + "_finalRankCache", 0, true, false);
            finalRankScore.postValueChangeEvent += (val) =>
            {
				char finalRank = AngryRankUtils.GetRankChar(val);
				rootPanel.rankText = finalRank.ToString();
				rootPanel.rankTextColor = AngryRankUtils.GetRankColor(finalRank, Color.white);

				if (finalRank == 'P')
				{
					rootPanel.fieldColor = new Color(171 / 255f, 108 / 255f, 2 / 255f);

					rootPanel.fillBgCenter = true;
					rootPanel.rankBgColor = new Color(241 / 255f, 168 / 255f, 8 / 255f);

					rootPanel.rankTextColor = Color.white;
				}
				else
				{
					rootPanel.fieldColor = Color.black;

					rootPanel.fillBgCenter = false;
					rootPanel.rankBgColor = Color.white;
				}
			};
			finalRankScore.TriggerPostValueChangeEvent();

			ButtonArrayField reloadButtons = new ButtonArrayField(rootPanel, rootPanel.guid + "_reloadButtons", 2, new float[2] { 0.5f, 0.5f }, new string[] { "Reload File", "Force Reload File" });
            reloadButtons.OnClickEventHandler(0).onClick += () => ReloadBundle(false, false);
            reloadButtons.OnClickEventHandler(1).onClick += () => ReloadBundle(true, false);

            new SpaceField(rootPanel, 5);

            new ConfigHeader(rootPanel, "Levels");
            statusText = new ConfigHeader(rootPanel, "", 16, TMPro.TextAlignmentOptions.Left);
            statusText.hidden = true;
            loadingCircle = new LoadingCircleField(rootPanel);
            loadingCircle.hidden = true;
            sceneDiv = new ConfigDivision(rootPanel, "sceneDiv_" + rootPanel.guid);
        }
    }
}
