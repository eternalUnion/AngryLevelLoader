using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(PowerIntro))]
	internal static class PowerIntroPatches
	{
		class ForceDisable : MonoBehaviour
		{
			void OnEnable()
			{
				gameObject.SetActive(false);
			}
		}

		[HarmonyPatch(nameof(PowerIntro.Start))]
		[HarmonyPrefix]
		private static bool SkipIntro(PowerIntro __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			foreach (MassAnimationReceiver fakePower in __instance.GetComponentsInChildren<MassAnimationReceiver>(true))
			{
				fakePower.gameObject.SetActive(false);
				fakePower.gameObject.AddComponent<ForceDisable>();
			}

			Power power = __instance.GetComponentInChildren<Power>(true);
			if (power != null)
			{
				power.gameObject.SetActive(true);
			}

			return false;
		}
	}
}
