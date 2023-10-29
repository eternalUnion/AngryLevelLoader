using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class UltraCoinsSoftBan
	{
		public const string PLUGIN_GUID = "ironfarm.uk.uc";

		public static bool UltraCoinsLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		// Amount of coins is not configurable
		public static SoftBanCheckResult Check()
		{
			return new SoftBanCheckResult(true, "UltraCoins is banned") { pluginName = "UltraCoins" };
		}
	}
}
