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

namespace AngryLevelLoader
{
	public class BundleData
	{
		public string bundleGuid { get; set; }
		public string buildHash { get; set; }
		public string bundleDataPath { get; set; }
		public List<string> levelDataPaths;
	}

	public class AngryBundleContainer
	{
		private class BundleManager : MonoBehaviour
		{
			private static BundleManager _instance;
			public static BundleManager instance
			{
				get
				{
					if (_instance == null)
					{
						_instance = new GameObject().AddComponent<BundleManager>();
						UnityEngine.Object.DontDestroyOnLoad(_instance);
					}

					return _instance;
				}
			}
		}

		public IResourceLocator locator;
		public string pathToTempFolder;
		public string pathToAngryBundle;
		public string hash;
		public string guid;
		public string name;
		public string author;
		public List<string> dataPaths = new List<string>();
		public Dictionary<string, AsyncOperationHandle<RudeLevelData>> dataDictionary = new Dictionary<string, AsyncOperationHandle<RudeLevelData>>();

		public ConfigPanel rootPanel;
		public ButtonField reloadButton;
		public ConfigHeader statusText;
		public ConfigDivision sceneDiv;
		public Dictionary<string, LevelContainer> levels = new Dictionary<string, LevelContainer>();

		// LEGACY
		private List<AssetBundle> allBundles = new List<AssetBundle>();
		public bool legacy = false;

		// LEGACY
		private bool CheckForLegacyFile()
		{
			using (FileStream fs = File.Open(pathToAngryBundle, FileMode.Open, FileAccess.Read))
			{
				try
				{
					BinaryReader reader = new BinaryReader(fs);
					fs.Seek(0, SeekOrigin.Begin);
					int bundleCount = reader.ReadInt32();
					if (bundleCount * 4 + 4 >= fs.Length)
						return false;
					int totalSize = 4 + bundleCount * 4;
					for (int i = 0; i < bundleCount && totalSize < fs.Length; i++)
						totalSize += reader.ReadInt32();

					if (totalSize == fs.Length)
					{
						return true;
					}
				}
				catch (Exception)
				{
					return false;
				}
			}

			return false;
		}

