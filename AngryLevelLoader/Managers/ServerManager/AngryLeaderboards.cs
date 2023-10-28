using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace AngryLevelLoader.Managers.ServerManager
{
	public static class AngryLeaderboards
	{
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
			public float time { get; set; }
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

		public static async Task<PostRecordResult> PostRecordTask(RecordCategory category, RecordDifficulty difficulty, string bundleGuid, string hash, string levelId, float time, CancellationToken cancellationToken = default)
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
			public float time { get; set; }
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
			public float time { get; set; }
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
	}
}
