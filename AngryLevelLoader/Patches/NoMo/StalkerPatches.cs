using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(Stalker))]
	public static class StalkerPatches
	{
		[HarmonyPatch(nameof(Stalker.SandExplode))]
		[HarmonyPrefix]
		public static bool PreventSandExplodeOnNomo()
		{
			if (!AngrySceneManager.isInCustomLevel || !(Plugin.NoMo || Plugin.NoMoW))
				return true;

			return false;
		}
	}
}
