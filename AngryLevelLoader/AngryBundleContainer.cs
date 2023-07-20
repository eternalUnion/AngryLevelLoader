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
		public ConfigHeader statusText;
		public ConfigDivision sceneDiv;
		public Dictionary<string, LevelContainer> levels = new Dictionary<string, LevelContainer>();

		public static PropertyInfo p_SceneHelper_CurrentScene = typeof(SceneHelper).GetProperty("CurrentScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
		public static PropertyInfo p_SceneHelper_LastScene = typeof(SceneHelper).GetProperty("LastScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

		/// <summary>
		/// Read .angry file and load the levels in memory
		/// </summary>
		/// <param name="forceReload">If set to false and a previously unzipped version exists, do not re-unzip the file</param>
		private void ReloadBundle(bool forceReload)
		{
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
				Addressables.ClearResourceLocators();
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
			locator = Addressables.LoadContentCatalogAsync(Path.Combine(pathToTempFolder, "catalog.json")).WaitForCompletion();

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
			return dataDictionary.Values.Select(data => data.WaitForCompletion());
		}

		public IEnumerable<string> GetAllScenePaths()
		{
			return dataDictionary.Values.Select(data => data.WaitForCompletion().scenePath);
		}

		/// <summary>
		/// Reloads the angry file and adds the new scenes
		/// </summary>
		/// <param name="forceReload">If set to false, previously unzipped files can be used instead of deleting and re-unzipping</param>
		public void UpdateScenes(bool forceReload)
		{
			if (!File.Exists(pathToAngryBundle))
			{
				statusText.text = "Could not find the file";
				sceneDiv.hidden = true;
				return;
			}

			ReloadBundle(forceReload);
			sceneDiv.interactable = false;
			sceneDiv.hidden = false;
			statusText.hidden = true;
			statusText.text = "";

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
					levelContainer.onLevelButtonPress += () => AngrySceneManager.LoadLevel(data.scenePath, pathToTempFolder);

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
			reloadButton.onClick += () => UpdateScenes(true);

			new SpaceField(rootPanel, 5);

			new ConfigHeader(rootPanel, "Levels");
			statusText = new ConfigHeader(rootPanel, "", 16, TextAnchor.MiddleLeft);
			statusText.hidden = true;
			sceneDiv = new ConfigDivision(rootPanel, "sceneDiv_" + rootPanel.guid);
		}
	}
}
