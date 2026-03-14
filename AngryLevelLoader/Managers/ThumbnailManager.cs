using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static AngryLevelLoader.Managers.OnlineLevelsManager;

namespace AngryLevelLoader.Managers
{
	public static class ThumbnailManager
	{
		private static Dictionary<string, Texture2D> bundleThumbnails = new Dictionary<string, Texture2D>();
		private static Dictionary<string, Texture2D> levelThumbnails = new Dictionary<string, Texture2D>();

		private static Dictionary<string, Task<Texture2D>> bundleThumbnailRequests = new Dictionary<string, Task<Texture2D>>();
		private static Dictionary<string, Task<Texture2D>> levelThumbnailRequests = new Dictionary<string, Task<Texture2D>>();

		private static async Task<Texture2D> _GetBundleThumbnailTask(string bundleGuid)
		{
			if (bundleThumbnails.TryGetValue(bundleGuid, out Texture2D cachedThumbnail))
				return cachedThumbnail;

			UnityWebRequest thumbnailReq = new UnityWebRequest(GetGithubURL(Repo.AngryLevels, $"Levels/{bundleGuid}/thumbnail.png"));
			thumbnailReq.downloadHandler = new DownloadHandlerTexture();
			await thumbnailReq.SendWebRequest();

			if (thumbnailReq.result != UnityWebRequest.Result.Success)
				return null;

			return bundleThumbnails[bundleGuid] = ((DownloadHandlerTexture)thumbnailReq.downloadHandler).texture;
		}

		public static async Task<Texture2D> GetBundleThumbnailTask(string bundleGuid)
		{
			if (bundleThumbnailRequests.TryGetValue(bundleGuid, out Task<Texture2D> currentTask) && !currentTask.IsCompleted)
				return await currentTask;

			return await (bundleThumbnailRequests[bundleGuid] = _GetBundleThumbnailTask(bundleGuid));
		}

		private static async Task<Texture2D> _GetLevelThumbnailTask(string bundleGuid, string levelId)
		{
			if (levelThumbnails.TryGetValue(levelId, out Texture2D cachedThumbnail))
				return cachedThumbnail;

			string levelMd5 = CryptographyUtils.GetMD5String(levelId);

			UnityWebRequest thumbnailReq = new UnityWebRequest(GetGithubURL(Repo.AngryLevels, $"Levels/{bundleGuid}/LevelThumbnails/{levelMd5}.png"));
			thumbnailReq.downloadHandler = new DownloadHandlerTexture();
			await thumbnailReq.SendWebRequest();

			if (thumbnailReq.result != UnityWebRequest.Result.Success)
				return null;

			return levelThumbnails[levelId] = ((DownloadHandlerTexture)thumbnailReq.downloadHandler).texture;
		}

		public static async Task<Texture2D> GetLevelThumbnailTask(string bundleGuid, string levelId)
		{
			if (levelThumbnailRequests.TryGetValue(levelId, out Task<Texture2D> currentTask) && !currentTask.IsCompleted)
				return await currentTask;

			return await (levelThumbnailRequests[levelId] = _GetLevelThumbnailTask(bundleGuid, levelId));
		}
	}
}
