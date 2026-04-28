using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Extensions;
using AngryLevelLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static AngryLevelLoader.UserInterface.OnlineLevelsList;

namespace AngryLevelLoader.Managers
{
	internal class CachedTexture
	{
		public Texture2D texture;
		public string hash;
		public CachedTask<Texture2D> currentTask;

		public CachedTexture(Texture2D texture, string hash)
		{
			this.texture = texture;
			this.hash = hash;
		}

		public static async Task<Texture2D> GetTextureFromFile(string path)
		{
			UnityWebRequest req = UnityWebRequestTexture.GetTexture("file://" + path);
			await req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
				return null;

			return DownloadHandlerTexture.GetContent(req);
		}
	}

	/// <summary>
	/// Cache for online icons of online bundles. Icons are saved locally to avoid
	/// unnecessary downloads.
	/// </summary>
	public static class AngryOnlineThumbnailCache
	{
		private static Dictionary<string, CachedTexture> textureCache = new Dictionary<string, CachedTexture>();

		public static Task<Texture2D> GetThumbnail(string bundleGuid)
		{
			if (!textureCache.TryGetValue(bundleGuid, out CachedTexture cachedTexture))
			{
				cachedTexture = new CachedTexture(null, string.Empty);
				cachedTexture.currentTask = new CachedTask<Texture2D>(() => _GetTexture(bundleGuid));
				textureCache.Add(bundleGuid, cachedTexture);
			}

			return cachedTexture.currentTask.GetTask();
		}

		private static async Task<Texture2D> _GetTexture(string guid)
		{
			LevelCatalog catalog = OnlineCatalogManager.Catalog ?? await OnlineCatalogManager.DownloadCatalogAsync();
			if (catalog == null)
				return null;

			BundleInfo bundle = catalog.Levels.Where(b => b.Guid == guid).FirstOrDefault();
			if (bundle == null)
				return null;

			string hash = bundle.ThumbnailHash;

			if (textureCache.TryGetValue(guid, out CachedTexture cachedTexture))
			{
				if (cachedTexture.texture != null && cachedTexture.hash == hash)
					return cachedTexture.texture;
			}
			else
			{
				cachedTexture = new CachedTexture(null, hash);
				textureCache.Add(guid, cachedTexture);
			}

			string imageCacheDir = AngryPaths.ThumbnailCacheFolderPath;
			AngryIOUtils.TryCreateDirectory(imageCacheDir);
			string imageCachePath = Path.Combine(imageCacheDir, $"{guid}.png");

			if (File.Exists(imageCachePath))
			{
				string fileHash = AngryCryptographyUtils.GetMD5String(File.ReadAllBytes(imageCachePath));
				if (fileHash == hash)
				{
					Texture2D fileTexture = await CachedTexture.GetTextureFromFile(imageCachePath);
					if (fileTexture != null)
					{
						cachedTexture.texture = fileTexture;
						cachedTexture.hash = hash;
						return fileTexture;
					}
				}
			}

			if (File.Exists(imageCachePath))
				File.Delete(imageCachePath);

			string url = AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevels, $"Levels/{guid}/thumbnail.png");

			UnityWebRequest thumbnailReq = new UnityWebRequest(url);
			thumbnailReq.downloadHandler = new DownloadHandlerFile(imageCachePath);
			await thumbnailReq.SendWebRequest();

			if (thumbnailReq.result != UnityWebRequest.Result.Success)
				return null;

			Texture2D texture = await CachedTexture.GetTextureFromFile(imageCachePath);
			if (texture != null)
			{
				cachedTexture.texture = texture;
				cachedTexture.hash = hash;
				return texture;
			}

			return null;
		}
	}

	/// <summary>
	/// Cache for level icons of online bundles. Downloaded on demand since there is
	/// no hashing implemented for level icons.
	/// </summary>
	public static class AngryLevelThumbnailCache
	{
		private static Dictionary<string, CachedTexture> textureCache = new Dictionary<string, CachedTexture>();

		public static Task<Texture2D> GetThumbnail(string bundleGuid, string levelId)
		{
			if (!textureCache.TryGetValue(levelId, out CachedTexture cachedTexture))
			{
				cachedTexture = new CachedTexture(null, string.Empty);
				cachedTexture.currentTask = new CachedTask<Texture2D>(() => _GetTexture(bundleGuid, levelId));
				textureCache.Add(levelId, cachedTexture);
			}

			return cachedTexture.currentTask.GetTask();
		}

		private static async Task<Texture2D> _GetTexture(string bundleGuid, string levelId)
		{
			CachedTexture cache = textureCache[levelId];
			if (cache.texture != null)
				return cache.texture;

			string levelMd5 = AngryCryptographyUtils.GetMD5String(levelId);

			UnityWebRequest thumbnailReq = new UnityWebRequest(AngryPaths.GetGithubURL(AngryPaths.Repo.AngryLevels, $"Levels/{bundleGuid}/LevelThumbnails/{levelMd5}.png"));
			thumbnailReq.downloadHandler = new DownloadHandlerTexture();
			await thumbnailReq.SendWebRequest();

			if (thumbnailReq.result != UnityWebRequest.Result.Success)
				return null;

			cache.texture = ((DownloadHandlerTexture)thumbnailReq.downloadHandler).texture;
			return cache.texture;
		}
	}
}
