namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class BillionDifficultySoftBan : SoftBan
	{
		public override string ModGuid => "billy.billiondifficulty";

		public override string ModName => "Billion Difficulty";

		public override SoftBanCheckResult Check()
		{
			if (BillionDifficulty.Util.IsDifficulty(19))
				return new SoftBanCheckResult(true, "Billion difficulty is not allowed in the leaderboards, turn off global difficulty and switch to other difficulties to be able to post records");

			return new SoftBanCheckResult();
		}
	}
}
