using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches
{
	// Gotta be very careful here

	[HarmonyPatch(typeof(GameProgressSaver))]
	public class GameProgressSaverPatches
	{
		[HarmonyPatch(nameof(GameProgressSaver.GetRankData), new Type[] { typeof(string), typeof(int), typeof(bool) }, new ArgumentType[] { ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
		[HarmonyPrefix]
		public static bool GetRankDataOverwrite(ref RankData __result)
		{
			try
			{
				if (AngrySceneManager.isInCustomLevel)
				{
					__result = null;
					return false;
				}

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError($"Caught exception in patch GetRankDataOverwrite\n{e}");
				return true;
			}
		}

		[HarmonyPatch(nameof(GameProgressSaver.SaveRank), new Type[0])]
		[HarmonyPrefix]
		public static bool SaveRankOverwrite()
		{
			try
			{
				if (AngrySceneManager.isInCustomLevel)
					return false;

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				return true;
			}
		}

		[HarmonyPatch(nameof(GameProgressSaver.ChallengeComplete), new Type[0])]
		[HarmonyPrefix]
		public static bool ChallengeCompleteOverwrite()
		{
			try
			{
				if (AngrySceneManager.isInCustomLevel)
					return false;

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				return true;
			}
		}

		[HarmonyPatch(nameof(GameProgressSaver.SaveProgress), new Type[] { typeof(int) })]
		[HarmonyPrefix]
		public static bool SaveProgressOverwrite()
		{
			try
			{
				if (AngrySceneManager.isInCustomLevel)
					return false;

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				return true;
			}
		}
	}
}
