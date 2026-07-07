using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using ULTRAKILL.Cheats;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(DisabledEnemiesChecker))]
	internal class DisabledEnemiesCheckerPatches
	{
		[HarmonyPatch(nameof(DisabledEnemiesChecker.Update))]
		[HarmonyPrefix]
		public static bool Update(DisabledEnemiesChecker __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			if (!__instance.activated && StatsManager.Instance && StatsManager.Instance.levelStarted)
			{
				__instance.activated = true;
				__instance.Invoke("Activate", __instance.delay);
			}

			return false;
		}
	}
}
