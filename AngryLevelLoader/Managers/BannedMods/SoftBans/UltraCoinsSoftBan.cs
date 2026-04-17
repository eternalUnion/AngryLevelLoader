using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	public class UltraCoinsSoftBan : SoftBan
	{
		private static bool currentlyBanned = false;

		public override string ModGuid => "ironfarm.uk.uc";

		public override string ModName => "UltraCoins";

		public override void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				currentlyBanned = false;
			};

			Ultracoins.ConfigManager.isEnabled.onValueChange += (e) =>
			{
				if (e.value)
					currentlyBanned = true;
			};
		}

		public override SoftBanCheckResult Check()
		{
			if (currentlyBanned)
			{
				return new SoftBanCheckResult(true, "UltraCoins was enabled while playing the level. Restart the level while UltraCoins is disabled in the config.");
			}

			if (Ultracoins.ConfigManager.isEnabled.value)
			{
				return new SoftBanCheckResult(true, "UltraCoins was enabled while playing the level. Restart the level while UltraCoins is disabled in the config.");
			}

			return new SoftBanCheckResult();
		}
	}
}
