using Newtonsoft.Json;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public class LevelInfo
	{
		public string Name { get; set; }
		public string Author { get; set; }
		public string Guid { get; set; }
		public int Size { get; set; }
		public string Hash { get; set; }
		public string ThumbnailHash { get; set; }
	}

	public class LevelCatalog
	{
		public List<LevelInfo> Levels;
	}

	public class LoadingCircleField : CustomConfigField
	{
		public static Sprite loadingIcon;
		private static bool init = false;
		public static void Init()
		{
			if (init)
				return;
			init = true;

			UnityWebRequest spriteReq = UnityWebRequestTexture.GetTexture(Path.Combine(Plugin.workingDir, "loading-icon.png"));
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

	public class OnlineLevelsManager : MonoBehaviour
	{
		public static OnlineLevelsManager instance;
		public static ConfigPanel onlineLevelsPanel;
		public static ConfigDivision onlineLevelContainer;
		public static LoadingCircleField loadingCircle;

		// Filters
		public static BoolField showInstalledLevels;
		public static BoolField showUpdateAvailableLevels;
		public static BoolField showNotInstalledLevels;

		public static void Init()
		{
			if (instance == null)
			{
				instance = new GameObject().AddComponent<OnlineLevelsManager>();
				UnityEngine.Object.DontDestroyOnLoad(instance);
			}

			var filterPanel = new ConfigPanel(onlineLevelsPanel, "Filters", "online_filters");
			filterPanel.hidden = true;

			new ConfigHeader(filterPanel, "State Filters");
			showInstalledLevels = new BoolField(filterPanel, "Installed", "online_installedLevels", true);
			showNotInstalledLevels = new BoolField(filterPanel, "Not installed", "online_notInstalledLevels", true);
			showUpdateAvailableLevels = new BoolField(filterPanel, "Update available", "online_updateAvailableLevels", true);

			var toolbar = new ButtonArrayField(onlineLevelsPanel, "online_toolbar", 2, new float[] { 0.5f, 0.5f }, new string[] { "Refresh", "Filters" });
			toolbar.OnClickEventHandler(0).onClick += RefreshAsync;
			toolbar.OnClickEventHandler(1).onClick += () => filterPanel.OpenPanel();

			loadingCircle = new LoadingCircleField(onlineLevelsPanel);
			loadingCircle.hidden = true;
			onlineLevelContainer = new ConfigDivision(onlineLevelsPanel, "p_onlineLevelsDiv");

			LoadThumbnailHashes();
		}

		private static bool downloadingCatalog = false;
		public static LevelCatalog catalog;
		public static Dictionary<string, OnlineLevelField> onlineLevels = new Dictionary<string, OnlineLevelField>();
		public static Dictionary<string, string> thumbnailHashes = new Dictionary<string, string>();

		public static void LoadThumbnailHashes()
		{
			thumbnailHashes.Clear();

			string thumbnailHashLocation = Path.Combine(Plugin.workingDir, "OnlineCache", "thumbnailCacheHashes.txt");
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
			string thumbnailHashDir = Path.Combine(Plugin.workingDir, "OnlineCache");
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
			catalog = null;
			downloadingCatalog = true;

			string currentVersionPath = Path.Combine(Plugin.workingDir, "OnlineCache", "LevelCatalogHash.txt");
			string newCatalogVersion = "";
			if (File.Exists(currentVersionPath))
			{
				string currentVersion = File.ReadAllText(currentVersionPath);

				try
				{
					UnityWebRequest catalogVersionRequest = new UnityWebRequest("https://raw.githubusercontent.com/eternalUnion/AngryLevels/release/LevelCatalogHash.txt");
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
						newCatalogVersion = catalogVersionRequest.downloadHandler.text;
					}
				}
				finally
				{
					downloadingCatalog = false;
				}
				
				if (newCatalogVersion == currentVersion)
				{
					string cachedCatalogPath = Path.Combine(Plugin.workingDir, "OnlineCache", "LevelCatalog.json");
					if (File.Exists(cachedCatalogPath))
					{
						Debug.Log("Current online level catalog is up to date, loading from cache");
						catalog = JsonConvert.DeserializeObject<LevelCatalog>(File.ReadAllText(cachedCatalogPath));
						PostCatalogLoad();
						yield break;
					}
				}
			}

			Debug.Log("Current online level catalog is out of date, downloading from web");
			downloadingCatalog = true;
			instance.StartCoroutine(m_DownloadCatalog(newCatalogVersion));
		}

		private IEnumerator m_DownloadCatalog(string newGuid)
		{
			downloadingCatalog = true;
			catalog = null;

			try
			{
				string catalogDir = Path.Combine(Plugin.workingDir, "OnlineCache");
				if (!Directory.Exists(catalogDir))
					Directory.CreateDirectory(catalogDir);
				string catalogPath = Path.Combine(catalogDir, "LevelCatalog.json");

				UnityWebRequest catalogRequest = new UnityWebRequest("https://raw.githubusercontent.com/eternalUnion/AngryLevels/release/LevelCatalog.json");
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
					catalog = JsonConvert.DeserializeObject<LevelCatalog>(File.ReadAllText(catalogPath));
					File.WriteAllText(Path.Combine(catalogDir, "LevelCatalogHash.txt"), newGuid);
				}
			}
			finally
			{
				downloadingCatalog = false;
				PostCatalogLoad();
			}
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
				if (!onlineLevels.TryGetValue(info.Guid, out field))
				{
					field = new OnlineLevelField(onlineLevelContainer);
					onlineLevels[info.Guid] = field;
				}

				// Update info text
				field.bundleName = info.Name;
				field.author = info.Author;
				field.bundleFileSize = info.Size;
				field.bundleGuid = info.Guid;
				field.bundleBuildHash = info.Hash;

				// Update ui
				field.UpdateUI();

				// Update thumbnail if not cahced or out of date
				string imageCacheDir = Path.Combine(Plugin.workingDir, "OnlineCache", "ThumbnailCache");
				if (!Directory.Exists(imageCacheDir))
					Directory.CreateDirectory(imageCacheDir);
				string imageCachePath = Path.Combine(imageCacheDir, $"{info.Guid}.png");

				bool downloadThumbnail = false;
				bool thumbnailExists = File.Exists(imageCachePath);
				if (!thumbnailExists)
				{
					Debug.Log($"Thumbnail for {info.Name} not cached, downloading");
					downloadThumbnail = true;
				}
				else if (!thumbnailHashes.TryGetValue(info.Guid, out string currentHash) || currentHash != info.ThumbnailHash)
				{
					Debug.Log($"Thumbnail for {info.Name} is outdated, downloading");
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

					UnityWebRequest req = new UnityWebRequest($"https://raw.githubusercontent.com/eternalUnion/AngryLevels/release/Levels/{info.Guid}/thumbnail.png");
					req.downloadHandler = new DownloadHandlerFile(imageCachePath);
					var handle = req.SendWebRequest();

					handle.completed += (e) =>
					{
						try
						{
							if (req.isHttpError || req.isNetworkError)
								return;
							field.DownloadPreviewImage(imageCachePath, true);
						}
						finally
						{
							req.Dispose();
						}
					};
				}
				else
				{
					field.DownloadPreviewImage(imageCachePath, false);
				}

				// Show the field if matches the field
				if (field.status == OnlineLevelField.OnlineLevelStatus.notInstalled)
				{
					if (showNotInstalledLevels.value)
						field.hidden = false;
				}
				else if (field.status == OnlineLevelField.OnlineLevelStatus.installed)
				{
					if (showInstalledLevels.value)
						field.hidden = false;
				}
				else if (field.status == OnlineLevelField.OnlineLevelStatus.updateAvailable)
				{
					if (showUpdateAvailableLevels.value)
						field.hidden = false;
				}
			}

			if (dirtyThumbnailCacheHashFile)
				SaveThumbnailHashes();

			CheckLevelUpdateText();
		}
	
		public static void CheckLevelUpdateText()
		{
			if (!Plugin.levelUpdateNotifierToggle.value)
			{
				Plugin.levelUpdateNotifier.hidden = true;
				return;
			}

			foreach (OnlineLevelField field in onlineLevels.Values)
			{
				if (field.status == OnlineLevelField.OnlineLevelStatus.updateAvailable)
				{
					Plugin.levelUpdateNotifier.hidden = false;
					return;
				}
			}

			Plugin.levelUpdateNotifier.hidden = true;
		}
	}
}
