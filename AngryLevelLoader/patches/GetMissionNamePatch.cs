using AngryLevelLoader.Managers;
using HarmonyLib;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(GetMissionName), nameof(GetMissionName.GetMission))]
	class GetMissionName_Patch
	{
		[HarmonyPrefix]
		static bool Prefix(ref string __result)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			__result = AngrySceneManager.currentLevelData.levelName;
			return false;
		}
	}
}
