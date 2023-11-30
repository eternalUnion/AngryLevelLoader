using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(StatueActivator))]
	public static class StatueActivatorPatches
	{
		[HarmonyPatch(nameof(StatueActivator.Start))]
		[HarmonyPrefix]
		public static bool PreventStatueActivation()
		{
			if (!AngrySceneManager.isInCustomLevel || !Plugin.NoMonsters)
				return true;

			return false;
		}
	}
}
