using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.DataTypes
{
	public class AngryServerBundleVoteInfo
	{
		public int upvotes { get; set; }
		public int downvotes { get; set; }
	}

	public class AngryServerBundleVotes
	{
		public string message { get; set; }
		public int status { get; set; }
		public Dictionary<string, AngryServerBundleVoteInfo> bundles;
	}
}
