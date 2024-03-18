using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AngryLevelLoader.Managers.ServerManager
{
	public static class AngryAdmin
	{
		#region Command
		public enum CommandStatus
		{
			MISSING_KEY = -3,
			FAILED = -2,
			OK = 0,
			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			DECRYPTION_ERROR = 3,
			MISSING_COMMAND = 4,
			VERIFICATION_ERROR = 5
		}

		public class CommandResponse : AngryResponse
		{
			public string result { get; set; }
		}

		public class CommandResult : AngryResult<CommandResponse, CommandStatus>
		{

		}

		public static async Task<CommandResult> SendCommand(string cmd, CancellationToken cancellationToken = default)
		{
			CommandResult result = new CommandResult();
			string url = AngryPaths.SERVER_ROOT + $"/admin/command?cmd={cmd}";

			await AngryRequest.MakeRequestWithAdminToken(url, result, CommandStatus.INVALID_TOKEN, CommandStatus.MISSING_KEY, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = CommandStatus.FAILED;
			return result;
		}
		#endregion

		#region Get All Level Info
		public enum GetAllLevelInfoStatus
		{
			MISSING_KEY = -3,
			FAILED = -2,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			DECRYPTION_ERROR = 3,
			VERIFICATION_ERROR = 4,
		}

		public class BundleLevelInfo
		{
			public string bundleGuid { get; set; }
			public string hash { get; set; }
			public string[] levels;
		}

		public class GetAllLevelInfoResponse : AngryResponse
		{
			public BundleLevelInfo[] result;
		}

		public class GetAllLevelInfoResult : AngryResult<GetAllLevelInfoResponse, GetAllLevelInfoStatus>
		{

		}

		public static async Task<GetAllLevelInfoResult> GetAllLevelInfoTask(CancellationToken cancellationToken = default)
		{
			GetAllLevelInfoResult result = new GetAllLevelInfoResult();
			string url = AngryPaths.SERVER_ROOT + $"/admin/getAllLevels?";

			await AngryRequest.MakeRequestWithAdminToken(url, result, GetAllLevelInfoStatus.INVALID_TOKEN, GetAllLevelInfoStatus.MISSING_KEY, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetAllLevelInfoStatus.FAILED;
			return result;
		}
		#endregion

		public class ReportObject
		{
			public string category { get; set; }
			public string difficulty { get; set; }
			public string bundleGuid { get; set; }
			public string levelId { get; set; }
			public int time { get; set; }
		}

		public class Report
		{
			public string sender { get; set; }
			public string targetId { get; set; }
			public string reason { get; set; }

			public ReportObject reportObject;
		}

		public class UserSentReportsInfo
		{
			public Report[] reports;
		}

		public class UserReceivedReportsInfo
		{
			public Report[] receivedReports;
		}

		#region Get Sent Reports
		public enum GetSentReportsStatus
		{
			MISSING_KEY = -3,
			FAILED = -2,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			DECRYPTION_ERROR = 3,
			VERIFICATION_ERROR = 4,
		}

		public class SentReportsResponse : AngryResponse
		{
			public Dictionary<string, UserSentReportsInfo> reports;
		}

		public class SentReportsResult : AngryResult<SentReportsResponse, GetSentReportsStatus>
		{

		}

		public static async Task<SentReportsResult> GetAllSentReportsTask(CancellationToken cancellationToken = default)
		{
			SentReportsResult result = new SentReportsResult();
			string url = AngryPaths.SERVER_ROOT + $"/admin/getAllSentReports?";

			await AngryRequest.MakeRequestWithAdminToken(url, result, GetSentReportsStatus.INVALID_TOKEN, GetSentReportsStatus.MISSING_KEY, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetSentReportsStatus.FAILED;
			return result;
		}
		#endregion

		#region Get Received Reports
		public enum GetReceivedReportsStatus
		{
			MISSING_KEY = -3,
			FAILED = -2,
			OK = 0,

			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			DECRYPTION_ERROR = 3,
			VERIFICATION_ERROR = 4,
		}

		public class ReceivedReportsResponse : AngryResponse
		{
			public Dictionary<string, UserReceivedReportsInfo> reports;
		}

		public class ReceivedReportsResult : AngryResult<ReceivedReportsResponse, GetReceivedReportsStatus>
		{

		}

		public static async Task<ReceivedReportsResult> GetAllReceivedReportsTask(CancellationToken cancellationToken = default)
		{
			ReceivedReportsResult result = new ReceivedReportsResult();
			string url = AngryPaths.SERVER_ROOT + $"/admin/getAllReceivedReports?";

			await AngryRequest.MakeRequestWithAdminToken(url, result, GetReceivedReportsStatus.INVALID_TOKEN, GetReceivedReportsStatus.MISSING_KEY, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = GetReceivedReportsStatus.FAILED;
			return result;
		}
		#endregion
	}
}
