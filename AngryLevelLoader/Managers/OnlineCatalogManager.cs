using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Extensions;
using AngryLevelLoader.Utils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace AngryLevelLoader.Managers
{
    /// <summary>
    /// Handles downloading and caching the online levels catalog.
    /// </summary>
    public static class OnlineCatalogManager
    {
        /// <summary>
        /// Current online levels catalog. Can be null if the catalog was not downloaded.
        /// </summary>
		public static LevelCatalog Catalog { get; private set; } = null;

        private static CachedTask<LevelCatalog> currentDownloadTask = new CachedTask<LevelCatalog>(DownloadCatalogWithHashCheck);
        /// <summary>
        /// True if there is a download task for the catalog.
        /// </summary>
        public static bool Downloading => currentDownloadTask.Running;

        /// <summary>
        /// Start downloading the catalog from GitHub. Before downloading the full catalog, only the
        /// MD5 hash of the catalog is downloaded and checked against the locally cached catalog. If
        /// the hashes match, cached catalog is returned without downloading a new copy. Otherwise,
        /// the latest catalog is downloaded and saved to the disk.
        /// 
        /// If there is already a download in progress, current download task is returned. Cancelling
        /// the task does not cancel tasks made by other callers.
        /// </summary>
        /// <returns>Up to date online level catalog. Can be null if there was a download error.</returns>
        public static Task<LevelCatalog> DownloadCatalogAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return currentDownloadTask.GetTask(cancellationToken);
        }

		private static async Task<LevelCatalog> DownloadCatalogWithHashCheck()
		{
			string newCatalogHash = "";
			string cachedCatalogPath = AngryPaths.LevelCatalogCachePath;
			AngryIOUtils.TryCreateDirectoryForFile(cachedCatalogPath);

			UnityWebRequest catalogVersionRequest = new UnityWebRequest(AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevels, "V2/LevelCatalogHash.txt"));
			catalogVersionRequest.downloadHandler = new DownloadHandlerBuffer();
			await catalogVersionRequest.SendWebRequest();

			if (catalogVersionRequest.result != UnityWebRequest.Result.Success)
			{
				Plugin.logger.LogError("Could not download catalog version");
				Catalog = null;
				return Catalog;
			}
			else
			{
				newCatalogHash = catalogVersionRequest.downloadHandler.text;
			}

			if (File.Exists(cachedCatalogPath))
			{
				string cachedCatalog = File.ReadAllText(cachedCatalogPath);
				string catalogHash = AngryCryptographyUtils.GetMD5String(cachedCatalog);

				try
				{
					LevelCatalog cachedCatalogObj = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);

					if (catalogHash == newCatalogHash)
					{
						Plugin.logger.LogInfo("Current online level catalog is up to date, loading from cache");
                        Catalog = cachedCatalogObj;
						return Catalog;
					}
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load cached level catalog, but it is corrupted");
				}
			}

			Plugin.logger.LogInfo("Current online level catalog is out of date, downloading from web");
            Catalog = await DownloadCatalog(newCatalogHash);
			return Catalog;
		}

		private static async Task<LevelCatalog> DownloadCatalog(string newHash)
		{
			string catalogPath = AngryPaths.LevelCatalogCachePath;
			AngryIOUtils.TryCreateDirectoryForFile(catalogPath);

			UnityWebRequest catalogRequest = new UnityWebRequest(AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevels, "V2/LevelCatalog.json"));
			catalogRequest.downloadHandler = new DownloadHandlerFile(catalogPath);
			await catalogRequest.SendWebRequest();

			if (catalogRequest.result != UnityWebRequest.Result.Success)
			{
				Plugin.logger.LogError("Could not download catalog");
				return null;
			}
			else
			{
				string cachedCatalog = File.ReadAllText(catalogPath);
				string catalogHash = AngryCryptographyUtils.GetMD5String(cachedCatalog);

                LevelCatalog catalog = null;

				try
				{
					catalog = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load level catalog, but it is corrupted");
					return null;
				}

				if (catalogHash != newHash)
				{
					Plugin.logger.LogWarning($"Catalog hash does not match, github did not cache the new catalog yet (current hash is {catalogHash}. online hash is {newHash})");
				}

                return catalog;
			}
		}
	
        internal static void LoadCachedCatalog()
        {
			string cachedCatalogPath = AngryPaths.LevelCatalogCachePath;
			if (File.Exists(cachedCatalogPath))
			{
				try
				{
					Catalog = JsonConvert.DeserializeObject<LevelCatalog>(File.ReadAllText(cachedCatalogPath));
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load cached level catalog, but it is corrupted");
				}
			}
		}
    }

	/// <summary>
	/// Handles downloading and caching the pre-revamp online levels catalog.
	/// </summary>
	internal static class OnlineCatalogManagerV1
	{
		/// <summary>
		/// Current online levels catalog. Can be null if the catalog was not downloaded.
		/// </summary>
		public static LevelCatalog Catalog { get; private set; } = null;

		private static CachedTask<LevelCatalog> currentDownloadTask = new CachedTask<LevelCatalog>(DownloadCatalogWithHashCheck);
		/// <summary>
		/// True if there is a download task for the catalog.
		/// </summary>
		public static bool Downloading => currentDownloadTask.Running;

		/// <summary>
		/// Start downloading the catalog from GitHub. Before downloading the full catalog, only the
		/// MD5 hash of the catalog is downloaded and checked against the locally cached catalog. If
		/// the hashes match, cached catalog is returned without downloading a new copy. Otherwise,
		/// the latest catalog is downloaded and saved to the disk.
		/// 
		/// If there is already a download in progress, current download task is returned. Cancelling
		/// the task does not cancel tasks made by other callers.
		/// </summary>
		/// <returns>Up to date online level catalog. Can be null if there was a download error.</returns>
		public static Task<LevelCatalog> DownloadCatalogAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return currentDownloadTask.GetTask(cancellationToken);
		}

		private static async Task<LevelCatalog> DownloadCatalogWithHashCheck()
		{
			string newCatalogHash = "";
			string cachedCatalogPath = AngryPaths.LevelCatalogV1CachePath;
			AngryIOUtils.TryCreateDirectoryForFile(cachedCatalogPath);

			UnityWebRequest catalogVersionRequest = new UnityWebRequest(AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevels, "LevelCatalogHash.txt"));
			catalogVersionRequest.downloadHandler = new DownloadHandlerBuffer();
			await catalogVersionRequest.SendWebRequest();

			if (catalogVersionRequest.result != UnityWebRequest.Result.Success)
			{
				Plugin.logger.LogError("Could not download catalog version");
				Catalog = null;
				return Catalog;
			}
			else
			{
				newCatalogHash = catalogVersionRequest.downloadHandler.text;
			}

			if (File.Exists(cachedCatalogPath))
			{
				string cachedCatalog = File.ReadAllText(cachedCatalogPath);
				string catalogHash = AngryCryptographyUtils.GetMD5String(cachedCatalog);

				try
				{
					LevelCatalog cachedCatalogObj = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);

					if (catalogHash == newCatalogHash)
					{
						Plugin.logger.LogInfo("Current online level catalog is up to date, loading from cache");
						Catalog = cachedCatalogObj;
						return Catalog;
					}
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load cached level catalog, but it is corrupted");
				}
			}

			Plugin.logger.LogInfo("Current online level catalog is out of date, downloading from web");
			Catalog = await DownloadCatalog(newCatalogHash);
			return Catalog;
		}

		private static async Task<LevelCatalog> DownloadCatalog(string newHash)
		{
			string catalogPath = AngryPaths.LevelCatalogV1CachePath;
			AngryIOUtils.TryCreateDirectoryForFile(catalogPath);

			UnityWebRequest catalogRequest = new UnityWebRequest(AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevels, "LevelCatalog.json"));
			catalogRequest.downloadHandler = new DownloadHandlerFile(catalogPath);
			await catalogRequest.SendWebRequest();

			if (catalogRequest.result != UnityWebRequest.Result.Success)
			{
				Plugin.logger.LogError("Could not download catalog");
				return null;
			}
			else
			{
				string cachedCatalog = File.ReadAllText(catalogPath);
				string catalogHash = AngryCryptographyUtils.GetMD5String(cachedCatalog);

				LevelCatalog catalog = null;

				try
				{
					catalog = JsonConvert.DeserializeObject<LevelCatalog>(cachedCatalog);
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load level catalog, but it is corrupted");
					return null;
				}

				if (catalogHash != newHash)
				{
					Plugin.logger.LogWarning($"Catalog hash does not match, github did not cache the new catalog yet (current hash is {catalogHash}. online hash is {newHash})");
				}

				return catalog;
			}
		}

		internal static void LoadCachedCatalog()
		{
			string cachedCatalogPath = AngryPaths.LevelCatalogV1CachePath;
			if (File.Exists(cachedCatalogPath))
			{
				try
				{
					Catalog = JsonConvert.DeserializeObject<LevelCatalog>(File.ReadAllText(cachedCatalogPath));
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load cached level catalog, but it is corrupted");
				}
			}
		}
	}
}
