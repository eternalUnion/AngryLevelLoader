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

			string text = null;
			if (Plugin.NoWeapons)
				text = __instance.lines ? "-- NO MONSTERS AND WEAPONS --" : "NO MONSTERS AND WEAPONS";
			else if (Plugin.NoMonsters)
				text = __instance.lines ? "-- NO MONSTERS --" : "NO MONSTERS";

			if (text == null)
				return;

			if (__instance.txt2 != null)
				__instance.txt2.text = text;
		}
	}
}
