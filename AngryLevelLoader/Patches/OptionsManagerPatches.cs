using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(OptionsManager))]
	public static class OptionsManagerPatches
	{
		[HarmonyPatch(nameof(OptionsManager.UnPause))]
		[HarmonyPostfix]
		public static void MakeReloadPromptTransparent()
		{
			if (Plugin.currentPanel != null)
				Plugin.currentPanel.reloadBundlePrompt.MakeTransparent(false);
		}

		[HarmonyPatch(nameof(OptionsManager.Pause))]
		[HarmonyPostfix]
		public static void MakeReloadPromptOpaque()
		{
			if (Plugin.currentPanel != null)
				Plugin.currentPanel.reloadBundlePrompt.MakeOpaque(false);
		}
	}
}
