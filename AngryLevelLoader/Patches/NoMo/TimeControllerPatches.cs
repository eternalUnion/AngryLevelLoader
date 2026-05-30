using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(TimeController))]
	internal class TimeControllerPatches
	{
		[HarmonyPatch(nameof(TimeController.SlowDown))]
		[HarmonyPrefix]
		private static bool DoNotSlowDown()
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			return false;
		}
	}
}
