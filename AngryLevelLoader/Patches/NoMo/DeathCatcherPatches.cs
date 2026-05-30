using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UltraFunGuns.UKEvents;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(Deathcatcher))]
	internal static class DeathCatcherPatches
	{
		[HarmonyPatch(nameof(Deathcatcher.Start))]
		[HarmonyPostfix]
		private static void KillIfAlreadyOpen(Deathcatcher __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return;

			Transform openerTrans = (__instance.transform.parent == null) ? null : __instance.transform.parent.Find("Opener");
			if (openerTrans != null && openerTrans.gameObject.TryGetComponent(out ObjectActivator obac) && !obac.gameObject.activeSelf)
			{
				obac.events.onActivate.AddListener(() =>
				{
					NoMoCommon.TriggerEnemyEvents(__instance.gameObject);
					UnityEngine.Object.Destroy(__instance.gameObject);
				});
			}
			else
			{
				NoMoCommon.TriggerEnemyEvents(__instance.gameObject);
				UnityEngine.Object.Destroy(__instance.gameObject);
			}
		}
	}
}
