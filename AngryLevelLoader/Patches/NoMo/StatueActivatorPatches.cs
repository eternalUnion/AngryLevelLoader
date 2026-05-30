using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(StatueActivator))]
	internal static class StatueActivatorPatches
	{
		[HarmonyPatch(nameof(StatueActivator.Start))]
		[HarmonyPrefix]
		private static bool DoNotActivate(StatueActivator __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			if (__instance.transform.parent == null)
				return true;

			StatueFake statueFake = __instance.transform.parent.GetComponentInChildren<StatueFake>();
			if (statueFake == null)
				return false;

			if (statueFake.onFirstCrack != null)
			{
				try
				{
					statueFake.onFirstCrack.Invoke();
				}
				catch (Exception e)
				{
					Plugin.logger.LogError(e);
				}
			}

			if (statueFake.onComplete != null)
			{
				try
				{
					statueFake.onComplete.Invoke();
				}
				catch (Exception e)
				{
					Plugin.logger.LogError(e);
				}
			}

			return false;
		}
	}
}
