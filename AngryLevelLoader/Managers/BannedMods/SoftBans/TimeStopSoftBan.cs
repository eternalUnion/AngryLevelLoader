using System.Reflection;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class TimeStopSoftBan : SoftBan
	{
		private static bool currentlyBanned = false;

		public override string ModGuid => "com.banana.timestop";

		public override string ModName => "TimeStop";

		private static void OnStopTime()
		{
			if (AngrySceneManager.isInCustomLevel && TimeStop.Main.IsActive)
				currentlyBanned = true;
		}

		public override void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				currentlyBanned = TimeStop.Main.IsActive;
			};

			Plugin.harmony.Patch(typeof(TimeStop.Main).GetMethod(nameof(TimeStop.Main.ToggleTimeStop), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
				postfix: new HarmonyLib.HarmonyMethod(typeof(TimeStopSoftBan).GetMethod(nameof(OnStopTime), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
		}

		public override SoftBanCheckResult Check()
		{
			if (currentlyBanned)
				return new SoftBanCheckResult(true, "Time was stopped while playing the level!");

			return new SoftBanCheckResult();
		}
	}
}
