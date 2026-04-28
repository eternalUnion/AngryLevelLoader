using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class FasterPunchSoftBan : SoftBan
	{
		private static bool currentlyBanned = false;

		public override string ModGuid => "ironfarm.uk.muda";

		public override string ModName => "Faster Punch";

		public override void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				currentlyBanned = false;
			};

			FasterPunch.ConfigManager.StandardEnabled.onValueChange += (e) =>
			{
				if (e.value)
					currentlyBanned = true;
			};

			FasterPunch.ConfigManager.HeavyEnabled.onValueChange += (e) =>
			{
				if (e.value)
					currentlyBanned = true;
			};

			FasterPunch.ConfigManager.HookEnabled.onValueChange += (e) =>
			{
				if (e.value)
					currentlyBanned = true;
			};

			FasterPunch.ConfigManager.ParryUpDamage.onValueChange += (e) =>
			{
				if (e.value)
					currentlyBanned = true;
			};
		}

		public override SoftBanCheckResult Check()
		{
			SoftBanCheckResult result = new SoftBanCheckResult();

			if (FasterPunch.ConfigManager.StandardEnabled.value)
			{
				result.banned = true;
				result.message = "- Fast feedbacker is banned, disable from settings to be able to post records";
			}

			if (FasterPunch.ConfigManager.HeavyEnabled.value)
			{
				result.banned = true;
				if (!string.IsNullOrEmpty(result.message))
					result.message += '\n';
				result.message += "- Fast knuckleblaster is banned, disable from settings to be able to post records";
			}

			if (FasterPunch.ConfigManager.HookEnabled.value)
			{
				result.banned = true;
				if (!string.IsNullOrEmpty(result.message))
					result.message += '\n';
				result.message += "- Fast whiplash is banned, disable from settings to be able to post records";
			}

			if (FasterPunch.ConfigManager.ParryUpDamage.value)
			{
				result.banned = true;
				if (!string.IsNullOrEmpty(result.message))
					result.message += '\n';
				result.message += "- Parry damage buff is banned, disable from settings to be able to post records";
			}

			if (currentlyBanned)
			{
				result.banned = true;
				if (!string.IsNullOrEmpty(result.message))
					result.message += '\n';
				result.message += "- You enabled the mod at least once during the level, restart the level with the mod disabled in the plugin config";
			}

			return result;
		}
	}
}
