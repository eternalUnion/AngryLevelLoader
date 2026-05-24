using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AngryLevelLoader.Managers.ServerManager
{
	public static class AngryVotes
	{
		#region Get All Votes
		public enum GetAllVotesStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			GET_ALL_VOTES_OK = 0
		}

		public class GetAllVotesBundleInfo
		{
			public int upvotes { get; set; }
			[Obsolete("Downvotes are no longer supported. This field will always be 0.")]
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
			if (!result.completedSuccessfully)
				result.status = GetAllVotesStatus.FAILED;
			return result;
		}
		#endregion

		#region Vote
		internal const string VOTE_OP_UPVOTE = "upvote";
		[Obsolete("This operation is no longer supported. VoteOperation.CLEAR will be executed instead.")]
		internal const string VOTE_OP_DOWNVOTE = "downvote";
		internal const string VOTE_OP_CLEAR = "clear";

		internal enum VoteOperation
		{
			UPVOTE,
			[Obsolete("This operation is no longer supported. VoteOperation.CLEAR will be executed instead.")]
			DOWNVOTE,
			CLEAR,
			UNKNOWN
		}

		internal enum VoteStatus
		{
			FAILED = -2,
			RATE_LIMITED = -1,
			VOTE_OK = 0,
			VOTE_INVALID_TOKEN = 1,
			VOTE_INVALID_BUNDLE = 2,
			VOTE_INVALID_OPERATION = 3,
		}

		internal class VoteResponse : AngryResponse
		{
			public string bundleGuid { get; set; }
			public string operation { get; set; }
			public int upvotes { get; set; }
			[Obsolete("Downvotes are no longer supported. This field will always be 0.")]
			public int downvotes { get; set; }
		}

		internal class VoteResult : AngryResult<VoteResponse, VoteStatus>
		{
			public VoteOperation operation;
		}

		internal static async Task<VoteResult> VoteTask(string bundleGuid, VoteOperation operation, CancellationToken cancellationToken = default)
		{
			VoteResult result = new VoteResult();

			string op = VOTE_OP_CLEAR;
			if (operation == VoteOperation.UPVOTE)
				op = VOTE_OP_UPVOTE;

			string url = AngryPaths.SERVER_ROOT + $"/user/vote?bundleGuid={bundleGuid}&op={op}";
			await AngryRequest.MakeRequestWithToken(url, result, VoteStatus.VOTE_INVALID_TOKEN, cancellationToken);

			result.operation = VoteOperation.CLEAR;
			if (result.completedSuccessfully && result.response != null)
			{
				if (result.response.operation == VOTE_OP_UPVOTE)
					result.operation = VoteOperation.UPVOTE;
			}

			result.completed = true;
			if (!result.completedSuccessfully)
				result.status = VoteStatus.FAILED;
			return result;
		}
		#endregion
	}
}
