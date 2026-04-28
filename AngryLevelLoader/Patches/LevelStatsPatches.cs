using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(LevelStats))]
	internal static class LevelStatsPatches
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
			__instance.gameObject.SetActive(true);
		}
	}
}
