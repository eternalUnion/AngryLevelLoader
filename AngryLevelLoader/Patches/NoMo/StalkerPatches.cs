using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(Stalker))]
	internal static class StalkerPatches
	{
		[HarmonyPatch(nameof(Stalker.SandExplode))]
		[HarmonyPrefix]
		public static bool PreventSandExplodeOnNomo()
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			return false;
		}
	}
}
