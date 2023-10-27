using AngryLevelLoader.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace AngryLevelLoader.Managers.ServerManager
{
	public static class AngryVotes
	{
		#region Get All Votes
		public enum GetAllVotesStatus
		{
			NETWORK_ERROR = -2,
			RATE_LIMITED = -1,
			GET_ALL_VOTES_OK = 0
		}

		public class GetAllVotesBundleInfo
		{
			public int upvotes { get; set; }
			public int downvotes { get; set; }
		}

		public class GetAllVotesResponse : AngryResponse
		{
			public Dictionary<string, GetAllVotesBundleInfo> bundles;
		}

		public class GetAllVotesResult : AngryResult<GetAllVotesResponse, GetAllVotesStatus>
		{

		}

		public static async Task<GetAllVotesResult> GetAllVotesTask(CancellationToken cancellationToken = default)
		{
			GetAllVotesResult result = new GetAllVotesResult();
			string url = AngryPaths.SERVER_ROOT + $"/votes";

			await AngryRequest.MakeRequest(url, result, cancellationToken);

			result.completed = true;
			return result;
		}
		#endregion

		#region Vote
		public const string VOTE_OP_UPVOTE = "upvote";
		public const string VOTE_OP_DOWNVOTE = "downvote";
		public const string VOTE_OP_CLEAR = "clear";

		public enum VoteOperation
		{
			UPVOTE,
			DOWNVOTE,
			CLEAR,
			UNKNOWN
		}

		public enum VoteStatus
		{
			NETWORK_ERROR = -2,
			RATE_LIMITED = -1,
			VOTE_OK = 0,
			VOTE_INVALID_TOKEN = 1,
			VOTE_INVALID_BUNDLE = 2,
			VOTE_INVALID_OPERATION = 3,
		}

		public class VoteResponse : AngryResponse
		{
			public string bundleGuid { get; set; }
			public string operation { get; set; }
			public int upvotes { get; set; }
			public int downvotes { get; set; }
		}

		public class VoteResult : AngryResult<VoteResponse, VoteStatus>
		{
			public VoteOperation operation;
		}

		public static async Task<VoteResult> VoteTask(string bundleGuid, VoteOperation operation, CancellationToken cancellationToken = default)
		{
			VoteResult result = new VoteResult();

			string op = VOTE_OP_CLEAR;
			if (operation == VoteOperation.UPVOTE)
				op = VOTE_OP_UPVOTE;
			else if (operation == VoteOperation.DOWNVOTE)
				op = VOTE_OP_DOWNVOTE;

			string url = AngryPaths.SERVER_ROOT + $"/user/vote?bundleGuid={bundleGuid}&op={op}";
			await AngryRequest.MakeRequestWithToken(url, result, VoteStatus.VOTE_INVALID_TOKEN, cancellationToken);

			result.operation = VoteOperation.CLEAR;
			if (result.completedSuccessfully && result.response != null)
			{
				if (result.response.operation == VOTE_OP_UPVOTE)
					result.operation = VoteOperation.UPVOTE;
				else if (result.response.operation == VOTE_OP_DOWNVOTE)
					result.operation = VoteOperation.DOWNVOTE;
			}

			result.completed = true;
			return result;
		}
		#endregion
	}
}
