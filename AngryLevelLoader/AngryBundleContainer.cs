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

namespace AngryLevelLoader
{
	public class AngryBundleContainer
	{
		public List<AssetBundle> bundles = new List<AssetBundle>();
		public string pathToAngryBundle;

		public static string lastLoadedScenePath = "";

		public ConfigPanel rootPanel;
		public ButtonField reloadButton;
		public ConfigHeader statusText;
		public ConfigDivision sceneDiv;
		public Dictionary<string, LevelContainer> levels = new Dictionary<string, LevelContainer>();

		public static PropertyInfo p_SceneHelper_CurrentScene = typeof(SceneHelper).GetProperty("CurrentScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
		public static PropertyInfo p_SceneHelper_LastScene = typeof(SceneHelper).GetProperty("LastScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

		public IEnumerable<string> GetAllScenePaths()
		{
			foreach (AssetBundle bundle in bundles)
			{
				foreach (string path in bundle.GetAllScenePaths())
					yield return path;
			}
		}

		public IEnumerable<RudeLevelData> GetAllLevelData()
		{
			foreach (AssetBundle bundle in bundles)
			{
				if (bundle.GetAllScenePaths().Length != 0)
					continue;

				foreach (RudeLevelData data in bundle.LoadAllAssets<RudeLevelData>())
					yield return data;
			}
		}

		private void LoadBundle(string path)
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
						bundles.Add(bundle);
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

		public static void LoadLevel(string path)
		{
			SceneManager.LoadScene(path, LoadSceneMode.Single);
			MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", Plugin.selectedDifficulty);
			p_SceneHelper_LastScene.SetValue(null, p_SceneHelper_CurrentScene.GetValue(null) as string);
			p_SceneHelper_CurrentScene.SetValue(null, path);
		}

		public void UpdateScenes()
		{
			sceneDiv.interactable = false;
			sceneDiv.hidden = false;
			statusText.hidden = true;
			statusText.text = "";

			foreach (RudeLevelData data in GetAllLevelData())
				Plugin.currentDatas.Remove(data);

			foreach (AssetBundle bundle in bundles)
			{
				try
				{
					bundle.Unload(false);
				}
				catch (Exception) { }
			}
			bundles.Clear();

			if (!File.Exists(pathToAngryBundle))
			{
				statusText.text = "Could not find the file";
				sceneDiv.hidden = true;
				return;
			}

			LoadBundle(pathToAngryBundle);

			// Disable all level interfaces
			foreach (KeyValuePair<string, LevelContainer> pair in levels)
				pair.Value.field.forceHidden = true;

			foreach (RudeLevelData data in GetAllLevelData().OrderBy(d => d.prefferedLevelOrder))
			{
				if (string.IsNullOrEmpty(data.uniqueIdentifier) || Plugin.currentDatas.FirstOrDefault(otherData => otherData.uniqueIdentifier == data.uniqueIdentifier) != null)
				{
					Debug.LogWarning($"Duplicate or invalid unique id {data.scenePath}");
					statusText.hidden = false;
					if (!string.IsNullOrEmpty(statusText.text))
						statusText.text += '\n';
					statusText.text += $"<color=red>Error: </color>Duplicate or invalid id {data.scenePath}";
					continue;
				}

				Plugin.currentDatas.Add(data);

				if (levels.TryGetValue(data.uniqueIdentifier, out LevelContainer container))
				{
					container.field.forceHidden = false;
					container.UpdateData(data);
				}
				else
				{
					LevelContainer levelContainer = new LevelContainer(sceneDiv, data);
					levelContainer.onLevelButtonPress += () => LoadLevel(data.scenePath);

					SceneManager.sceneLoaded += (scene, mode) =>
					{
						if (levelContainer.hidden)
							return;

						if (scene.path == data.scenePath)
						{
							Plugin.ReplaceShaders();
							Plugin.LinkMixers();
							lastLoadedScenePath = data.scenePath;

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
			reloadButton.onClick += UpdateScenes;

			new SpaceField(rootPanel, 5);

			new ConfigHeader(rootPanel, "Levels");
			statusText = new ConfigHeader(rootPanel, "", 16, TextAnchor.MiddleLeft);
			statusText.hidden = true;
			sceneDiv = new ConfigDivision(rootPanel, "sceneDiv_" + rootPanel.guid);
		}
	}
}
