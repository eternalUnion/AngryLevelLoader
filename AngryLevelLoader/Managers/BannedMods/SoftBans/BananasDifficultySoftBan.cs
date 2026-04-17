using BepInEx.Bootstrap;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	public class BananasDifficultySoftBan : SoftBan
	{
		public override string ModGuid => "com.banana.BananaDifficulty";

		public override string ModName => "Banana Difficulty";

		public override SoftBanCheckResult Check()
		{
			if (BananaDifficulty.BananaDifficultyPlugin.CanUseIt(-1))
				return new SoftBanCheckResult(true, "Bananas difficulty is not allowed in the leaderboards, turn off global difficulty and switch to other difficulties to be able to post records");

			return new SoftBanCheckResult();
		}
	}
}
