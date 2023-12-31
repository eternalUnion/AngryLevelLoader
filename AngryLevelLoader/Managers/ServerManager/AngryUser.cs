﻿using Newtonsoft.Json;
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
using static AngryLevelLoader.Managers.ServerManager.AngryVotes;

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
			FAILED = -2,
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
            public TokengenStatus status = TokengenStatus.FAILED;

            public TokenGenResponse response;
		}

		private static Task<TokenGenResult> currentTokenGenTask = null;
		private static async Task<TokenGenResult> TokenGenTask()
        {
			TokenGenResult result = new TokenGenResult();

			try
            {
				AuthTicket ticketTask = await SteamUser.GetAuthSessionTicketAsync(new Steamworks.Data.NetIdentity());
                if (ticketTask == null)
                {
                    result.networkError = true;
                    return result;
                }

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
            catch (Exception e)
            {
                Plugin.logger.LogError($"Failed to obtain user token\n{e}");
                result.networkError = true;
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
			FAILED = -2,
			RATE_LIMITED = -1,
            OK = 0,
			INVALID_TOKEN = 1,
		}

		public class UserInfoData
		{
			public string[] upvotedBundles;
			public string[] downvotedBundles;
		}

		public class UserInfoResponse : AngryResponse
        {
            public UserInfoData info;
		}

        public class UserInfoResult : AngryResult<UserInfoResponse, UserInfoStatus>
        {

		}

        public static async Task<UserInfoResult> GetUserInfo(CancellationToken cancellationToken = default)
        {
			UserInfoResult result = new UserInfoResult();
            string url = AngryPaths.SERVER_ROOT + $"/user/info?";

            await AngryRequest.MakeRequestWithToken(url, result, UserInfoStatus.INVALID_TOKEN, cancellationToken);

            result.completed = true;
            if (!result.completedSuccessfully)
                result.status = UserInfoStatus.FAILED;
            return result;
		}
		#endregion

		#region Report
        public enum ReportStatus
        {
            FAILED = -2,
            RATE_LIMITED = -1,
            OK = 0,

			INVALID_TOKEN = 1,
            BANNED = 2,
            INVALID_TARGET_ID = 3,
            INVALID_ENTRY = 4,
		}

        public class ReportResponse : AngryResponse
        {
            public bool alreadySent { get; set; }
		}

        public class ReportResult : AngryResult<ReportResponse, ReportStatus>
        {

        }

		public static async Task<ReportResult> ReportTask(string category, string difficulty, string bundleGuid, string levelId, string targetId, string reason, CancellationToken cancellationToken = default)
		{
			ReportResult result = new ReportResult();

			string url = AngryPaths.SERVER_ROOT + $"/user/report?category={category}&difficulty={difficulty}&bundleGuid={bundleGuid}&levelId={levelId}&targetId={targetId}&reason={reason}";
			await AngryRequest.MakeRequestWithToken(url, result, ReportStatus.INVALID_TOKEN, cancellationToken);

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = ReportStatus.FAILED;
			return result;
		}
		#endregion
	}
}
