using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
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
			NETWORK_ERROR = -2,
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

		public class GetRecordsResponse
		{
			public string message { get; set; }
			public int status { get; set; }
			public int offset { get; set; }
			public RecordInfo[] records;
			public int totalCount { get; set; }
		}

		public class GetRecordsResult
		{
			public bool networkError = false;
			public bool httpError = false;

			public GetRecordsStatus status = GetRecordsStatus.NETWORK_ERROR;
			public GetRecordsResponse response;
		}

		public static async Task<GetRecordsResult> GetRecordsTask(RecordCategory category, RecordDifficulty difficulty, string bundleGuid, string levelId, int offset, int count)
		{
			GetRecordsResult result = new GetRecordsResult();

			UnityWebRequest req = new UnityWebRequest(AngryPaths.SERVER_ROOT + $"/leaderboards/getRecords?category={RECORD_CATEGORY_DICT[category]}&difficulty={RECORD_DIFFICULTY_DICT[difficulty]}&bundleGuid={bundleGuid}&levelId={levelId}&offset={offset}&count={count}");
			req.downloadHandler = new DownloadHandlerBuffer();
			await req.SendWebRequest();

			if (req.isNetworkError)
			{
				result.networkError = true;
				return result;
			}
			if (req.isHttpError)
			{
				result.httpError = true;
				return result;
			}

			GetRecordsResponse response = JsonConvert.DeserializeObject<GetRecordsResponse>(req.downloadHandler.text);

			result.response = response;
			result.status = (GetRecordsStatus)response.status;
			return result;
		}
		#endregion
	}
}
