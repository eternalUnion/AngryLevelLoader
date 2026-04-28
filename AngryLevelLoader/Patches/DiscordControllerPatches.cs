using HarmonyLib;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(DiscordController))]
	internal static class DiscordControllerPatches
	{
		[HarmonyPatch(nameof(DiscordController.FetchSceneActivity))]
		[HarmonyPrefix]
		public static bool FetchSceneActivityOverwrite()
		{
			SceneHelperPatches.forceDisableIsInCustomLevel = true;
			return true;
		}

		[HarmonyPatch(nameof(DiscordController.FetchSceneActivity))]
		[HarmonyPostfix]
		public static void PostFetchSceneActivityOverwrite()
		{
			SceneHelperPatches.forceDisableIsInCustomLevel = false;
		}
	}
}
