using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.Awake))]
	class StatsManager_Awake_Patch
	{
		public static bool Prefix(StatsManager __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			__instance.levelNumber = -1;
			return true;
		}

		// Load previously found secrets manually
		// as well as challenge complete status
		[HarmonyPostfix]
		public static void Postfix(StatsManager __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return;

			__instance.challengeComplete = false;

			__instance.secretObjects = new GameObject[AngrySceneManager.currentLevelData.secretCount];

			__instance.prevSecrets.Clear();
			__instance.newSecrets.Clear();
			string secretsStr = AngrySceneManager.currentLevelContainer.secrets.value;
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
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			if (__instance.prevSecrets.Contains(__0) || __instance.newSecrets.Contains(__0))
				return false;

			string currentSecrets = AngrySceneManager.currentLevelContainer.secrets.value;
			StringBuilder sb = new StringBuilder(currentSecrets);
			sb[__0] = 'T';

			AngrySceneManager.currentLevelContainer.secrets.value = sb.ToString();
			AngrySceneManager.currentLevelContainer.UpdateUI();

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
			bool secretLevel = __instance.fr.transform.Find("Challenge") == null;
			if (!AngrySceneManager.isInCustomLevel || secretLevel)
				return true;

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

				if (AngrySceneManager.currentLevelData.secretCount == 0)
				{
					Transform child = secretContainer.GetChild(0);
					UnityEngine.Object.Destroy(child.gameObject);
					child.transform.SetParent(null);
				}
				else
				{
					List<Transform> secrets = new List<Transform>() { secretContainer.GetChild(0) };
					for (int i = 1; i < AngrySceneManager.currentLevelData.secretCount; i++)
					{
						GameObject newChild = UnityEngine.Object.Instantiate(secretContainer.GetChild(0).gameObject, secretContainer);
						secrets.Add(newChild.transform);
					}

					for (int i = 0; i < 5 - AngrySceneManager.currentLevelData.secretCount; i++)
					{
						GameObject newChild = UnityEngine.Object.Instantiate(secretContainer.GetChild(0).gameObject, secretContainer);
						newChild.GetComponent<Image>().color = new Color(0, 0, 0, 0);
					}

					string secretStr = AngrySceneManager.currentLevelContainer.secrets.value;
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
				Plugin.logger.LogWarning("Could not find secrets container");

			return true;
		}

		[HarmonyPostfix]
		static void Postfix(StatsManager __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return;

			// Send record
			if (Plugin.leaderboardToggle.value)
			{
				AngryLeaderboards.PostRecordInfo record = new AngryLeaderboards.PostRecordInfo();
				record.category = AngryLeaderboards.RecordCategory.ALL;
				record.difficulty = AngryLeaderboards.DifficultyFromInteger(PrefsManager.Instance.GetInt("difficulty", -1));
				record.bundleGuid = AngrySceneManager.currentBundleContainer.bundleData.bundleGuid;
				record.hash = AngrySceneManager.currentBundleContainer.bundleData.buildHash;
				record.levelId = AngrySceneManager.currentLevelData.uniqueIdentifier;
				record.time = (int)(__instance.seconds * 1000);
				AngryLeaderboards.TryPostRecordTask(record);

				if (__instance.rankScore == 12)
				{
					record.category = AngryLeaderboards.RecordCategory.PRANK;
					AngryLeaderboards.TryPostRecordTask(record);
				}
			}

			bool secretLevel = __instance.fr.transform.Find("Challenge") == null;
			if (secretLevel)
			{
				char prevRank = AngrySceneManager.currentLevelContainer.finalRank.value[0];
				if (prevRank != 'P')
					AngrySceneManager.currentLevelContainer.finalRank.value = AssistController.instance.cheatsEnabled ? " " : "P";

				return;
			}

			char currentRank = RemoveFormatting(__instance.fr.totalRank.text)[0];
			// Ultrakill cheats symbol to angry loader cheats symbol
			//  '-' : not completed, ' ' : cheats used
			if (currentRank == '-')
				currentRank = ' ';

			int previousRankScore = RankUtils.GetRankScore(AngrySceneManager.currentLevelContainer.finalRank.value[0]);
			int currentRankScore = RankUtils.GetRankScore(currentRank);

			bool usedCheats = AssistController.instance.cheatsEnabled;
			bool challengeCompletedThisSeason = ChallengeManager.instance.challengeDone && !ChallengeManager.instance.challengeFailed;
			bool challengeCompletedBefore = AngrySceneManager.currentLevelContainer.challenge.value;
            bool playerBestWithoutCheats = !usedCheats && (currentRankScore > previousRankScore || (currentRankScore == previousRankScore && __instance.seconds < AngrySceneManager.currentLevelContainer.time.value));
			bool firstTimeWithCheats = previousRankScore == -1 && usedCheats;

			if (playerBestWithoutCheats || firstTimeWithCheats)
			{
				AngrySceneManager.currentLevelContainer.time.value = __instance.seconds;
				AngrySceneManager.currentLevelContainer.timeRank.value = RemoveFormatting(__instance.fr.timeRank.text);
				AngrySceneManager.currentLevelContainer.kills.value = __instance.kills;
				AngrySceneManager.currentLevelContainer.killsRank.value = RemoveFormatting(__instance.fr.killsRank.text);
				AngrySceneManager.currentLevelContainer.style.value = __instance.stylePoints;
				AngrySceneManager.currentLevelContainer.styleRank.value = RemoveFormatting(__instance.fr.styleRank.text);

				if (usedCheats)
				{
					AngrySceneManager.currentLevelContainer.finalRank.value = " ";
				}
				else
				{
					AngrySceneManager.currentLevelContainer.finalRank.value = RemoveFormatting(__instance.fr.totalRank.text);
					if (!challengeCompletedBefore && AngrySceneManager.currentLevelData.levelChallengeEnabled)
						AngrySceneManager.currentLevelContainer.challenge.value = challengeCompletedThisSeason;
				}

				AngrySceneManager.currentBundleContainer.RecalculateFinalRank();
				AngrySceneManager.currentLevelContainer.UpdateUI();
			}

			// Set challenge text
			Transform challengeTextRect = __instance.fr.transform.Find("Challenge/Text");
			if (challengeTextRect != null)
			{
				challengeTextRect.GetComponent<Text>().text = AngrySceneManager.currentLevelData.levelChallengeEnabled ? AngrySceneManager.currentLevelData.levelChallengeText : "No challenge available for the level";
			}
			else
				Plugin.logger.LogWarning("Could not find challenge text");

			// Set challenge panel
			if (AngrySceneManager.currentLevelData.levelChallengeEnabled && (challengeCompletedThisSeason || challengeCompletedBefore))
			{
				Plugin.logger.LogInfo("Enabling challenge panel since it is completed now or before");
				ChallengeManager.Instance.challengePanel.GetComponent<Image>().color = usedCheats && !challengeCompletedBefore ? new Color(0, 1, 0, 0.5f) : new Color(1f, 0.696f, 0f, 0.5f);
                ChallengeManager.Instance.challengePanel.GetComponent<AudioSource>().volume = !challengeCompletedBefore && !usedCheats ? 1f : 0f;
                ChallengeManager.Instance.challengePanel.SetActive(true);
            }
			else
			{
                Plugin.logger.LogInfo("Disabling challenge panel since it is not completed now and before");
                ChallengeManager.Instance.challengePanel.SetActive(false);
            }
		}
	}
}
