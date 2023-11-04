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
	}
}
