using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(StatueActivator))]
	internal static class StatueActivatorPatches
	{
		[HarmonyPatch(nameof(StatueActivator.Start))]
		[HarmonyPrefix]
		public static bool PreventStatueActivation()
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			return false;
		}
	}
}
