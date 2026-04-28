using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(DifficultyTitle))]
	internal static class DifficultyTitlePatches
	{
		[HarmonyPatch(nameof(DifficultyTitle.Check))]
		[HarmonyPostfix]
		public static void OverrideTitle(DifficultyTitle __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return;

			string text = null;
			if (AngryGamemodeManager.NoWeapons)
				text = __instance.lines ? "-- NO MONSTERS AND WEAPONS --" : "NO MONSTERS AND WEAPONS";
			else if (AngryGamemodeManager.NoMonsters)
				text = __instance.lines ? "-- NO MONSTERS --" : "NO MONSTERS";

			if (text == null)
				return;

			if (__instance.txt2 != null)
				__instance.txt2.text = text;
		}
	}
}
