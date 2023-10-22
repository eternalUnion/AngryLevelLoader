using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
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
			NETWORK_ERROR = -2,
			OK = 0,
			INVALID_TOKEN = 1,
			ACCESS_DENIED = 2,
			DECRYPTION_ERROR = 3,
			MISSING_COMMAND = 4
		}

		private class CommandResponse
		{
			public string message { get; set; }
			public int status { get; set; }
			public string result { get; set; }
		}

		public class CommandResult
		{
			public bool networkError = false;

			public string message;
			public CommandStatus status = CommandStatus.NETWORK_ERROR;
			public string result;
		}

		public static async Task<CommandResult> SendCommand(string cmd, bool tokenRequested = false)
		{
			CommandResult result = new CommandResult();
			bool invalidToken = string.IsNullOrEmpty(AngryUser.token);

			if (string.IsNullOrEmpty(CryptographyUtils.AdminPrivateKey))
			{
				result.message = "Angry failed to locate admin private key (env variable ANGRY_ADMIN_KEY)";
				result.status = CommandStatus.MISSING_KEY;
				return result;
			}

			if (!invalidToken)
			{
				string encryptedToken = Convert.ToBase64String(CryptographyUtils.Encrypt(AngryUser.token, CryptographyUtils.AdminPrivateKey));
				UnityWebRequest req = new UnityWebRequest(AngryPaths.SERVER_ROOT + $"/admin/command?steamId={AngryUser.steamId}&token={encryptedToken}&cmd={cmd}");
				req.downloadHandler = new DownloadHandlerBuffer();
				await req.SendWebRequest();

				if (req.isNetworkError || req.isHttpError)
				{
					result.networkError = true;
					return result;
				}

				CommandResponse response = JsonConvert.DeserializeObject<CommandResponse>(req.downloadHandler.text);
				
				if (response.status == (int)CommandStatus.INVALID_TOKEN)
				{
					invalidToken = true;
				}
				else
				{
					result.message = response.message;
					result.status = (CommandStatus)response.status;
					if (result.status == CommandStatus.OK)
						result.result = response.result;

					return result;
				}
			}
		
			if (invalidToken)
			{
				result.message = "Angry failed to obtain a valid token";
				result.status = CommandStatus.INVALID_TOKEN;

				if (tokenRequested)
					return result;

				AngryUser.TokenGenResult tokenRes = await AngryUser.GenerateToken();

				if (tokenRes.networkError || tokenRes.status != AngryUser.TokengenStatus.OK)
					return result;

				return await SendCommand(cmd, true);
			}

			return result;
		}
		#endregion
	}
}
