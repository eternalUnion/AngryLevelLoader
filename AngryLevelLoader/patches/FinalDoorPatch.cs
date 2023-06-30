using HarmonyLib;
using UnityEngine;

namespace RudeLevelScripts
{
	[HarmonyPatch(typeof(FinalDoor), nameof(FinalDoor.GetOnMaterial))]
	public static class FinalDoorPatch
	{
		[HarmonyPostfix]
		public static bool Prefix(FinalDoor __instance, MeshRenderer __0, ref int __result)
		{
			string mrName = __0.material.name;
			while (mrName.EndsWith(" (Instance)"))
				mrName = mrName.Substring(0, mrName.Length - " (Instance)".Length);

			for (int i = 0; i < __instance.offMaterials.Length; i++)
			{
				if (__instance.offMaterials[i].name.StartsWith(mrName))
				{
					__result = i;
					return false;
				}
			}

			__result = -1;
			return false;
		}
	}
}
