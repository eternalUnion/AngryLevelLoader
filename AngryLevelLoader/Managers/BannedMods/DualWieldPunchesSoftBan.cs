using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class DualWieldPunchesSoftBan
	{
		public const string PLUGIN_GUID = "DualPunches";

		public static bool DualWieldPunchesLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		// This mod has no configuration
		public static SoftBanCheckResult Check()
		{
			return new SoftBanCheckResult(true, "This mod is not allowed in the leaderboards, unload to be able to post records");
		}
	}
}
