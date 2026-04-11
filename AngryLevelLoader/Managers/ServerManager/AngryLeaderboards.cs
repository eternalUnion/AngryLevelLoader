using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers.BannedMods;
using BepInEx;
using BepInEx.Bootstrap;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		public const string RECORD_CATEGORY_CHALLENGE = "challenge";
		public const string RECORD_CATEGORY_NOMO = "nomo";
		public const string RECORD_CATEGORY_NOMOW = "nomow";
		public enum RecordCategory
		{
			ALL,
			PRANK,
			CHALLENGE,
			NOMO,
			NOMOW,
		}
		public static readonly Dictionary<RecordCategory, string> RECORD_CATEGORY_DICT = new Dictionary<RecordCategory, string>()
		{
			{ RecordCategory.ALL, RECORD_CATEGORY_ALL },
			{ RecordCategory.PRANK, RECORD_CATEGORY_PRANK },
			{ RecordCategory.CHALLENGE, RECORD_CATEGORY_CHALLENGE },
			{ RecordCategory.NOMO, RECORD_CATEGORY_NOMO },
			{ RecordCategory.NOMOW, RECORD_CATEGORY_NOMOW },
		};

		public const string RECORD_DIFFICULTY_ANY = "any";
		public const string RECORD_DIFFICULTY_HARMLESS = "harmless";	
		public const string RECORD_DIFFICULTY_LENIENT = "lenient";
		public const string RECORD_DIFFICULTY_STANDARD = "standard";
		public const string RECORD_DIFFICULTY_VIOLENT = "violent";
		public const string RECORD_DIFFICULTY_BRUTAL = "brutal";
		public enum RecordDifficulty
		{
			ANY,
			HARMLESS,
			LENIENT,
			STANDARD,
			VIOLENT,
			BRUTAL,
		}
		public static readonly Dictionary<RecordDifficulty, string> RECORD_DIFFICULTY_DICT = new Dictionary<RecordDifficulty, string>()
		{
			{ RecordDifficulty.ANY, RECORD_DIFFICULTY_ANY },
			{ RecordDifficulty.HARMLESS, RECORD_DIFFICULTY_HARMLESS },
			{ RecordDifficulty.LENIENT, RECORD_DIFFICULTY_LENIENT },
			{ RecordDifficulty.STANDARD, RECORD_DIFFICULTY_STANDARD },
			{ RecordDifficulty.VIOLENT, RECORD_DIFFICULTY_VIOLENT },
			{ RecordDifficulty.BRUTAL, RECORD_DIFFICULTY_BRUTAL },
		};
		public static readonly Dictionary<string, RecordDifficulty> RECORD_DIFFICULTY_REVERSE_DICT = new Dictionary<string, RecordDifficulty>()
		{
			{ RECORD_DIFFICULTY_ANY, RecordDifficulty.ANY },
			{ RECORD_DIFFICULTY_HARMLESS, RecordDifficulty.HARMLESS },
			{ RECORD_DIFFICULTY_LENIENT, RecordDifficulty.LENIENT },
			{ RECORD_DIFFICULTY_STANDARD, RecordDifficulty.STANDARD },
			{ RECORD_DIFFICULTY_VIOLENT, RecordDifficulty.VIOLENT },
			{ RECORD_DIFFICULTY_BRUTAL, RecordDifficulty.BRUTAL },
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
				case 4:
					return RecordDifficulty.BRUTAL;
			}
		}

		public class RecordInfo
		{
			public string steamId { get; set; }
			public int time { get; set; }
			public string difficulty { get; set; }
			public bool censorIcon { get; set; }
			public bool censorName { get; set; }
		}
		#endregion
		
		public static string[] bannedMods = null;
		public static bool bannedModsListLoaded = false;

		private static Task<GetBannedModsResult> loadBannedModsTask = null;
		public static bool loadingBannedModsList
		{
			get => loadBannedModsTask != null && !loadBannedModsTask.IsCompleted;
		}

		public static void LoadBannedModsList(Action<bool> callback = null)
		{
			if (bannedModsListLoaded)
			{
				if (callback != null)
					callback(true);

				return;
			}

			if (loadBannedModsTask != null)
			{
				if (callback != null)
				{
					loadBannedModsTask.ContinueWith((task) =>
					{
						callback(task.Result.status == GetBannedModsStatus.OK);
					}, TaskScheduler.FromCurrentSynchronizationContext());
				}

				return;
			}

			loadBannedModsTask = GetBannedModsTask();
			loadBannedModsTask.ContinueWith((task) =>
			{
				loadBannedModsTask = null;
				var result = task.Result;

				if (result.status == GetBannedModsStatus.OK)
				{
					bannedModsListLoaded = true;
					bannedMods = result.response.mods;
				}

				Plugin.CheckForBannedMods();

				if (callback != null)
					callback(result.status == GetBannedModsStatus.OK);
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

		private static async Task<string> TryPostRecordInternalTask(PostRecordInfo info)
		{
			// Cheats + major assists check
			if (LeaderboardController.LeaderboardsBlocked)
			{
				Plugin.logger.LogWarning("Angry did not post the record because cheats or major assists were used");
				return "<color=red>Failed to post record:\nCheats used</color>";
			}

			// Difficulty range
			int difficulty = PrefsManager.Instance.GetInt("difficulty", -1);
			if (difficulty < 0 || difficulty > 4)
			{
				Plugin.logger.LogWarning("Angry did not post the record because current difficulty is not valid");
				return "<color=red>Failed to post record:\nInvalid difficulty</color>";
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
				return "<color=red>Failed to post record:\nBanned mods found</color>";
			}

			Plugin.logger.LogInfo("Environment safe to send record. Posting to angry servers...");

			var postResult = await PostRecordTask(info.category, info.difficulty, info.bundleGuid, info.hash, info.levelId, info.time);

			if (postResult.completedSuccessfully)
			{
				if (postResult.status == PostRecordStatus.OK)
				{
					Plugin.logger.LogInfo($"Record posted successfully! Ranking: {postResult.response.ranking}, New Best: {postResult.response.newBest}");
					return $"<color=green>Record posted! Rank {postResult.response.ranking}{(postResult.response.newBest ? "\n(NEW BEST)" : "")}</color>";
				}
				else
				{
					switch (postResult.status)
					{
						case PostRecordStatus.BANNED:
							Plugin.logger.LogError("User banned from the leaderboards");
							return "<color=red>Failed to post record:\nBanned</color>";

						case PostRecordStatus.INVALID_BUNDLE:
						case PostRecordStatus.INVALID_ID:
							Plugin.logger.LogWarning("Level's leaderboards were not enabled");
							return "<color=red>Failed to post record:\nLeaderboards disabled for this level</color>";

						case PostRecordStatus.RATE_LIMITED:
							Plugin.logger.LogWarning("Too many requests sent. Adding record to the pending list");
							Plugin.AddPendingRecord(info);
							return "<color=red>Failed to post record:\nSent too many requests, added to pending list</color>";

						case PostRecordStatus.INVALID_HASH:
							Plugin.logger.LogWarning("Current bundle version is not up to date with the leaderboard");
							return "<color=red>Failed to post record:\nBundle out of date</color>";

						case PostRecordStatus.INVALID_TIME:
							Plugin.logger.LogWarning($"Angry server rejected the sent time {info.time}");
							return "<color=red>Failed to post record:\nInvalid time</color>";

						default:
							Plugin.logger.LogWarning($"Encountered an unknown error while posting record. Status: {postResult.status}, Message: '{postResult.message}'. Adding record to the pending list");
							Plugin.AddPendingRecord(info);
							return "<color=red>Failed to post record:\nReason unknown</color>";
					}
				}
			}
			else
			{
				Plugin.logger.LogWarning($"Encountered a network error while posting record. Adding to the pending list");
				Plugin.AddPendingRecord(info);
				return "<color=red>Failed to post record:\nNetwork error, added to the pending list</color>";
			}
		}

		public static Task<string> TryPostRecordTask(PostRecordInfo info)
		{
			Task<string> postRecordTask = TryPostRecordInternalTask(info);
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
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/postRecord?category={RECORD_CATEGORY_DICT[category]}&difficulty={RECORD_DIFFICULTY_DICT[difficulty]}&bundleGuid={bundleGuid}&hash={hash}&levelId={Uri.EscapeDataString(levelId)}&time={time}";

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
			public string difficulty { get; set; }
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
			public string difficulty { get; set; }
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

		#region Get User Info
		public enum GetUserInfoStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			INTERNAL_ERROR = 3,
			INVALID_ID = 4,
		}

		public class GetUserInfoResponse : AngryResponse
		{
			public bool leaderboardBanned { get; set; }
			public int recordCount { get; set; }
			public int reportCount { get; set; }
			public int removedRecordCount { get; set; }
		}

		public class GetUserInfoResult : AngryResult<GetUserInfoResponse, GetUserInfoStatus>
		{

		}

		public static async Task<GetUserInfoResult> GetUserInfoTask(string targetId, CancellationToken cancellationToken = default)
		{
			GetUserInfoResult result = new GetUserInfoResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/getUserInfo?targetId={targetId}";

			await AngryRequest.MakeRequestWithToken(url, result, GetUserInfoStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetUserInfoStatus.FAILED;
			return result;
		}
		#endregion

		#region Manage User
		public enum ManageUserStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			INTERNAL_ERROR = 3,
			INVALID_ID = 4,
		}

		public class ManageUserResponse : AngryResponse
		{

		}

		public class ManageUserResult : AngryResult<ManageUserResponse, ManageUserStatus>
		{

		}

		public static async Task<ManageUserResult> ManageUserTask(string targetId, bool censorIcon = false, bool censorName = false, bool banUser = false, CancellationToken cancellationToken = default)
		{
			ManageUserResult result = new ManageUserResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/manageUser?targetId={targetId}&censorIcon={(censorIcon ? "true" : "false")}&censorName={(censorName ? "true" : "false")}&banUser={(banUser ? "true" : "false")}";

			await AngryRequest.MakeRequestWithToken(url, result, ManageUserStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = ManageUserStatus.FAILED;
			return result;
		}
		#endregion

		#region Remove Record
		public enum RemoveRecordStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			INTERNAL_ERROR = 3,
			INVALID_ID = 4,
			MISSING_CATEGORY = 5,
			INVALID_CATEGORY = 6,
			MISSING_DIFFICULTY = 7,
			INVALID_DIFFICULTY = 8,
			MISSING_BUNDLE = 9,
			INVALID_BUNDLE = 10,
			INVALID_LEVEL_ID = 11,
			MISSING_LEVEL_ID = 12,
			ENTRY_NOT_FOUND = 13,
		}

		public class RemoveRecordResponse : AngryResponse
		{

		}

		public class RemoveRecordResult : AngryResult<RemoveRecordResponse, RemoveRecordStatus>
		{

		}

		public static async Task<RemoveRecordResult> RemoveRecordTask(string targetId, string bundleGuid, string levelId, RecordCategory category, RecordDifficulty difficulty, CancellationToken cancellationToken = default)
		{
			// bundleGuid, levelId, category, difficulty
			RemoveRecordResult result = new RemoveRecordResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/removeRecord?targetId={targetId}&bundleGuid={bundleGuid}&levelId={Uri.EscapeDataString(levelId)}&category={RECORD_CATEGORY_DICT[category]}&difficulty={RECORD_DIFFICULTY_DICT[difficulty]}";

			await AngryRequest.MakeRequestWithToken(url, result, RemoveRecordStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = RemoveRecordStatus.FAILED;
			return result;
		}
		#endregion

		#region Clear Records
		public enum ClearRecordsStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			INTERNAL_ERROR = 3,
			INVALID_ID = 4,
		}

		public class ClearRecordsResponse : AngryResponse
		{
			public int removedRecordCount { get; set; }
		}

		public class ClearRecordsResult : AngryResult<ClearRecordsResponse, ClearRecordsStatus>
		{

		}

		public static async Task<ClearRecordsResult> ClearRecordsTask(string targetId, CancellationToken cancellationToken = default)
		{
			ClearRecordsResult result = new ClearRecordsResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/clearRecords?targetId={targetId}";

			await AngryRequest.MakeRequestWithToken(url, result, ClearRecordsStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = ClearRecordsStatus.FAILED;
			return result;
		}
		#endregion

		#region Get User History
		public enum GetUserHistoryStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			INTERNAL_ERROR = 3,
			INVALID_ID = 4,
		}

		public class GetUserHistoryResponse : AngryResponse
		{
			public Dictionary<string, int> bundleGuidPK;
			public Dictionary<string, int> levelIdPK;
			public int[][] runHistory;
		}

		public class GetUserHistoryResult : AngryResult<GetUserHistoryResponse, GetUserHistoryStatus>
		{

		}

		public static async Task<GetUserHistoryResult> GetUserHistoryTask(string targetId, CancellationToken cancellationToken = default)
		{
			GetUserHistoryResult result = new GetUserHistoryResult();
			string url = AngryPaths.SERVER_ROOT + $"/leaderboards/getUserHistory?targetId={targetId}";

			await AngryRequest.MakeRequestWithToken(url, result, GetUserHistoryStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetUserHistoryStatus.FAILED;
			return result;
		}
		#endregion
	}
}
