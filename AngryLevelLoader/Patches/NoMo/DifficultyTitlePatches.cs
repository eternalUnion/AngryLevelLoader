using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(DifficultyTitle))]
	public static class DifficultyTitlePatches
	{
		[HarmonyPatch(nameof(DifficultyTitle.Check))]
		[HarmonyPostfix]
		public static void OverrideTitle(DifficultyTitle __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return;

			if (Plugin.difficultyField.gamemodeListValueIndex == 1)
				__instance.txt.text = __instance.lines ? "-- NO MONSTERS --" : "NO MONSTERS";
			else if (Plugin.difficultyField.gamemodeListValueIndex == 2)
				__instance.txt.text = __instance.lines ? "-- NO MONSTERS AND WEAPONS --" : "NO MONSTERS AND WEAPONS";
		}
	}
}
