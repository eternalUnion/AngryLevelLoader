namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class WipFixHardBan : SoftBan
	{
		public override string ModGuid => "maranara_whipfix";

		public override string ModName => "Whiplash buff";

		public override SoftBanCheckResult Check()
		{
			return new SoftBanCheckResult(true, "This mod is not allowed in the leaderboards, unload to be able to post records");
		}
	}
}
