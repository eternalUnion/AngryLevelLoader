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
using System.Reflection;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO.Compression;
using Newtonsoft.Json;
using Unity.Audio;
using System.Collections;
using RudeLevelScripts.Essentials;
using AngryLevelLoader.Fields;
using PluginConfig.API.Fields;
using AngryLevelLoader.Managers;
using AngryLevelLoader.DataTypes;
using System.Xml.Linq;
using PluginConfig;
using AngryLevelLoader.Notifications;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace AngryLevelLoader.Containers
{
    public class AngryBundleContainer
    {
        public IResourceLocator locator = null;
        public string pathToTempFolder;
        public string pathToAngryBundle;

        public AngryBundleData bundleData;

        public Dictionary<string, AsyncOperationHandle<RudeLevelData>> dataDictionary = new Dictionary<string, AsyncOperationHandle<RudeLevelData>>();

        public ConfigPanelForBundles rootPanel;
        public LoadingCircleField loadingCircle;
        public ConfigHeader statusText;
        public ConfigDivision sceneDiv;
        public IntField finalRankScore;
        public Dictionary<string, LevelContainer> levels = new Dictionary<string, LevelContainer>();

        private async Task Unload()
        {
            // Release data handle
            foreach (AsyncOperationHandle<RudeLevelData> data in dataDictionary.Values)
            {
                Plugin.idDictionary.Remove(data.Result.uniqueIdentifier);
                Addressables.Release(data);
            }
            dataDictionary.Clear();

            // Unload the content catalog
            if (locator != null)
            {
                Addressables.RemoveResourceLocator(locator);
                await AssetManager.CleanBundleCache();
			}
        }

        /// <summary>
        /// Read .angry file and load the levels in memory
        /// </summary>
        /// <param name="forceReload">If set to false and a previously unzipped version exists, do not re-unzip the file</param>
        /// <returns>Success</returns>
        private async Task ReloadBundle(bool forceReload, bool lazyLoad)
        {
            await Unload();

            // Open the angry zip archive
            AngryBundleData latestData = AngryFileUtils.GetAngryBundleData(pathToAngryBundle);
            bool rewriteData = false;
            bool unzip = true;
            bool fileChanged = false;

            pathToTempFolder = Path.Combine(Plugin.tempFolderPath, latestData.bundleGuid);
            
            rootPanel.displayName = string.IsNullOrEmpty(latestData.bundleName) ? Path.GetFileNameWithoutExtension(pathToAngryBundle) : latestData.bundleName;
            rootPanel.headerText = $"--{rootPanel.displayName}--";
            if (!string.IsNullOrEmpty(latestData.bundleAuthor))
            {
                rootPanel.displayName += $"\n<color=#909090>by {latestData.bundleAuthor}</color>";
            }

            if (string.IsNullOrEmpty(latestData.bundleName))
            {
                lazyLoad = false;
                rewriteData = true;
            }

            if (latestData.bundleVersion == -1)
            {
                latestData.bundleVersion = 2;
                bundleData.bundleVersion = 2;

				using (ZipArchive angryFile = new ZipArchive(File.Open(pathToAngryBundle, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update))
				{
					ZipArchiveEntry dataEntry = angryFile.GetEntry("data.json");

					using (StreamWriter sw = new StreamWriter(dataEntry.Open()))
					{
						sw.BaseStream.SetLength(0);
						sw.BaseStream.Seek(0, SeekOrigin.Begin);
						await sw.WriteAsync(JsonConvert.SerializeObject(latestData));
						await sw.FlushAsync();
					}
				}
			}

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
                if (Directory.Exists(pathToTempFolder))
                    Directory.Delete(pathToTempFolder, true);
                Directory.CreateDirectory(pathToTempFolder);

                using (ZipArchive zip = new ZipArchive(File.Open(pathToAngryBundle, FileMode.Open, FileAccess.Read)))
                    zip.ExtractToDirectory(pathToTempFolder);

                bundleData = JsonConvert.DeserializeObject<AngryBundleData>(File.ReadAllText(Path.Combine(pathToTempFolder, "data.json")));
                rootPanel.SetIconWithURL("file://" + Path.Combine(pathToTempFolder, "icon.png"));
            }
            else
            {
                if (rootPanel.icon == null)
                    rootPanel.SetIconWithURL("file://" + Path.Combine(pathToTempFolder, "icon.png"));
            }

			if (fileChanged)
				Plugin.UpdateLastUpdate(this);

			// We don't need to load the bunde assets if all we need is the bundle interface
			if (lazyLoad)
                return;

			fileChangeDetected = false;

			// Load the catalog
			var addressableHandle = Addressables.LoadContentCatalogAsync(Path.Combine(pathToTempFolder, "catalog.json"), false);
            await addressableHandle;
            locator = addressableHandle.Result;

            // Load the bundle name
            if (!string.IsNullOrEmpty(bundleData.bundleDataPath))
            {
                AsyncOperationHandle<RudeBundleData> bundleHandle = Addressables.LoadAssetAsync<RudeBundleData>(bundleData.bundleDataPath);
                await bundleHandle;
                RudeBundleData bundleDataObj = bundleHandle.Result;

                if (bundleDataObj != null)
                {
                    rootPanel.displayName = bundleDataObj.bundleName;
                    if (!string.IsNullOrEmpty(bundleDataObj.author))
                    {
                        rootPanel.displayName += $"\n<color=#909090>by {bundleDataObj.author}</color>";
                    }

                    rootPanel.headerText = $"--{bundleDataObj.bundleName}--";

                    bundleData.bundleName = bundleDataObj.bundleName;
                    bundleData.bundleAuthor = bundleDataObj.author;
                }

                bundleDataObj = null;
                Addressables.Release(bundleHandle);
            }

            if (rewriteData)
            {
				using (ZipArchive angryFile = new ZipArchive(File.Open(pathToAngryBundle, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update))
                {
                    ZipArchiveEntry dataEntry = angryFile.GetEntry("data.json");

                    using (StreamWriter sw = new StreamWriter(dataEntry.Open()))
                    {
                        sw.BaseStream.SetLength(0);
                        sw.BaseStream.Seek(0, SeekOrigin.Begin);
                        await sw.WriteAsync(JsonConvert.SerializeObject(bundleData));
                        await sw.FlushAsync();
                    }
                }
            }

            // Load the level data
            statusText.text = "";
            statusText.hidden = true;
            foreach (string path in bundleData.levelDataPaths)
            {
                AsyncOperationHandle<RudeLevelData> handle = Addressables.LoadAssetAsync<RudeLevelData>(path);
                await handle;
                RudeLevelData data = handle.Result;

                if (data == null)
                    continue;

                if (Plugin.idDictionary.ContainsKey(data.uniqueIdentifier))
                {
                    Plugin.logger.LogWarning($"Duplicate or invalid unique id {data.scenePath}");
                    statusText.hidden = false;
                    if (!string.IsNullOrEmpty(statusText.text))
                        statusText.text += '\n';
                    statusText.text += $"<color=red>Error: </color>Duplicate or invalid id {data.scenePath}";

                    continue;
                }

                dataDictionary[data.uniqueIdentifier] = handle;
                Plugin.idDictionary[data.uniqueIdentifier] = data;
            }
        }

        public IEnumerable<RudeLevelData> GetAllLevelData()
        {
            foreach (var data in dataDictionary.Values)
                yield return data.Result;
        }

        public IEnumerable<string> GetAllScenePaths()
        {
            return GetAllLevelData().Select(data => data.scenePath);
        }

        private async Task UpdateScenesTask(bool forceReload, bool lazyLoad)
        {
            if (!File.Exists(pathToAngryBundle))
            {
                statusText.text = "<color=red>Could not find the file</color>";
                return;
            }

            AngryBundleData fileData = AngryFileUtils.GetAngryBundleData(pathToAngryBundle);
            if (fileData.bundleGuid != bundleData.bundleGuid)
            {
                statusText.text = "<color=red>Target file has a different guid</color>";
                return;
            }
            
            bool inTempScene = false;
            Scene tempScene = new Scene();
            string previousPath = SceneManager.GetActiveScene().path;
            string previousName = SceneManager.GetActiveScene().name;
            if (GetAllScenePaths().Contains(previousPath))
            {
                tempScene = SceneManager.CreateScene("temp");
                inTempScene = true;
                await SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name);
            }

            // Disable all level interfaces
            foreach (KeyValuePair<string, LevelContainer> pair in levels)
                pair.Value.field.forceHidden = true;

            await ReloadBundle(forceReload, lazyLoad);
            
            int currentIndex = 0;
            foreach (RudeLevelData data in GetAllLevelData().OrderBy(d => d.prefferedLevelOrder))
            {
                if (levels.TryGetValue(data.uniqueIdentifier, out LevelContainer container))
                {
                    container.field.siblingIndex = currentIndex++;
                    container.field.forceHidden = false;
                    container.UpdateData(data);
                }
                else
                {
                    LevelContainer levelContainer = new LevelContainer(sceneDiv, this, data);
                    levelContainer.field.siblingIndex = currentIndex++;
                    levelContainer.onLevelButtonPress += () =>
                    {
                        AngrySceneManager.LevelButtonPressed(this, levelContainer, data, data.scenePath);
                    };

                    levels[data.uniqueIdentifier] = levelContainer;
                }
            }

            // Locked levels fix
            foreach (var level in levels.Values)
                level.UpdateUI();

            if (inTempScene)
            {
                if (SceneHelper.Instance != null && SceneHelper.Instance.loadingBlocker != null)
                    SceneHelper.Instance.loadingBlocker.SetActive(true);

                if (GetAllScenePaths().Contains(previousPath))
                {
                    await Addressables.LoadSceneAsync(previousName);
                }
                else
                {
                    await Addressables.LoadSceneAsync("Main Menu");
                }

                if (SceneHelper.Instance != null && SceneHelper.Instance.loadingBlocker != null)
                    SceneHelper.Instance.loadingBlocker.SetActive(false);
            }
            
            // Update online field if there are any
            if (OnlineLevelsManager.onlineLevels.TryGetValue(bundleData.bundleGuid, out OnlineLevelField field))
            {
                if (field.bundleBuildHash == bundleData.buildHash)
                {
                    field.status = OnlineLevelField.OnlineLevelStatus.installed;
                }
                else
                {
                    field.status = OnlineLevelField.OnlineLevelStatus.updateAvailable;
                    if (Plugin.levelUpdateNotifierToggle.value)
                        Plugin.levelUpdateNotifier.hidden = false;
                }

                field.UpdateUI();
            }

            if (!lazyLoad)
                RecalculateFinalRank();
        }

        private Task updateTask = null;
        public bool updating
        {
            get => updateTask != null && !updateTask.IsCompleted;
        }

        /// <summary>
        /// Reloads the angry file and adds the new scenes
        /// </summary>
        /// <param name="forceReload">If set to false, previously unzipped files can be used instead of deleting and re-unzipping</param>
        public Task UpdateScenes(bool forceReload, bool lazyLoad)
        {
            if (updating)
                return updateTask;

			loadingCircle.hidden = false;
			sceneDiv.hidden = true;
			sceneDiv.interactable = false;
			statusText.hidden = true;
			statusText.text = "";

			updateTask = UpdateScenesTask(forceReload, lazyLoad).ContinueWith((task) => {
				loadingCircle.hidden = true;
				sceneDiv.hidden = false;
				sceneDiv.interactable = true;
			}, TaskScheduler.FromCurrentSynchronizationContext());

            return updateTask;
		}

        // Faster ordering since not all fields are moved, only this one
        public void UpdateOrder()
        {
            int order = 0;
            AngryBundleContainer[] allBundles = Plugin.angryBundles.Values.OrderBy(b => b.rootPanel.siblingIndex).ToArray();

            if (Plugin.bundleSortingMode.value == Plugin.BundleSorting.Alphabetically)
            {
                while (order < allBundles.Length)
                {
                    if (order == rootPanel.siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (string.Compare(bundleData.bundleName, allBundles[order].bundleData.bundleName) == -1)
                        break;

                    order += 1;
                }
            }
            else if (Plugin.bundleSortingMode.value == Plugin.BundleSorting.Author)
            {
                while (order < allBundles.Length)
                {
                    if (order == rootPanel.siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (string.Compare(bundleData.bundleAuthor, allBundles[order].bundleData.bundleAuthor) == -1)
                        break;

                    order += 1;
                }
            }
            else if (Plugin.bundleSortingMode.value == Plugin.BundleSorting.LastPlayed)
            {
                if (!Plugin.lastPlayed.TryGetValue(bundleData.bundleGuid, out long lastTime))
                    lastTime = 0;

                while (order < allBundles.Length)
                {
                    if (order == rootPanel.siblingIndex)
                    {
                        order += 1;
                        continue;
                    }

                    if (!Plugin.lastPlayed.TryGetValue(allBundles[order].bundleData.bundleGuid, out long otherPlayime))
                        otherPlayime = 0;

                    if (lastTime > otherPlayime)
                        break;

                    order += 1;
                }
            }
			else if (Plugin.bundleSortingMode.value == Plugin.BundleSorting.LastUpdate)
			{
				if (!Plugin.lastUpdate.TryGetValue(bundleData.bundleGuid, out long lastUpdate))
					lastUpdate = 0;

				while (order < allBundles.Length)
				{
					if (order == rootPanel.siblingIndex)
					{
						order += 1;
						continue;
					}

					if (!Plugin.lastUpdate.TryGetValue(allBundles[order].bundleData.bundleGuid, out long otherLastUpdate))
						otherLastUpdate = 0;

					if (lastUpdate > otherLastUpdate)
						break;

					order += 1;
				}
			}

			if (order < 0)
                order = 0;
            else if (order >= allBundles.Length)
                order = allBundles.Length - 1;

            rootPanel.siblingIndex = order;
        }

        public void RecalculateFinalRank()
        {
            int totalRankScore = 0;
            int currentRankScore = 0;

            foreach (var data in GetAllLevelData())
            {
                if (data.isSecretLevel)
                    continue;

                totalRankScore += 6;

                if (levels.TryGetValue(data.uniqueIdentifier, out LevelContainer container))
                {
                    currentRankScore += Math.Max(0, RankUtils.GetRankScore(container.finalRank.value[0]));
                }
            }

            finalRankScore.value = totalRankScore <= 0 ? 0 : (int)(((float)currentRankScore / totalRankScore) * 6f);
            UpdateFinalRankUI();
        }

        public void UpdateFinalRankUI()
        {
            char finalRank = RankUtils.GetRankChar(finalRankScore.value);
            rootPanel.rankText = finalRank.ToString();
            rootPanel.rankTextColor = RankUtils.GetRankColor(finalRank, Color.white);

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
        }

        internal async Task DeleteBundle()
        {
            if (File.Exists(pathToAngryBundle))
                File.Delete(pathToAngryBundle);

            if (Directory.Exists(pathToTempFolder))
                Directory.Delete(pathToTempFolder, true);

            if (OnlineLevelsManager.onlineLevels.TryGetValue(bundleData.bundleGuid, out var onlineField))
            {
                onlineField.status = OnlineLevelField.OnlineLevelStatus.notInstalled;
                onlineField.UpdateUI();
            }

            pathToAngryBundle = "";
            pathToTempFolder = "";
            rootPanel.hidden = true;

            await Unload();
		}

        public void OpenDeletePanel()
        {
            NotificationPanel.Open(new DeleteBundleNotification(this));
        }

        private bool fileChangeDetected = false;
        public void FileChanged()
        {
            if (AngryFileUtils.TryGetAngryBundleData(pathToAngryBundle, out AngryBundleData updatedData, out Exception e))
            {
                // Different guid, would break the container
                if (updatedData.bundleGuid != bundleData.bundleGuid)
                {
                    Plugin.logger.LogError($"File {Path.GetFileName(pathToAngryBundle)} was changed, but the new file's guid does not match its container! Unlinking");
                    fileChangeDetected = false;
                    pathToAngryBundle = "";
                    return;
				}

                fileChangeDetected = updatedData.buildHash != bundleData.buildHash;
                if (fileChangeDetected)
                    Plugin.UpdateLastUpdate(this);

                CheckReloadPrompt();
			}
        }

        public void CheckReloadPrompt()
        {
			if (AngrySceneManager.isInCustomLevel && AngrySceneManager.currentBundleContainer == this)
			{
				if (Plugin.currentPanel != null)
				{
					if (fileChangeDetected)
					{
						Plugin.currentPanel.reloadBundlePrompt.gameObject.SetActive(true);
						Plugin.currentPanel.reloadBundlePrompt.audio.Play();
						Plugin.currentPanel.reloadBundlePrompt.text.text = $"File update detected\nPress <color=orange>{Plugin.reloadFileKeybind.value}</color> to reload\n(Can be binded in the settings)";
						Plugin.currentPanel.reloadBundlePrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
						Plugin.currentPanel.reloadBundlePrompt.reloadButton.onClick.AddListener(() =>
						{
                            fileChangeDetected = false;
							UpdateScenes(false, false);
						});

                        Plugin.currentPanel.reloadBundlePrompt.ignoreButton.onClick = new Button.ButtonClickedEvent();
						Plugin.currentPanel.reloadBundlePrompt.ignoreButton.onClick.AddListener(() =>
                        {
							Plugin.currentPanel.reloadBundlePrompt.reloadButton.onClick = new Button.ButtonClickedEvent();
							Plugin.currentPanel.reloadBundlePrompt.gameObject.SetActive(false);

							fileChangeDetected = false;
                        });
					}
					else
					{
						Plugin.currentPanel.reloadBundlePrompt.gameObject.SetActive(false);
					}
				}
			}
		}

        private bool _loadedAfterPanelOpen = false;
        public AngryBundleContainer(string path, AngryBundleData data)
        {
            Plugin.logger.LogInfo($"Creating bundle container for {path}");
            pathToAngryBundle = path;
            bundleData = data;

            rootPanel = new ConfigPanelForBundles(this, Plugin.bundleDivision, data.bundleName, data.bundleGuid);
            rootPanel.onPannelOpenEvent += (external) =>
            {
                if (locator == null && !_loadedAfterPanelOpen)
                {
                    _loadedAfterPanelOpen = true;

                    if (updating)
                    {
                        updateTask.ContinueWith((task) =>
                        {
                            UpdateScenes(false, false);
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    else
                    {
                        UpdateScenes(false, false);
                    }
                }
            };

            finalRankScore = new IntField(rootPanel, "final bundle rank", rootPanel.guid + "_finalRankCache", -1, true, false);
            finalRankScore.postValueChangeEvent += (val) =>
            {
                if (val < 0)
                {
                    if (locator == null)
                        UpdateScenes(false, false);
                    else
                        RecalculateFinalRank();
                }
                else
                {
                    UpdateFinalRankUI();
                }
            };
            // To prevent force load on preset reset
            finalRankScore.defaultValue = 0;

            ButtonArrayField reloadButtons = new ButtonArrayField(rootPanel, rootPanel.guid + "_reloadButtons", 2, new float[2] { 0.5f, 0.5f }, new string[] { "Reload File", "Force Reload File" });
            reloadButtons.OnClickEventHandler(0).onClick += () => UpdateScenes(false, false);
            reloadButtons.OnClickEventHandler(1).onClick += () => UpdateScenes(true, false);

            new SpaceField(rootPanel, 5);

            new ConfigHeader(rootPanel, "Levels");
            statusText = new ConfigHeader(rootPanel, "", 16, TextAnchor.MiddleLeft);
            statusText.hidden = true;
            loadingCircle = new LoadingCircleField(rootPanel);
            loadingCircle.hidden = true;
            sceneDiv = new ConfigDivision(rootPanel, "sceneDiv_" + rootPanel.guid);
            
            UpdateFinalRankUI();
        }
    }
}
