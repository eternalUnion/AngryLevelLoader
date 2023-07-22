using Newtonsoft.Json;
using PluginConfig.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AngryLevelLoader
{
	public class LevelInfo
	{
		public string Name { get; set; }
		public string Author { get; set; }
		public string Location { get; set; }
		public float Size { get; set; }
		public string Hash { get; set; }
	}

	public class LevelCatalog
	{
		public List<LevelInfo> Levels;
	}

	public class OnlineLevelsManager : MonoBehaviour
	{
		private static OnlineLevelsManager instance;
		public static ConfigPanel onlineLevelsPanel;

		public static void Init()
		{
			if (instance == null)
			{
				instance = new GameObject().AddComponent<OnlineLevelsManager>();
				UnityEngine.Object.DontDestroyOnLoad(instance);
			}
		}

		public static LevelCatalog catalog;

		public static void RefreshAsync()
		{
			if (instance.downloadingCatalog)
				return;

			instance.StartCoroutine(instance.m_Refresh());
		}

		private bool downloadingCatalog = false;
		private IEnumerator m_Refresh()
		{
			downloadingCatalog = true;

			try
			{
				UnityWebRequest catalogRequest = new UnityWebRequest("https://raw.githubusercontent.com/eternalUnion/AngryLevels/release/LevelCatalog.json");
				catalogRequest.downloadHandler = new DownloadHandlerBuffer();
				yield return catalogRequest.SendWebRequest();
			
				if (catalogRequest.isNetworkError || catalogRequest.isHttpError)
				{
					Debug.LogError("Could not download catalog");
				}
				else
				{
					catalog = JsonConvert.DeserializeObject<LevelCatalog>(catalogRequest.downloadHandler.text);
				}
			}
			finally
			{
				downloadingCatalog = false;
			}
		}
	}
}
