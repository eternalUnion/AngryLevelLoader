using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace AngryLevelLoader.Patches
{
	public struct SteamUserCache
	{
		public ulong steamId;
		public string name;
		public Texture2D profilePicture;
	}

	public static class SteamCacheManager
	{
		private static Dictionary<ulong, SteamUserCache> steamUserCacheDict = new Dictionary<ulong, SteamUserCache>();
		private static Dictionary<ulong, Task<SteamUserCache>> requestDict = new Dictionary<ulong, Task<SteamUserCache>>();

		public static bool TryGetUser(ulong steamId, out SteamUserCache user)
		{
			return steamUserCacheDict.TryGetValue(steamId, out user);
		}

		public static Task<SteamUserCache> RequestUser(ulong steamId)
		{
			if (requestDict.TryGetValue(steamId, out Task<SteamUserCache> reqTask))
				return reqTask;

			Task<SteamUserCache> newTask = GetSteamUserTask(steamId);
			newTask.ContinueWith((task) =>
			{
				requestDict.Remove(steamId);
			}, TaskScheduler.FromCurrentSynchronizationContext());
			requestDict[steamId] = newTask;

			return newTask;
		}

		private static async Task<SteamUserCache> GetSteamUserTask(ulong steamId)
		{
			if (steamUserCacheDict.TryGetValue(steamId, out SteamUserCache cachedUser))
				return cachedUser;

			bool doCache = true;
			SteamUserCache result = new SteamUserCache();
			result.steamId = steamId;

			SteamId userId = steamId;
			SteamFriends.RequestUserInformation(userId, true);
			
			var profilePicture = await SteamFriends.GetMediumAvatarAsync(userId);
			if (profilePicture != null)
			{
				Texture2D texture2D = new Texture2D((int)profilePicture.Value.Width, (int)profilePicture.Value.Height, TextureFormat.RGBA32, false);
				texture2D.LoadRawTextureData(profilePicture.Value.Data);
				texture2D.Apply();
				result.profilePicture = texture2D;
			}
			else
			{
				doCache = false;
			}

			result.name = new Friend(userId).Name;

			if (doCache)
				steamUserCacheDict[steamId] = result;

			return result;
		}
	}
}
