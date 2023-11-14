using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(LevelStats))]
	public static class LevelStatsPatches
	{
		[HarmonyPatch(nameof(LevelStats.Start))]
		[HarmonyPostfix]
		public static void OverwriteTabName(LevelStats __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return;

			StockMapInfo mapInfo = StockMapInfo.Instance;
			if (mapInfo != null)
			{
				__instance.levelName.text = mapInfo.assets.LargeText;
			}
			else
			{
				__instance.levelName.text = "???";
			}

			__instance.ready = true;
			__instance.CheckStats();
		}
	}
}
