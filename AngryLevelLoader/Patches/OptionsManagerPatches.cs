using AngryLevelLoader.UserInterface;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(OptionsManager))]
	internal static class OptionsManagerPatches
	{
		[HarmonyPatch(nameof(OptionsManager.UnPause))]
		[HarmonyPostfix]
		public static void MakeReloadPromptTransparent()
		{
			AngryUI.MakeTransparent(true);
		}

		[HarmonyPatch(nameof(OptionsManager.Pause))]
		[HarmonyPostfix]
		public static void MakeReloadPromptOpaque()
		{
			AngryUI.MakeTransparent(false);
		}
	}
}
