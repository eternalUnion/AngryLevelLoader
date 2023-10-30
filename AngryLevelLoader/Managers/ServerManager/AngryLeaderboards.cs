using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers.BannedMods;
using BepInEx;
using BepInEx.Bootstrap;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AngryLevelLoader.Managers.ServerManager
{
	public static class AngryLeaderboards
	{
		#region Structs
		public const string RECORD_CATEGORY_ALL = "all";
		public const string RECORD_CATEGORY_PRANK = "prank";
		public enum RecordCategory
		{
			ALL,
			PRANK
		}
		public static readonly Dictionary<RecordCategory, string> RECORD_CATEGORY_DICT = new Dictionary<RecordCategory, string>()
		{
			{ RecordCategory.ALL, RECORD_CATEGORY_ALL },
			{ RecordCategory.PRANK, RECORD_CATEGORY_PRANK },
		};

		public const string RECORD_DIFFICULTY_HARMLESS = "harmless";	
		public const string RECORD_DIFFICULTY_LENIENT = "lenient";
		public const string RECORD_DIFFICULTY_STANDARD = "standard";
		public const string RECORD_DIFFICULTY_VIOLENT = "violent";
		public enum RecordDifficulty
		{
			HARMLESS,
			LENIENT,
			STANDARD,
			VIOLENT
		}
		public static readonly Dictionary<RecordDifficulty, string> RECORD_DIFFICULTY_DICT = new Dictionary<RecordDifficulty, string>()
		{
			{ RecordDifficulty.HARMLESS, RECORD_DIFFICULTY_HARMLESS },
			{ RecordDifficulty.LENIENT, RECORD_DIFFICULTY_LENIENT },
			{ RecordDifficulty.STANDARD, RECORD_DIFFICULTY_STANDARD },
			{ RecordDifficulty.VIOLENT, RECORD_DIFFICULTY_VIOLENT },
		};
		public static RecordDifficulty DifficultyFromInteger(int difficulty)
		{
			switch (difficulty)
			{
				case 0:
					return RecordDifficulty.HARMLESS;
				case 1:
					return RecordDifficulty.LENIENT;
				case 2:
					return RecordDifficulty.STANDARD;
				default:
				case 3:
					return RecordDifficulty.VIOLENT;
			}
		}

		public class RecordInfo
		{
			public string steamId { get; set; }
			public int time { get; set; }
		}
		#endregion
		
		public static string[] bannedMods = null;
		public static bool bannedModsListLoaded = false;

		private static Task<GetBannedModsResult> loadBannedModsTask = null;
		public static bool loadingBannedModsList
		{
			get => loadBannedModsTask != null && !loadBannedModsTask.IsCompleted;
		}

		public static void LoadBannedModsList()
		{
			if (bannedModsListLoaded || loadingBannedModsList)
				return;

			loadBannedModsTask = GetBannedModsTask();
			loadBannedModsTask.ContinueWith((task) =>
			{
				var result = task.Result;

				if (result.status == GetBannedModsStatus.OK)
				{
					bannedModsListLoaded = true;
					bannedMods = result.response.mods;
				}

				Plugin.CheckForBannedMods();
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public struct PostRecordInfo
		{
			public RecordCategory category;
			public RecordDifficulty difficulty;
			public string bundleGuid;
			public string hash;
			public string levelId;
			public int time;
		}
		public static List<Task> postRecordTasks = new List<Task>();

		private static async Task TryPostRecordInternalTask(PostRecordInfo info)
		{
			// Cheats + major assists check
			if (!GameStateManager.CanSubmitScores)
			{
				Plugin.logger.LogWarning("Angry did not post the record because cheats or major assists were used");
				return;
			}

			// Difficulty range
			int difficulty = PrefsManager.Instance.GetInt("difficulty", -1);
			if (difficulty < 0 || difficulty > 3)
			{
				Plugin.logger.LogWarning("Angry did not post the record because current difficulty is not valid");
				return;
			}

			// Leaderboard banned mods
			string[] bannedModsList = bannedMods;
			if (!bannedModsListLoaded)
			{
				if (!loadingBannedModsList)
					LoadBannedModsList();
				await loadBannedModsTask;

				if (!bannedModsListLoaded)
				{
					Plugin.logger.LogWarning("Banned mods list could not be loaded, using the local list");
					bannedModsList = BannedModsManager.LOCAL_BANNED_MODS_LIST;
				}
			}

			bool bannedModsFound = false;
			foreach (string plugin in Chainloader.PluginInfos.Keys)
			{
				if (Array.IndexOf(bannedModsList, plugin) == -1)
					continue;

				if (!BannedModsManager.guidToName.TryGetValue(plugin, out string realName))
					realName = plugin;

				// First, check for a soft ban checker
				if (BannedModsManager.checkers.TryGetValue(plugin, out Func<SoftBanCheckResult> checker))
				{
					try
					{
						var result = checker();

						if (result.banned)
						{
							Plugin.logger.LogWarning($"Banned mod found: {realName}\n{result.message}");
							bannedModsFound = true;
						}
					}
					catch (Exception e)
					{
						Plugin.logger.LogError($"Exception thrown while checking for soft ban for {realName}\n{e}");
						bannedModsFound = true;
					}
				}
				// Failsafe: assume banned
				else
				{
					Plugin.logger.LogWarning($"Mod {realName} has no checker. Assumed to be banned. Is your angry up to date?");
					bannedModsFound = true;
				}
			}

			if (bannedModsFound)
			{
				Plugin.logger.LogWarning("Angry did not post the record because there were banned mods found");
				return;
			}

			Plugin.logger.LogInfo("Environment safe to send record. Posting to angry servers...");

			var postResult = await PostRecordTask(info.category, info.difficulty, info.bundleGuid, info.hash, info.levelId, info.time);

			if (postResult.completedSuccessfully)
			{
				if (postResult.status == PostRecordStatus.OK)
				{
					Plugin.logger.LogInfo($"Record posted successfully! Ranking: {postResult.response.ranking}, New Best: {postResult.response.newBest}");
				}
				else
				{
					switch (postResult.status)
					{
						case PostRecordStatus.BANNED:
							Plugin.logger.LogError("User banned from the leaderboards");
							break;

						case PostRecordStatus.INVALID_BUNDLE:
						case PostRecordStatus.INVALID_ID:
							Plugin.logger.LogWarning("Level's leaderboards were not enabled");
							break;

						case PostRecordStatus.RATE_LIMITED:
							Plugin.logger.LogWarning("Too many requests sent. Adding record to the pending list");
							Plugin.AddPendingRecord(info);
							break;

						case PostRecordStatus.INVALID_HASH:
							Plugin.logger.LogWarning("Current bundle version is not up to date with the leaderboard");
							break;

						case PostRecordStatus.INVALID_TIME:
							Plugin.logger.LogWarning($"Angry server rejected the sent time {info.time}");
							break;

						default:
							Plugin.logger.LogWarning($"Encountered an unknown error while posting record. Status: {postResult.status}, Message: '{postResult.message}'. Adding record to the pending list");
							Plugin.AddPendingRecord(info);
							break;
					}
				}
			}
			else
			{
				Plugin.logger.LogWarning($"Encountered a network error while posting record. Adding to the pending list");
				Plugin.AddPendingRecord(info);
			}
		}

		public static Task TryPostRecordTask(PostRecordInfo info)
		{
			Task postRecordTask = TryPostRecordInternalTask(info);
			postRecordTasks.Add(postRecordTask);
			postRecordTask.ContinueWith((task) =>
			{
				if (task.Exception != null)
				{
					Plugin.logger.LogError($"Post record task threw an exception\n{task.Exception}");
				}

				postRecordTasks.Remove(task);
			}, TaskScheduler.FromCurrentSynchronizationContext());

			return postRecordTask;
		}

		#region Get Records
		public enum GetRecordsStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			MISSING_CATEGORY = 4,
			INVALID_CATEGORY = 5,
			MISSING_DIFFICULTY = 6,
			INVALID_DIFFICULTY = 7,
			MISSING_BUNDLE = 8,
			INVALID_BUNDLE = 9,
			MISSING_ID = 10,
			INVALID_ID = 11,
			MISSING_OFFSET = 12,
			INVALID_OFFSET = 13,
			MISSING_COUNT = 14,
			INVALID_COUNT = 15,
		}

		public class GetRecordsResponse : AngryResponse
		{
			public int offset { get; set; }
			public RecordInfo[] records;
			public int totalCount { get; set; }
		}

		public class GetRecordsResult : AngryResult<GetRecordsResponse, GetRecordsStatus>
		{

		}

		public static async Task<GetRecordsResult> GetRecordsTask(RecordCategory category, RecordDifficulty difficulty, string bundleGuid, string levelId, int offset, int count, CancellationToken cancellationToken = default)
		{
			GetRecordsResult result = new GetRecordsResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/getRecords?category={RECORD_CATEGORY_DICT[category]}&difficulty={RECORD_DIFFICULTY_DICT[difficulty]}&bundleGuid={bundleGuid}&levelId={levelId}&offset={offset}&count={count}";

			await AngryRequest.MakeRequest(url, result, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetRecordsStatus.FAILED;
			return result;
		}
		#endregion

		#region Post Record
		public enum PostRecordStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,
			INVALID_TOKEN = 1,
			MISSING_TIME = 2,
			INVALID_TIME = 3,
			MISSING_CATEGORY = 4,
			INVALID_CATEGORY = 5,
			MISSING_DIFFICULTY = 6,
			INVALID_DIFFICULTY = 7,
			MISSING_BUNDLE = 8,
			INVALID_BUNDLE = 9,
			MISSING_ID = 10,
			INVALID_ID = 11,
			MISSING_HASH = 12,
			INVALID_HASH = 13,
			BANNED = 14,
		}

		public class PostRecordResponse : AngryResponse
		{
			public int ranking { get; set; }
			public bool newBest { get; set; }
		}

		public class PostRecordResult : AngryResult<PostRecordResponse, PostRecordStatus>
		{

		}

		public static async Task<PostRecordResult> PostRecordTask(RecordCategory category, RecordDifficulty difficulty, string bundleGuid, string hash, string levelId, int time, CancellationToken cancellationToken = default)
		{
			PostRecordResult result = new PostRecordResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/postRecord?category={RECORD_CATEGORY_DICT[category]}&difficulty={RECORD_DIFFICULTY_DICT[difficulty]}&bundleGuid={bundleGuid}&hash={hash}&levelId={levelId}&time={time}";

			await AngryRequest.MakeRequestWithToken(url, result, PostRecordStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = PostRecordStatus.FAILED;
			return result;
		}
		#endregion

		#region Get User Record
		public enum GetUserRecordStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			INVALID_TOKEN = 1,
			MISSING_CATEGORY = 4,
			INVALID_CATEGORY = 5,
			MISSING_DIFFICULTY = 6,
			INVALID_DIFFICULTY = 7,
			MISSING_BUNDLE = 8,
			INVALID_BUNDLE = 9,
			MISSING_ID = 10,
			INVALID_ID = 11,
			MISSING_TARGET_USER_ID = 12,
		}

		public class GetUserRecordResponse : AngryResponse
		{
			public int ranking { get; set; }
			public int time { get; set; }
		}

		public class GetUserRecordResult : AngryResult<GetUserRecordResponse, GetUserRecordStatus>
		{

		}

		public static async Task<GetUserRecordResult> GetUserRecordTask(RecordCategory category, RecordDifficulty difficulty, string bundleGuid, string levelId, string targetUserId, CancellationToken cancellationToken = default)
		{
			GetUserRecordResult result = new GetUserRecordResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/getUserRecord?category={RECORD_CATEGORY_DICT[category]}&difficulty={RECORD_DIFFICULTY_DICT[difficulty]}&bundleGuid={bundleGuid}&levelId={levelId}&targetUserId={targetUserId}";

			await AngryRequest.MakeRequestWithToken(url, result, GetUserRecordStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetUserRecordStatus.FAILED;
			return result;
		}
		#endregion

		#region Get User Records
		public enum GetUserRecordsStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			INVALID_TOKEN = 1,
			MISSING_CATEGORY = 4,
			INVALID_CATEGORY = 5,
			MISSING_DIFFICULTY = 6,
			INVALID_DIFFICULTY = 7,
			MISSING_BUNDLE = 8,
			INVALID_BUNDLE = 9,
			MISSING_ID = 10,
			INVALID_ID = 11,
			MISSING_JSON_BODY = 12,
		}

		public class UserRecord
		{
			public string steamId { get; set; }
			public int time { get; set; }
			public int globalRank { get; set; }
		}

		public class GetUserRecordsResponse : AngryResponse
		{
			public UserRecord[] records;
		}

		public class GetUserRecordsResult : AngryResult<GetUserRecordsResponse, GetUserRecordsStatus>
		{

		}

		private class GetUserRecordsBodyObject
		{
			public string[] targetUserIds;
		}

		public static async Task<GetUserRecordsResult> GetUserRecordsTask(RecordCategory category, RecordDifficulty difficulty, string bundleGuid, string levelId, IEnumerable<string> targetUserIds, CancellationToken cancellationToken = default)
		{
			GetUserRecordsResult result = new GetUserRecordsResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/getUserRecords?category={RECORD_CATEGORY_DICT[category]}&difficulty={RECORD_DIFFICULTY_DICT[difficulty]}&bundleGuid={bundleGuid}&levelId={levelId}";
			
			GetUserRecordsBodyObject bodyObj = new GetUserRecordsBodyObject();
			bodyObj.targetUserIds = targetUserIds.ToArray();
			string body = JsonConvert.SerializeObject(bodyObj);

			await AngryRequest.MakeRequestWithToken(url, result, GetUserRecordsStatus.INVALID_TOKEN, cancellationToken, method: "POST", body: body, contentType: AngryRequest.CONTENT_TYPE_JSON);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetUserRecordsStatus.FAILED;
			return result;
		}
		#endregion

		#region Check For Banned Mods
		public enum CheckForBannedModsState
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			BANNED_MOD = 1,
			MISSING_JSON = 2,
			MISSING_MODS_ARR = 3,
		}

		public class CheckForBannedModsResponse : AngryResponse
		{
			public string[] mods;
		}

		public class CheckForBannedModsResult : AngryResult<CheckForBannedModsResponse, CheckForBannedModsState>
		{

		}

		private class CheckForBannedModsBodyObject
		{
			public string[] mods;
		}
		
		public static async Task<CheckForBannedModsResult> CheckForBannedModsTask(IEnumerable<string> mods, CancellationToken cancellationToken = default)
		{
			CheckForBannedModsResult result = new CheckForBannedModsResult();
			string url = AngryPaths.SERVER_ROOT + "/leaderboards/checkForBannedMods";

			CheckForBannedModsBodyObject bodyObject = new CheckForBannedModsBodyObject();
			bodyObject.mods = mods.ToArray();
			string body = JsonConvert.SerializeObject(bodyObject);

			await AngryRequest.MakeRequest(url, result, cancellationToken, method: "POST", body: body, contentType: AngryRequest.CONTENT_TYPE_JSON);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = CheckForBannedModsState.FAILED;
			return result;
		}

		public static Task<CheckForBannedModsResult> CheckForBannedModsTask(CancellationToken cancellationToken = default)
		{
			return CheckForBannedModsTask(BepInEx.Bootstrap.Chainloader.PluginInfos.Keys, cancellationToken);
		}
		#endregion

		#region Get Banned Mods
		public enum GetBannedModsStatus
		{
			FAILED = -2,
			OK = 0
		}

		public class GetBannedModsResponse : AngryResponse
		{
			public string[] mods;
		}

		public class GetBannedModsResult : AngryResult<GetBannedModsResponse, GetBannedModsStatus>
		{

		}

		public static async Task<GetBannedModsResult> GetBannedModsTask(CancellationToken cancellationToken = default)
		{
			GetBannedModsResult result = new GetBannedModsResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/getBannedMods";

			await AngryRequest.MakeRequest(url, result, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetBannedModsStatus.FAILED;
			return result;
		}
		#endregion
	}
}
