using HarmonyLib;
using RudeLevelScripts.Essentials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
			foreach (ExecuteOnSceneLoad obj in UnityEngine.Object.FindObjectsOfType<ExecuteOnSceneLoad>().OrderBy(exe => exe.relativeExecutionOrder))
			{
				try
				{
					obj.Execute();
				}
				catch (Exception e)
				{
					Debug.LogError($"Error while executing OnSceneLoad script for {obj.gameObject.name}: {e}");
				}
			}
		}
	}
}
