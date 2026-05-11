namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class BananasDifficultySoftBan : SoftBan
	{
		public override string ModGuid => "com.banana.BananaDifficulty";

		public override string ModName => "Banana Difficulty";

		public override SoftBanCheckResult Check()
		{
			if (BananaDifficulty.BananaDifficultyPlugin.CanUseIt(-1))
				return new SoftBanCheckResult(true, "Bananas difficulty is not allowed in the leaderboards, switch to other difficulties to be able to post records");

			return new SoftBanCheckResult();
		}
	}
}
