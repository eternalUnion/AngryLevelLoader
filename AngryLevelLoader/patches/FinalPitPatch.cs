using HarmonyLib;
using RudeLevelScript;

namespace AngryLevelLoader.patches
{
	[HarmonyPatch(typeof(FinalPit), nameof(FinalPit.SendInfo))]
	public static class FinalPit_SendInfo_Patch
	{
		public static FinalRoomTarget lastTarget = null;

		[HarmonyPrefix]
		public static bool Prefix(FinalPit __instance)
		{
			lastTarget = __instance.transform.parent.GetComponentInParent<FinalRoomTarget>();
			return true;
		}
	}
}
