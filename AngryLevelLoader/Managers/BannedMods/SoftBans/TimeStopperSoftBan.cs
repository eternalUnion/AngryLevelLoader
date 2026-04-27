using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class TimeStopperSoftBan : SoftBan
	{
		private static bool currentlyBanned = false;

		public override string ModGuid => "dev.galvin.timestopper";

		public override string ModName => "The Timestopper";

		private static void OnStopTime()
		{
			if (AngrySceneManager.isInCustomLevel)
				currentlyBanned = true;
		}

		public override void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				currentlyBanned = false;
			};

			Plugin.harmony.Patch(typeof(The_Timestopper.Timestopper).GetMethod(nameof(The_Timestopper.Timestopper.StopTime), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				postfix: new HarmonyLib.HarmonyMethod(typeof(TimeStopperSoftBan).GetMethod(nameof(OnStopTime), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
		}

		public override SoftBanCheckResult Check()
		{
			if (currentlyBanned)
				return new SoftBanCheckResult(true, "Time was stopped while playing the level!");

			return new SoftBanCheckResult();
		}
	}
}
