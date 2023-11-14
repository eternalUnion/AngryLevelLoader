using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(SteamController))]
	public static class SteamControllerPatches
	{
		[HarmonyPatch(nameof(SteamController.FetchSceneActivity))]
		[HarmonyPrefix]
		public static bool FetchSceneActivityOverwrite()
		{
			SceneHelperPatches.forceDisableIsInCustomLevel = true;
			return true;
		}

		[HarmonyPatch(nameof(SteamController.FetchSceneActivity))]
		[HarmonyPostfix]
		public static void PostFetchSceneActivityOverwrite()
		{
			SceneHelperPatches.forceDisableIsInCustomLevel = false;
		}
	}
}
