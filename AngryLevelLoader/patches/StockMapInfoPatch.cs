using HarmonyLib;
using RudeLevelScripts.Essentials;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.patches
{
	/*
	 * Stealing script execution order
	 */

	[HarmonyPatch(typeof(StockMapInfo), nameof(StockMapInfo.Awake))]
	public static class StockMapInfoPatch
	{
		[HarmonyPostfix]
		public static void Postfix()
		{
			foreach (ExecuteOnSceneLoad obj in UnityEngine.Object.FindObjectsOfType<ExecuteOnSceneLoad>())
				obj.Execute();
		}
	}
}