		// LEGACY
		private void LoadLegacy(string path)
		{
			using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read))
			using (BinaryReader br = new BinaryReader(fs))
			{
				int bundleCount = br.ReadInt32();
				int currentOffset = 0;

				for (int i = 0; i < bundleCount; i++)
				{
					fs.Seek(4 + i * 4, SeekOrigin.Begin);
					int bundleLen = br.ReadInt32();

					byte[] bundleData = new byte[bundleLen];
					fs.Seek(4 + bundleCount * 4 + currentOffset, SeekOrigin.Begin);
					fs.Read(bundleData, 0, bundleLen);
					AssetBundle bundle = AssetBundle.LoadFromMemory(bundleData);
					if (bundle != null)
						allBundles.Add(bundle);
					else
					{
						statusText.hidden = false;
						if (!string.IsNullOrEmpty(statusText.text))
							statusText.text += '\n';
						statusText.text += "<color=red>Error: </color>Could not load some of the bundles. Possible confliction with another angry file.";
					}

					currentOffset += bundleLen;
				}
			}
		}

		/// <summary>
		/// Read .angry file and load the levels in memory
		/// </summary>
		/// <param name="forceReload">If set to false and a previously unzipped version exists, do not re-unzip the file</param>
		/// <returns>Success</returns>
		private bool ReloadBundle(bool forceReload)
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
				Addressables.CleanBundleCache(new string[] { locator.LocatorId });
			}

			// LEGACY
			foreach (AssetBundle bundle in allBundles)
			{
				try
				{
					bundle.Unload(false);
				}
				catch (Exception) { }
			}
			allBundles.Clear();

			// LEGACY
			if (CheckForLegacyFile())
			{
				legacy = true;
				statusText.hidden = false;
				if (!string.IsNullOrEmpty(statusText.text))
					statusText.text += '\n';
				statusText.text += "<color=yellow>Warning: Legacy angry file detected! Support for this format will be dropped on future updates!</color>";

				LoadLegacy(pathToAngryBundle);
				return true;
			}

			// Open the angry zip archive
			string bundleDataAddress = "";
			using(ZipArchive zip = new ZipArchive(File.Open(pathToAngryBundle, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read))
			{
				var dataEntry = zip.GetEntry("data.json");
				bool unzip = true;

				if (dataEntry == null)
					return false;

				using (TextReader dataReader = new StreamReader(dataEntry.Open()))
				{
					BundleData newData = JsonConvert.DeserializeObject<BundleData>(dataReader.ReadToEnd());

					hash = newData.buildHash;
					guid = newData.bundleGuid;

					pathToTempFolder = Path.Combine(Plugin.tempFolderPath, newData.bundleGuid);
					dataPaths = newData.levelDataPaths;
					bundleDataAddress = newData.bundleDataPath;

					// If force reload is set to false, check if the build hashes match
					// between unzipped bundle and the current angry file.
					// Build hash is generated randomly every build so avoiding unzipping
					if (!forceReload && Directory.Exists(pathToTempFolder) && File.Exists(Path.Combine(pathToTempFolder, "data.json")) && File.Exists(Path.Combine(pathToTempFolder, "catalog.json")))
					{
						BundleData previousData = JsonConvert.DeserializeObject<BundleData>(File.ReadAllText(Path.Combine(pathToTempFolder, "data.json")));
						if (previousData.buildHash == newData.buildHash)
						{
							unzip = false;
						}
					}
				}

				if (unzip)
				{
					if (Directory.Exists(pathToTempFolder))
						Directory.Delete(pathToTempFolder, true);
					Directory.CreateDirectory(pathToTempFolder);
					zip.ExtractToDirectory(pathToTempFolder);

					rootPanel.SetIconWithURL(Path.Combine(pathToTempFolder, "icon.png"));
				}
				else
				{
					if (rootPanel.icon == null)
						rootPanel.SetIconWithURL(Path.Combine(pathToTempFolder, "icon.png"));
				}
			}

			// Load the catalog
			locator = Addressables.LoadContentCatalogAsync(Path.Combine(pathToTempFolder, "catalog.json"), true).WaitForCompletion();

			// Load the bundle name
			if (bundleDataAddress != null && !string.IsNullOrEmpty(bundleDataAddress))
			{
				AsyncOperationHandle<RudeBundleData> bundleHandle = Addressables.LoadAssetAsync<RudeBundleData>(bundleDataAddress);
				bundleHandle.WaitForCompletion();
				RudeBundleData bundleData = bundleHandle.Result;

				if (bundleData != null)
				{
					rootPanel.displayName = name = bundleData.bundleName;
					if (!string.IsNullOrEmpty(bundleData.author))
					{
						rootPanel.displayName += $"\n<color=#909090>by {bundleData.author}</color>";
						author = bundleData.author;
					}

					rootPanel.headerText = $"--{bundleData.bundleName}--";
				}

				bundleData = null;
				Addressables.Release(bundleHandle);
			}

			// Load the level data
			statusText.text = "";
			statusText.hidden = true;
			foreach (string path in dataPaths)
			{
				AsyncOperationHandle<RudeLevelData> handle = Addressables.LoadAssetAsync<RudeLevelData>(path);
				handle.WaitForCompletion();
				RudeLevelData data = handle.Result;

				if (data == null)
					continue;

				if (Plugin.idDictionary.ContainsKey(data.uniqueIdentifier))
				{
					Debug.LogWarning($"Duplicate or invalid unique id {data.scenePath}");
					statusText.hidden = false;
					if (!string.IsNullOrEmpty(statusText.text))
						statusText.text += '\n';
					statusText.text += $"<color=red>Error: </color>Duplicate or invalid id {data.scenePath}";

					continue;
				}

				dataDictionary[data.uniqueIdentifier] = handle;
				Plugin.idDictionary[data.uniqueIdentifier] = data;
			}

			return true;
		}

		public IEnumerable<RudeLevelData> GetAllLevelData()
		{
			// LEGACY
			if (legacy)
			{
				foreach (var dataArr in allBundles.Where(bundle => !bundle.isStreamedSceneAssetBundle).Select(bundle => bundle.LoadAllAssets<RudeLevelData>()))
					foreach (var data in dataArr)
						yield return data;

				yield break;
			}

			foreach (var data in dataDictionary.Values)
				yield return data.Result;
		}

		public IEnumerable<string> GetAllScenePaths()
		{
			return GetAllLevelData().Select(data => data.scenePath);
		}

		private bool updating = false;
		private IEnumerator UpdateScenesInternal(bool forceReload)
		{
			updating = true;
			sceneDiv.interactable = false;
			sceneDiv.hidden = true;

			bool inTempScene = false;
			Scene tempScene = new Scene();
			string previousPath = SceneManager.GetActiveScene().path;
			string previousName = SceneManager.GetActiveScene().name;
			if (GetAllScenePaths().Contains(previousPath) && !legacy)
			{
				tempScene = SceneManager.CreateScene("temp");
				inTempScene = true;
				yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name);
			}

			Exception e = null;
			try
			{
				if (!File.Exists(pathToAngryBundle))
				{
					statusText.text = "<color=red>Could not find the file</color>";
				}
				else
				{
					// Disable all level interfaces
					foreach (KeyValuePair<string, LevelContainer> pair in levels)
						pair.Value.field.forceHidden = true;

					sceneDiv.interactable = false;
					sceneDiv.hidden = true;
					statusText.hidden = true;
					statusText.text = "";
					ReloadBundle(forceReload);

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
							LevelContainer levelContainer = new LevelContainer(sceneDiv, data);
							levelContainer.field.siblingIndex = currentIndex++;
							levelContainer.onLevelButtonPress += () =>
							{
								Plugin.currentBundleContainer = this;
								Plugin.currentLevelContainer = levelContainer;
								Plugin.currentLevelData = data;

								// LEGACY
								if (legacy)
								{
									AngrySceneManager.LoadLegacyLevel(data.scenePath);
								}
								else
								{
									// AngrySceneManager.LoadLevel(this, levelContainer, data, data.scenePath);
									AngrySceneManager.LevelButtonPressed(this, levelContainer, data, data.scenePath);
								}
							};

							levels[data.uniqueIdentifier] = levelContainer;
						}
					}
				}
			}
			catch (Exception err)
			{
				e = err;
			}

			if (inTempScene)
			{
				if (GetAllScenePaths().Contains(previousPath))
				{
					if (legacy)
						yield return SceneManager.LoadSceneAsync(previousName);
					else
					{
						yield return Addressables.LoadSceneAsync(previousName);
						AngrySceneManager.PostSceneLoad();
					}
				}
				else
					yield return Addressables.LoadSceneAsync("Main Menu");
			}

			updating = false;

			if (e != null)
			{
				Debug.LogError($"Error while loading bundle {e}\n{e.StackTrace}");
			}

			sceneDiv.interactable = true;
			sceneDiv.hidden = false;

			// Update online field if there are any
			if (author != Plugin.levelUpdateAuthorIgnore.value && OnlineLevelsManager.onlineLevels.TryGetValue(guid, out OnlineLevelField field))
			{
				if (field.bundleBuildHash == hash)
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

			UpdateOrder();
		}

		// Faster ordering since not all fields are moved, only this one
		private void UpdateOrder()
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

					if (string.Compare(name, allBundles[order].name) == -1)
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

					if (string.Compare(author, allBundles[order].author) == -1)
						break;

					order += 1;
				}
			}
			else if (Plugin.bundleSortingMode.value == Plugin.BundleSorting.LastPlayed)
			{
				long lastTime = 0;
				Plugin.lastPlayed.TryGetValue(guid, out lastTime);

				while (order < allBundles.Length)
				{
					if (order == rootPanel.siblingIndex)
					{
						order += 1;
						continue;
					}

					long otherPlayime = 0;
					Plugin.lastPlayed.TryGetValue(allBundles[order].guid, out otherPlayime);

					if (lastTime > otherPlayime)
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

		/// <summary>
		/// Reloads the angry file and adds the new scenes
		/// </summary>
		/// <param name="forceReload">If set to false, previously unzipped files can be used instead of deleting and re-unzipping</param>
		public void UpdateScenes(bool forceReload)
		{
			if (updating)
				return;
			BundleManager.instance.StartCoroutine(UpdateScenesInternal(forceReload));
		}
		
		public AngryBundleContainer(string path)
		{
			Debug.Log($"Creating bundle container for {path}");
			this.pathToAngryBundle = path;

			rootPanel = new ConfigPanel(Plugin.bundleDivision, Path.GetFileNameWithoutExtension(path), Path.GetFileName(path), ConfigPanel.PanelFieldType.StandardWithBigIcon);
			guid = "";
			hash = "";
			name = rootPanel.displayName;
			author = "";

			reloadButton = new ButtonField(rootPanel, "Reload File", "reloadButton");
			reloadButton.onClick += () => UpdateScenes(false);
			
			new SpaceField(rootPanel, 5);

			new ConfigHeader(rootPanel, "Levels");
			statusText = new ConfigHeader(rootPanel, "", 16, TextAnchor.MiddleLeft);
			statusText.hidden = true;
			sceneDiv = new ConfigDivision(rootPanel, "sceneDiv_" + rootPanel.guid);
		}
	}
}
