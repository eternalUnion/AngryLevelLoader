using AngryLevelLoader.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace AngryLevelLoader.Managers
{
	/// <summary>
	/// Handles downloading and caching the online scripts catalog.
	/// </summary>
	public static class OnlineScriptsManager
	{
		/// <summary>
		/// Current online scripts catalog. Can be null if the catalog was not downloaded.
		/// </summary>
		public static ScriptCatalog ScriptCatalog { get; private set; } = null;

		private static Task<ScriptCatalog> downloadTask = null;
		/// <summary>
		/// True if there is a download task for the catalog.
		/// </summary>
		public static bool Downloading
		{
			get => downloadTask != null && !downloadTask.IsCompleted;
		}

		/// <summary>
		/// Start downloading the catalog from GitHub.
		/// If there is already a download in progress, current download task is returned. Cancelling
		/// the task does not cancel tasks made by other callers.
		/// </summary>
		/// <returns>Up to date online level catalog. Can be null if there was a download error.</returns>
		public static async Task<ScriptCatalog> DownloadCatalogAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!Downloading)
			{
				downloadTask = DownloadTask();
			}

			return await Task.Run(async () =>
			{
				return await downloadTask;
			}, cancellationToken);
		}

		private static async Task<ScriptCatalog> DownloadTask()
		{
			string newHash = "";
			UnityWebRequest hashReq = new UnityWebRequest(OnlineLevelsUI.GetGithubURL(OnlineLevelsUI.Repo.AngryLevels, "ScriptCatalogHash.txt"));
			
			try
			{
				hashReq.downloadHandler = new DownloadHandlerBuffer();
				await hashReq.SendWebRequest();

				if (hashReq.result != UnityWebRequest.Result.Success)
				{
					Plugin.logger.LogError("Could not download the script catalog hash");
					return ScriptCatalog;
				}

				newHash = hashReq.downloadHandler.text;
			}
			finally
			{
				hashReq.Dispose();
			}

			string cachedCatalogPath = AngryPaths.ScriptCatalogCachePath;
			if (File.Exists(cachedCatalogPath))
			{
				string catalog = File.ReadAllText(cachedCatalogPath);
				string hash = AngryCryptographyUtils.GetMD5String(catalog);
				if (hash == newHash)
				{
					Plugin.logger.LogInfo("Cached script catalog up to date");
					try
					{
						ScriptCatalog = JsonConvert.DeserializeObject<ScriptCatalog>(catalog);
					}
					catch (Exception)
					{
						Plugin.logger.LogError("Tried to load the script catalog, but it is corrupted");
						return null;
					}

					return ScriptCatalog;
				}
			}

			UnityWebRequest updatedCatalogRequest = new UnityWebRequest(OnlineLevelsUI.GetGithubURL(OnlineLevelsUI.Repo.AngryLevels, "ScriptCatalog.json"));
			try
			{
				updatedCatalogRequest.downloadHandler = new DownloadHandlerBuffer();
				await updatedCatalogRequest.SendWebRequest();

				if (updatedCatalogRequest.result != UnityWebRequest.Result.Success)
				{
					Plugin.logger.LogError("Could not download the script catalog");
					return ScriptCatalog;
				}

				try
				{
					ScriptCatalog = JsonConvert.DeserializeObject<ScriptCatalog>(updatedCatalogRequest.downloadHandler.text);
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load script catalog, but it is corrupted");
					return null;
				}

				File.WriteAllText(cachedCatalogPath, updatedCatalogRequest.downloadHandler.text);
				string currentHash = AngryCryptographyUtils.GetMD5String(updatedCatalogRequest.downloadHandler.text);

				if (currentHash != newHash)
				{
					Plugin.logger.LogWarning($"New script catalog hash value does not match online catalog hash value, github page not cached yet (current hash is {currentHash}. online hash is {newHash})");
				}

				return ScriptCatalog;
			}
			finally
			{
				updatedCatalogRequest.Dispose();
			}
		}

		internal static void LoadCachedCatalog()
		{
			string cachedCatalogPath = AngryPaths.ScriptCatalogCachePath;
			if (File.Exists(cachedCatalogPath))
			{
				try
				{
					ScriptCatalog = JsonConvert.DeserializeObject<ScriptCatalog>(File.ReadAllText(cachedCatalogPath));
				}
				catch (Exception)
				{
					Plugin.logger.LogError("Tried to load cached script catalog, but it is corrupted");
				}
			}
		}

		/// <summary>
		/// Return whether the given script exists in the catalog or not.
		/// </summary>
		/// <param name="script">Full name of the script file, including the .dll extension.</param>
		public static bool ScriptExistsInCatalog(string script)
		{
			if (ScriptCatalog == null)
				return false;
			return ScriptCatalog.Scripts.Where(s => s.FileName == script).Any();
		}

		/// <summary>
		/// Attempt to get the online entry for the given script.
		/// </summary>
		/// <param name="script">Full name of the script file, including the .dll extension.</param>
		/// <returns></returns>
		public static bool TryGetScriptInfo(string script, out ScriptInfo info)
		{
			info = ScriptCatalog == null ? null : ScriptCatalog.Scripts.Where(s => s.FileName == script).FirstOrDefault();
			return info != null;
		}
	}
}
