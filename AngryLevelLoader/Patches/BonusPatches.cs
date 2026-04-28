using HarmonyLib;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(Bonus))]
	internal class BonusPatches
	{
		internal static Bonus lastCaller = null;

		[HarmonyPatch(nameof(Bonus.OnTriggerEnter))]
		[HarmonyPrefix]
		private static void OnTriggerEnter(Bonus __instance)
		{
			lastCaller = __instance;
		}
	}
}
