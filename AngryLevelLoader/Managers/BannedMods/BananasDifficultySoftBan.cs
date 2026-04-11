using BepInEx.Bootstrap;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class BananasDifficultySoftBan
	{
		public const string PLUGIN_GUID = "com.banana.BananaDifficulty";

		public static bool BananasLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			if (BananaDifficulty.BananaDifficultyPlugin.CanUseIt(-1))
				return new SoftBanCheckResult(true, "Bananas difficulty is not allowed in the leaderboards, turn off global difficulty and switch to other difficulties to be able to post records");

			return new SoftBanCheckResult();
		}
	}
}
