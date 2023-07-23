using Newtonsoft.Json;
using PluginConfig.API;
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

		
		private class LoadingBarSpin : MonoBehaviour
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
				currentUi.SetActive(false);
		}

		public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
		{
			if (currentUi != null)
				currentUi.SetActive(!hierarchyHidden);
		}
	}

	public class OnlineLevelsManager : MonoBehaviour
	{
		private static OnlineLevelsManager instance;
		public static ConfigPanel onlineLevelsPanel;
		public static ConfigDivision onlineLevelContainer;
		public static LoadingCircleField loadingCircle;

		public static void Init()
		{
			if (instance == null)
			{
				instance = new GameObject().AddComponent<OnlineLevelsManager>();
				UnityEngine.Object.DontDestroyOnLoad(instance);
			}

			var refreshButton = new ButtonField(onlineLevelsPanel, "Refresh", "b_onlineLevelsRefresh");
			refreshButton.onClick += RefreshAsync;
			loadingCircle = new LoadingCircleField(onlineLevelsPanel);
			loadingCircle.hidden = true;
			onlineLevelContainer = new ConfigDivision(onlineLevelsPanel, "p_onlineLevelsDiv");
		}

		private static bool downloadingCatalog = false;
		public static LevelCatalog catalog;

		public static void RefreshAsync()
		{
			if (downloadingCatalog)
				return;

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
		}
	}
}
