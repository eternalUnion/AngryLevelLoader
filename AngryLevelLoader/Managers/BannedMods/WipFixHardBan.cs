using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class WipFixHardBan
	{
		public const string PLUGIN_GUID = "maranara_whipfix";

		public static bool WipFixLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		// Amount of coins is not configurable
		public static SoftBanCheckResult Check()
		{
			return new SoftBanCheckResult(true, "This mod is not allowed in the leaderboards, unload to be able to post records");
		}
	}
}
