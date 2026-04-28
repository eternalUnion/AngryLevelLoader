using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class UltrapainSoftBan : SoftBan
	{
		public override string ModGuid => "com.eternalUnion.ultraPain";

		public override string ModName => "UltraPain";

		private static BoolField globalDifficultySwitch;

		private static bool currentlyBanned = false;

		public override void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				currentlyBanned = false;
			};

			PluginConfigurator ultrapainConfig = PluginConfiguratorController.GetConfig(Ultrapain.Plugin.PLUGIN_GUID);

			if (ultrapainConfig != null)
				globalDifficultySwitch = ultrapainConfig.rootPanel["globalDifficultySwitch"] as BoolField;

			globalDifficultySwitch.onValueChange += (e) =>
			{
				if (e.value)
					currentlyBanned = true;
			};
		}

		public override SoftBanCheckResult Check()
		{
			if (globalDifficultySwitch == null)
			{
				PluginConfigurator ultrapainConfig = PluginConfiguratorController.GetConfig(Ultrapain.Plugin.PLUGIN_GUID);

				if (ultrapainConfig != null)
					globalDifficultySwitch = ultrapainConfig.rootPanel["globalDifficultySwitch"] as BoolField;
			}

			if (Ultrapain.Plugin.ultrapainDifficulty || globalDifficultySwitch != null && globalDifficultySwitch.value)
				return new SoftBanCheckResult(true, "Ultrapain is not allowed in the leaderboards, turn off global difficulty and switch to other difficulties to be able to post records");

			if (currentlyBanned)
				return new SoftBanCheckResult(true, "Ultrapain global difficulty tweaks were enabled, restart the level with the global difficulty tweaks turned off");

			return new SoftBanCheckResult();
		}
	}
}
