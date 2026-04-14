using BepInEx.Bootstrap;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class BillionDifficultySoftBan
	{
		public const string PLUGIN_GUID = "billy.billiondifficulty";

		public static bool BillionLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			if (BillionDifficulty.Util.IsHardMode())
				return new SoftBanCheckResult(true, "Billion difficulty is not allowed in the leaderboards, turn off global difficulty and switch to other difficulties to be able to post records");

			return new SoftBanCheckResult();
		}
	}
}
