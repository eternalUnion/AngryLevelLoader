using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(Bonus))]
	class BonusPatches
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
