using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.patches
{
	[HarmonyPatch(typeof(FinalRank), nameof(FinalRank.Start))]
	class FinalRank_Start_Patch
	{
		[HarmonyPrefix]
		static bool Prefix(FinalRank __instance)
		{
			if (!Plugin.isInCustomScene)
				return true;

			__instance.levelSecrets = StatsManager.instance.secretObjects;
			if (__instance.levelSecrets.Length != Plugin.currentLevelData.secretCount)
			{
				Debug.LogWarning($"Inconsistent secrets size, expected {Plugin.currentLevelData.secretCount}, found {__instance.levelSecrets.Length}");
				__instance.levelSecrets = new GameObject[Plugin.currentLevelData.secretCount];
			}

			return true;
		}
	}
}
