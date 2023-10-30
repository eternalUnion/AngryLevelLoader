using BepInEx.Bootstrap;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
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

		private static BoolField globalDifficultySwitch;
		public static SoftBanCheckResult Check()
		{
			if (globalDifficultySwitch == null)
			{
				PluginConfigurator ultrapainConfig = PluginConfiguratorController.GetConfig(Ultrapain.Plugin.PLUGIN_GUID);

				if (ultrapainConfig != null)
					globalDifficultySwitch = ultrapainConfig.rootPanel["globalDifficultySwitch"] as BoolField;
			}

			if (Ultrapain.Plugin.ultrapainDifficulty || (globalDifficultySwitch != null && globalDifficultySwitch.value))
				return new SoftBanCheckResult(true, "Ultrapain is not allowed in the leaderboards, turn off global difficulty and switch to other difficulties to be able to post records");

			return new SoftBanCheckResult();
		}
	}
}
