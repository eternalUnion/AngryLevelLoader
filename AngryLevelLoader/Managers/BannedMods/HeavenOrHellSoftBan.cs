using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class HeavenOrHellSoftBan
	{
		public const string PLUGIN_GUID = "com.heaven.orhell";

		public static bool HeavenOrHellLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			if (MyCoolMod.Plugin.isHeavenOrHell)
				return new SoftBanCheckResult(true, "Heaven or hell difficulty is not allowed in the leaderboards, switch to another difficulty to be able to post records");

			return new SoftBanCheckResult();
		}
	}
}
