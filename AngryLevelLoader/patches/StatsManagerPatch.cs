﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AngryLevelLoader.patches
{
	[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.Awake))]
	class StatsManager_Awake_Patch
	{
		// Load previously found secrets manually
		// as well as challenge complete status
		[HarmonyPostfix]
		public static void Postfix(StatsManager __instance)
		{
			Plugin.CheckIsInCustomScene(SceneManager.GetActiveScene());
			if (!Plugin.isInCustomScene)
				return;

			__instance.challengeComplete = Plugin.currentLevelContainer.challenge.value;

			__instance.secretObjects = new GameObject[Plugin.currentLevelData.secretCount];

			__instance.prevSecrets.Clear();
			__instance.newSecrets.Clear();
			string secretsStr = Plugin.currentLevelContainer.secrets.value;
			for (int i = 0; i < secretsStr.Length; i++)
				if (secretsStr[i] == 'T')
					__instance.prevSecrets.Add(i);
		}
	}

	[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.SecretFound))]
	class StatsManager_SecretFound_Patch
	{
		// Handle secret found trigger for custom levels
		[HarmonyPrefix]
		static bool Prefix(StatsManager __instance, int __0)
		{
			if (!Plugin.isInCustomScene)
				return true;

			if (__instance.prevSecrets.Contains(__0) || __instance.newSecrets.Contains(__0))
				return false;

			string currentSecrets = Plugin.currentLevelContainer.secrets.value;
			StringBuilder sb = new StringBuilder(currentSecrets);
			sb[__0] = 'T';

			Plugin.currentLevelContainer.secrets.value = sb.ToString();
			Plugin.currentLevelContainer.UpdateUI();

			__instance.newSecrets.Add(__0);

			return false;
		}
	}

	[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.SendInfo))]
	class StatsManager_SendInfo_Patch
	{
		static string RemoveFormatting(string str)
		{
			Regex rich = new Regex(@"<[^>]*>");
			if (rich.IsMatch(str))
				return rich.Replace(str, string.Empty);
			else
				return str;
		}

		[HarmonyPrefix]
		static bool Prefix(StatsManager __instance)
		{
			if (!Plugin.isInCustomScene || Plugin.currentLevelData.isSecretLevel)
				return true;

			if (!Plugin.currentLevelData.levelChallengeEnabled)
				__instance.challengeComplete = true;

			Transform secretContainer = __instance.fr.transform.Find("Secrets - Info");
			if (secretContainer != null)
			{
				HorizontalLayoutGroup secretsLayout = secretContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
				secretsLayout.childControlWidth = true;
				secretsLayout.childForceExpandWidth = true;

				while (secretContainer.childCount != 1)
				{
					Transform child = secretContainer.GetChild(1);
					UnityEngine.Object.Destroy(child.gameObject);
					child.transform.SetParent(null);
				}

				if (Plugin.currentLevelData.secretCount == 0)
				{
					Transform child = secretContainer.GetChild(0);
					UnityEngine.Object.Destroy(child.gameObject);
					child.transform.SetParent(null);
				}
				else
				{
					List<Transform> secrets = new List<Transform>() { secretContainer.GetChild(0) };
					for (int i = 1; i < Plugin.currentLevelData.secretCount; i++)
					{
						GameObject newChild = UnityEngine.Object.Instantiate(secretContainer.GetChild(0).gameObject, secretContainer);
						secrets.Add(newChild.transform);
					}

					for (int i = 0; i < 5 - Plugin.currentLevelData.secretCount; i++)
					{
						GameObject newChild = UnityEngine.Object.Instantiate(secretContainer.GetChild(0).gameObject, secretContainer);
						newChild.GetComponent<Image>().color = new Color(0, 0, 0, 0);
					}

					string secretStr = Plugin.currentLevelContainer.secrets.value;
					for (int i = 0; i < secrets.Count; i++)
					{
						if (secretStr[i] == 'T')
							secrets[i].GetComponent<Image>().color = Color.white;
						else
							secrets[i].GetComponent<Image>().color = Color.black;
					}

					__instance.fr.secretsInfo = secrets.Select(e => e.GetComponent<Image>()).ToArray();
				}

				__instance.fr.levelSecrets = new GameObject[0];
			}
			else
				Debug.LogWarning("Could not find secrets container");

			return true;
		}

		[HarmonyPostfix]
		static void Postfix(StatsManager __instance)
		{
			if (!Plugin.isInCustomScene)
				return;

			if (Plugin.currentLevelData.isSecretLevel)
			{
				char prevRank = Plugin.currentLevelContainer.finalRank.value[0];
				if (prevRank != 'P')
					Plugin.currentLevelContainer.finalRank.value = AssistController.instance.cheatsEnabled ? " " : "P";

				return;
			}

			char currentRank = RemoveFormatting(__instance.fr.totalRank.text)[0];
			// Ultrakill cheats symbol to angry loader cheats symbol
			//  '-' : not completed, ' ' : cheats used
			if (currentRank == '-')
				currentRank = ' ';

			int previousRankScore = RankUtils.GetRankScore(Plugin.currentLevelContainer.finalRank.value[0]);
			int currentRankScore = RankUtils.GetRankScore(currentRank);

			bool playerBestWithoutCheats = !AssistController.instance.cheatsEnabled && (currentRankScore > previousRankScore || (currentRankScore == previousRankScore && __instance.seconds < Plugin.currentLevelContainer.time.value));
			bool firstTimeWithCheats = previousRankScore == -1 && AssistController.instance.cheatsEnabled;

			if (playerBestWithoutCheats || firstTimeWithCheats)
			{
				Plugin.currentLevelContainer.time.value = __instance.seconds;
				Plugin.currentLevelContainer.timeRank.value = RemoveFormatting(__instance.fr.timeRank.text);
				Plugin.currentLevelContainer.kills.value = __instance.kills;
				Plugin.currentLevelContainer.killsRank.value = RemoveFormatting(__instance.fr.killsRank.text);
				Plugin.currentLevelContainer.style.value = __instance.stylePoints;
				Plugin.currentLevelContainer.styleRank.value = RemoveFormatting(__instance.fr.styleRank.text);

				if (AssistController.instance.cheatsEnabled)
				{
					Plugin.currentLevelContainer.finalRank.value = " ";
				}
				else
				{
					Plugin.currentLevelContainer.finalRank.value = RemoveFormatting(__instance.fr.totalRank.text);
					if (!Plugin.currentLevelContainer.challenge.value && Plugin.currentLevelData.levelChallengeEnabled)
						Plugin.currentLevelContainer.challenge.value = ChallengeManager.instance.challengeDone && !ChallengeManager.instance.challengeFailed;
				}

				Plugin.UpdateAllUI();
			}

			// Set challenge text
			Transform challengeTextRect = __instance.fr.transform.Find("Challenge/Text");
			if (challengeTextRect != null)
			{
				challengeTextRect.GetComponent<Text>().text = Plugin.currentLevelData.levelChallengeEnabled ? Plugin.currentLevelData.levelChallengeText : "No challenge available for the level";
			}
			else
				Debug.LogWarning("Could not find challenge text");
		}
	}
}