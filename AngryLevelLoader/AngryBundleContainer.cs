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

namespace AngryLevelLoader
{
	public class BundleData
	{
		public string bundleGuid { get; set; }
		public string buildHash { get; set; }
		public List<string> levelDataPaths;
	}

	public class AngryBundleContainer
	{
		public IResourceLocator locator;
		public string pathToTempFolder;
		public string pathToAngryBundle;
		public List<string> dataPaths = new List<string>();
		public Dictionary<string, AsyncOperationHandle<RudeLevelData>> dataDictionary = new Dictionary<string, AsyncOperationHandle<RudeLevelData>>();

		public ConfigPanel rootPanel;
		public ButtonField reloadButton;
		public ConfigHeader bundleReloadBlockText;
		public ConfigHeader statusText;
		public ConfigDivision sceneDiv;
		public Dictionary<string, LevelContainer> levels = new Dictionary<string, LevelContainer>();

		public static PropertyInfo p_SceneHelper_CurrentScene = typeof(SceneHelper).GetProperty("CurrentScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
		public static PropertyInfo p_SceneHelper_LastScene = typeof(SceneHelper).GetProperty("LastScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

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

		// LEGACY
		private List<AssetBundle> allBundles = new List<AssetBundle>();
		public bool legacy = false;

		/// <summary>
		/// Read .angry file and load the levels in memory
		/// </summary>
		/// <param name="forceReload">If set to false and a previously unzipped version exists, do not re-unzip the file</param>
		private void ReloadBundle(bool forceReload)
		{
			legacy = false;

			// Release all data assets
			foreach (AsyncOperationHandle<RudeLevelData> handle in dataDictionary.Values)
			{
				Plugin.idDictionary.Remove(handle.WaitForCompletion().uniqueIdentifier);
				Addressables.Release(handle);
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
				return;
			}

			// Open the angry zip archive
			using(ZipArchive zip = new ZipArchive(File.Open(pathToAngryBundle, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read))
			{
				var dataEntry = zip.GetEntry("data.json");
				bool unzip = true;

				using (TextReader dataReader = new StreamReader(dataEntry.Open()))
				{
					BundleData newData = JsonConvert.DeserializeObject<BundleData>(dataReader.ReadToEnd());
					pathToTempFolder = Path.Combine(Plugin.tempFolderPath, newData.bundleGuid);
					dataPaths = newData.levelDataPaths;
					
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
				}
			}

			// Load the catalog
			locator = Addressables.LoadContentCatalogAsync(Path.Combine(pathToTempFolder, "catalog.json"), true).WaitForCompletion();

			// Load the level data
			statusText.text = "";
			statusText.hidden = true;
			foreach (string path in dataPaths)
			{
				var handle = Addressables.LoadAssetAsync<RudeLevelData>(path);
				RudeLevelData data = handle.WaitForCompletion();

				if (handle.Status != AsyncOperationStatus.Succeeded)
					continue;

				if (Plugin.idDictionary.ContainsKey(data.uniqueIdentifier))
				{
					Debug.LogWarning($"Duplicate or invalid unique id {data.scenePath}");
					statusText.hidden = false;
					if (!string.IsNullOrEmpty(statusText.text))
						statusText.text += '\n';
					statusText.text += $"<color=red>Error: </color>Duplicate or invalid id {data.scenePath}";

					Addressables.Release(handle);
					continue;
				}

				dataDictionary[path] = handle;
				Plugin.idDictionary[data.uniqueIdentifier] = data;
			}
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

			foreach (var data in dataDictionary.Values.Select(data => data.WaitForCompletion()))
				yield return data;
		}

		public IEnumerable<string> GetAllScenePaths()
		{
			return GetAllLevelData().Select(data => data.scenePath);
		}

		/// <summary>
		/// Reloads the angry file and adds the new scenes
		/// </summary>
		/// <param name="forceReload">If set to false, previously unzipped files can be used instead of deleting and re-unzipping</param>
		public void UpdateScenes(bool forceReload)
		{
			if (!File.Exists(pathToAngryBundle))
			{
				statusText.text = "<color=red>Could not find the file</color>";
				return;
			}

			sceneDiv.interactable = false;
			sceneDiv.hidden = false;
			statusText.hidden = true;
			statusText.text = "";
			ReloadBundle(forceReload);

			// Disable all level interfaces
			foreach (KeyValuePair<string, LevelContainer> pair in levels)
				pair.Value.field.forceHidden = true;

			foreach (RudeLevelData data in GetAllLevelData().OrderBy(d => d.prefferedLevelOrder))
			{
				if (levels.TryGetValue(data.uniqueIdentifier, out LevelContainer container))
				{
					container.field.forceHidden = false;
					container.UpdateData(data);
				}
				else
				{
					LevelContainer levelContainer = new LevelContainer(sceneDiv, data);
					levelContainer.onLevelButtonPress += () =>
					{
						// LEGACY
						if (legacy)
						{
							AngrySceneManager.LoadLegacyLevel(data.scenePath);
						}
						else
						{
							AngrySceneManager.LoadLevel(data.scenePath, pathToTempFolder);
						}
					};

					SceneManager.sceneLoaded += (scene, mode) =>
					{
						if (levelContainer.hidden)
							return;

						if (scene.path == data.scenePath)
						{
							levelContainer.AssureSecretsSize();

							string secretString = levelContainer.secrets.value;
							foreach (Bonus bonus in Resources.FindObjectsOfTypeAll<Bonus>())
							{
								if (bonus.gameObject.scene.path != data.scenePath)
									continue;

								if (bonus.secretNumber >= 0 && bonus.secretNumber < secretString.Length && secretString[bonus.secretNumber] == 'T')
								{
									bonus.beenFound = true;
									bonus.BeenFound();
								}
							}
						}
					};

					levels[data.uniqueIdentifier] = levelContainer;
				}
			}

			sceneDiv.interactable = true;
		}
		
		public AngryBundleContainer(string path)
		{
			this.pathToAngryBundle = path;

			rootPanel = new ConfigPanel(Plugin.config.rootPanel, Path.GetFileNameWithoutExtension(path), Path.GetFileName(path));

			reloadButton = new ButtonField(rootPanel, "Reload File", "reloadButton");
			reloadButton.onClick += () => UpdateScenes(false);
			bundleReloadBlockText = new ConfigHeader(rootPanel, "Bundle cannot be reloaded while in a scene");
			bundleReloadBlockText.hidden = true;

			new SpaceField(rootPanel, 5);

			new ConfigHeader(rootPanel, "Levels");
			statusText = new ConfigHeader(rootPanel, "", 16, TextAnchor.MiddleLeft);
			statusText.hidden = true;
			sceneDiv = new ConfigDivision(rootPanel, "sceneDiv_" + rootPanel.guid);

			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (GetAllScenePaths().Contains(scene.path))
				{
					reloadButton.interactable = false;
					bundleReloadBlockText.hidden = false;
				}
				else
				{
					reloadButton.interactable = true;
					bundleReloadBlockText.hidden = true;
				}
			};
		}
	}
}
