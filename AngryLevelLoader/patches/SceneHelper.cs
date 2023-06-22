using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.patches
{
	[HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.LoadScene))]
	class SceneHelper_LoadScene_Patch
	{
		static bool Prefix(ref string __0)
		{
			if (Plugin.isInCustomScene && __0 == "")
			{
				__0 = "Main Menu";
			}

			return true;
		}
	}
}
