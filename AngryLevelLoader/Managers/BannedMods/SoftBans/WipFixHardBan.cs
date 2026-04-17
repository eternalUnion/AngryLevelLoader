using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	public class WipFixHardBan : SoftBan
	{
		public override string ModGuid => "maranara_whipfix";

		public override string ModName => "Whiplash buff";

		public override SoftBanCheckResult Check()
		{
			return new SoftBanCheckResult(true, "This mod is not allowed in the leaderboards, unload to be able to post records");
		}
	}
}
