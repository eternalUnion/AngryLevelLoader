using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(FakeMassActivator))]
	internal static class FakeMassActivatorPatches
	{
		[HarmonyPatch(nameof(FakeMassActivator.OnEnable))]
		[HarmonyPrefix]
		private static bool InstantActivation(FakeMassActivator __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			MassAnimationReceiver mass = __instance.transform.parent.GetComponentInChildren<MassAnimationReceiver>();
			mass.SpawnMass();

			__instance.enabled = false;
			return false;
		}
	}
}
