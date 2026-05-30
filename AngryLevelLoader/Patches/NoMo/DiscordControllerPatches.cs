using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(DiscordController))]
	internal static class DiscordControllerPatches
	{
		[HarmonyPatch(nameof(DiscordController.FetchSceneActivity))]
		[HarmonyPrefix]
		private static void OverwriteDifficultyNamePrefix(out string __state)
		{
			__state = null;
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return;

			if (PresenceController.Instance == null || PresenceController.Instance.diffNames == null || PresenceController.Instance.diffNames.Length == 0)
				return;

			__state = PresenceController.Instance.diffNames[0];
			PresenceController.Instance.diffNames[0] = (AngryGamemodeManager.NoWeapons) ? "NOMOW" : "NOMO";
		}

		[HarmonyPatch(nameof(DiscordController.FetchSceneActivity))]
		[HarmonyPostfix]
		private static void OverwriteDifficultyNamePostfix(string __state)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters || __state == null)
				return;

			if (PresenceController.Instance == null || PresenceController.Instance.diffNames == null || PresenceController.Instance.diffNames.Length == 0)
				return;

			PresenceController.Instance.diffNames[0] = __state;
		}
	}
}
