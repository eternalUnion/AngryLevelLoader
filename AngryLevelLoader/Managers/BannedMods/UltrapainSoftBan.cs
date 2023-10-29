using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class UltrapainSoftBan
	{
		public const string PLUGIN_GUID = "com.eternalUnion.ultraPain";

		public static bool UltrapainLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}
		
		public static SoftBanCheckResult Check()
		{
			if (Ultrapain.Plugin.ultrapainDifficulty)
				return new SoftBanCheckResult(true, "Ultrapain is not allowed in the leaderboards") { pluginName = "UltraPain" };

			return new SoftBanCheckResult() { pluginName = "UltraPain" };
		}
	}
}
