using Newtonsoft.Json;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AngryLevelLoader.Managers.ServerManager
{
    public static class AngryUser
    {
        // Angry uses the access token in queries of most of the api requests
        internal static string token = "";
        // Steam id is also used in the requests
        internal static string steamId = "";

        #region Token Gen
        public enum TokengenStatus
        {
			NETWORK_ERROR = -2,
			RATE_LIMITED = -1,
            OK = 0,
            INVALID_TICKET = 1,
            INVALID_APP_ID = 2,
            TICKET_EXPIRED = 3,
        }

		public class TokenGenResponse
		{
			public string message { get; set; }
			public int status { get; set; }
			public string steamId { get; set; }
			public string token { get; set; }
		}

		public class TokenGenResult
        {
            public bool networkError = false;
            public bool httpError = false;
            public TokengenStatus status = TokengenStatus.NETWORK_ERROR;

            public TokenGenResponse response;
		}

		private static Task<TokenGenResult> currentTokenGenTask = null;
		private static async Task<TokenGenResult> TokenGenTask()
        {
            try
            {
				AuthTicket ticketTask = await SteamUser.GetAuthSessionTicketAsync();

                static string ByteArrayToString(byte[] ba)
                {
                    StringBuilder hex = new StringBuilder(ba.Length * 2);
                    foreach (byte b in ba)
                        hex.AppendFormat("{0:x2}", b);
                    return hex.ToString();
                }

                string ticket = ByteArrayToString(ticketTask.Data);

                UnityWebRequest tokenReq = new UnityWebRequest(AngryPaths.SERVER_ROOT + $"/user/tokengen?ticket={ticket}");
                tokenReq.downloadHandler = new DownloadHandlerBuffer();
                await tokenReq.SendWebRequest();

                TokenGenResult result = new TokenGenResult();

				if (tokenReq.isNetworkError)
                {
					result.networkError = true;
                    return result;
                }
                if (tokenReq.isHttpError)
                {
                    result.httpError = true;
                    return result;
                }

                TokenGenResponse response = JsonConvert.DeserializeObject<TokenGenResponse>(tokenReq.downloadHandler.text);

				result.response = response;
				result.status = (TokengenStatus)response.status;

                if (result.status == TokengenStatus.OK)
                {
                    steamId = response.steamId;
                    token = response.token;
                }
                return result;
            }
            finally
            {
                currentTokenGenTask = null;
			}
        }

        public static Task<TokenGenResult> GenerateToken()
        {
            if (currentTokenGenTask != null)
                return currentTokenGenTask;

            token = "";
            steamId = "";
            currentTokenGenTask = TokenGenTask();

            return currentTokenGenTask;
		}
		#endregion

		#region User Info
        public enum UserInfoStatus
        {
			NETWORK_ERROR = -2,
			RATE_LIMITED = -1,
            OK = 0,
			INVALID_TOKEN = 1,
		}

		public class UserInfoData
		{
			public string[] upvotedBundles;
			public string[] downvotedBundles;
		}

		public class UserInfoResponse
        {
            public string message { get; set; }
            public int status { get; set; }
            public UserInfoData info;
		}

        public class UserInfoResult
        {
            public bool networkError = false;
            public bool httpError = false;
            public UserInfoStatus status = UserInfoStatus.NETWORK_ERROR;

            public UserInfoResponse response;
		}

        public static async Task<UserInfoResult> GetUserInfo(CancellationToken cancellationToken = default(CancellationToken), bool tokenRequested = false)
        {
			UserInfoResult result = new UserInfoResult();
            bool invalidToken = string.IsNullOrEmpty(token);

            if (!invalidToken)
            {
				UnityWebRequest req = new UnityWebRequest(AngryPaths.SERVER_ROOT + $"/user/info?steamId={steamId}&token={token}");
                req.downloadHandler = new DownloadHandlerBuffer();
                cancellationToken.Register(() =>
                {
                    if (!req.isDone)
                        req.Abort();
                });
                await req.SendWebRequest();

                if (cancellationToken.IsCancellationRequested)
                {
                    return result;
                }

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

                UserInfoResponse response = JsonConvert.DeserializeObject<UserInfoResponse>(req.downloadHandler.text);
                result.response = response;
                result.status = (UserInfoStatus)response.status;

                if (result.status == UserInfoStatus.INVALID_TOKEN)
                {
                    invalidToken = true;
                }
                else
                {
                    return result;
                }
            }

            if (invalidToken)
            {
				result.status = UserInfoStatus.INVALID_TOKEN;

                if (tokenRequested)
                    return result;

                var generatedToken = await GenerateToken();

				if (generatedToken.networkError || generatedToken.httpError || generatedToken.status != TokengenStatus.OK)
					return result;

                return await GetUserInfo(cancellationToken, true);
			}

            return result;
		}
		#endregion
	}
}
