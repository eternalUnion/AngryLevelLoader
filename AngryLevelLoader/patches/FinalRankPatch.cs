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


	[HarmonyPatch(typeof(FinalRank), nameof(FinalRank.CountSecrets))]
	class FinalRank_CountSecrets_Patch
	{
		static bool Prefix(FinalRank __instance)
		{
			if (!Plugin.isInCustomScene)
				return true;

			Plugin.currentLevelContainer.AssureSecretsSize();
			if (Plugin.currentLevelContainer.secrets.value[__instance.secretsCheckProgress] != 'T')
			{
				__instance.secretsInfo[__instance.secretsCheckProgress].color = Color.black;
				__instance.secretsCheckProgress += 1;

				if (__instance.secretsCheckProgress < __instance.levelSecrets.Length)
				{
					__instance.Invoke("CountSecrets", __instance.timeBetween);
					return false;
				}
				__instance.Invoke("Appear", __instance.timeBetween);
				return false;
			}

			return true;
		}
	}
}
