using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using AngryLevelLoader.Utils;
using HarmonyLib;
using RudeLevelScripts.Essentials;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.Awake))]
	internal class StatsManager_Awake_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(StatsManager __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			__instance.levelNumber = -1;
			__instance.secretObjects = new GameObject[0];
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

			LevelContainer currentLevel = AngrySceneManager.currentLevelContainer;

			for (int i = 0; i < currentLevel.SecretCount; i++)
				if (currentLevel.SecretDiscovered(i))
					__instance.prevSecrets.Add(i);
		}
	}

	[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.SecretFound))]
	internal class StatsManager_SecretFound_Patch
	{
		// Handle secret found trigger for custom levels
		[HarmonyPrefix]
		static bool Prefix(StatsManager __instance, int __0)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			if (__instance.prevSecrets.Contains(__0) || __instance.newSecrets.Contains(__0))
				return false;

			if (BonusPatches.lastCaller == null || BonusPatches.lastCaller.secretNumber != __0)
				return false;

			if (BonusPatches.lastCaller.GetComponent<IgnoreSecret>() != null)
				return false;

			AngrySceneManager.currentLevelContainer.SetSecretDiscovered(__0, true);

			__instance.newSecrets.Add(__0);

			return false;
		}
	}

	[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.SendInfo))]
	internal class StatsManager_SendInfo_Patch
	{
		static char GetRank(int[] ranksToCheck, float value, bool reverse)
		{
			int num = 0;
			while (true)
			{
				if (num >= ranksToCheck.Length)
				{
					return 'S';
				}
				if ((reverse && value <= ranksToCheck[num]) || (!reverse && value >= ranksToCheck[num]))
				{
					num++;
				}
				else
				{
					switch (num)
					{
						case 0:
							return 'D';
						case 1:
							return 'C';
						case 2:
							return 'B';
						case 3:
							return 'A';
					}
				}
			}
		}

		[HarmonyPrefix]
		static bool Prefix(StatsManager __instance)
		{
			bool secretLevel = AngrySceneManager.currentLevelData.isSecretLevel;
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

					LevelContainer currentLevel = AngrySceneManager.currentLevelContainer;
					for (int i = 0; i < secrets.Count; i++)
					{
						if (currentLevel.SecretDiscovered(i))
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

			LevelEndLeaderboard leaderboard = null;
			if (FinalRank.Instance != null)
				leaderboard = FinalRank.Instance.GetComponentInChildren<LevelEndLeaderboard>(true);

			void AppendInfoToLeaderboard(string message)
			{
				if (leaderboard == null)
				{
					Debug.LogWarning("Failed to find leaderboard for record info");
					return;
				}

				Transform bottomText = leaderboard.transform.Find("SettingsReminder");
				if (bottomText != null && bottomText.gameObject.TryGetComponent(out TextMeshProUGUI bottomTextComp))
				{
					Vector4 margin = bottomTextComp.margin;
					margin.w = -500;
					bottomTextComp.margin = margin;

					bottomTextComp.text += $"\n{message}";
				}
            }

			// Send record
			if (InternalConfigManager.leaderboardToggle.value)
			{
				// No gamemode
				if (AngryGamemodeManager.SelectedGamemode == AngryGamemodeManager.Gamemode.None)
				{
					AngryLeaderboards.PostRecordInfo record = new AngryLeaderboards.PostRecordInfo();
					record.category = AngryLeaderboards.RecordCategory.ALL;
					record.difficulty = AngryLeaderboards.DifficultyFromInteger(PrefsManager.Instance.GetInt("difficulty", -1));
					record.bundleGuid = AngrySceneManager.currentBundleContainer.bundleGuid;
					record.hash = AngrySceneManager.currentBundleContainer.BuildHash;
					record.levelId = AngrySceneManager.currentLevelData.uniqueIdentifier;
					record.time = (int)(__instance.seconds * 1000);
					AngryLeaderboards.TryPostRecordTask(record).ContinueWith((t) =>
					{
						if (t.Exception != null)
							Debug.LogException(t.Exception);
						else
							AppendInfoToLeaderboard(t.Result);
					}, TaskScheduler.FromCurrentSynchronizationContext());

					if (__instance.rankScore == 12)
					{
						record.category = AngryLeaderboards.RecordCategory.PRANK;
						AngryLeaderboards.TryPostRecordTask(record);
					}

					if (ChallengeManager.Instance != null)
					{
						bool postChallengeRecord = ChallengeManager.Instance.challengeDone && !ChallengeManager.Instance.challengeFailed;
						if (postChallengeRecord && AngrySceneManager.currentLevelData.levelChallengeEnabled)
						{
							record.category = AngryLeaderboards.RecordCategory.CHALLENGE;
							AngryLeaderboards.TryPostRecordTask(record);
						}
					}
				}
				// Nomo/Nomow
				else if (AngryGamemodeManager.SelectedGamemode == AngryGamemodeManager.Gamemode.NoMonsters || AngryGamemodeManager.SelectedGamemode == AngryGamemodeManager.Gamemode.NoMonstersAndWeapons)
				{
					AngryLeaderboards.PostRecordInfo record = new AngryLeaderboards.PostRecordInfo();
					record.category = AngryGamemodeManager.SelectedGamemode == AngryGamemodeManager.Gamemode.NoMonsters ? AngryLeaderboards.RecordCategory.NOMO : AngryLeaderboards.RecordCategory.NOMOW;
					record.difficulty = AngryLeaderboards.RecordDifficulty.HARMLESS;
					record.bundleGuid = AngrySceneManager.currentBundleContainer.bundleGuid;
					record.hash = AngrySceneManager.currentBundleContainer.BuildHash;
					record.levelId = AngrySceneManager.currentLevelData.uniqueIdentifier;
					record.time = (int)(__instance.seconds * 1000);
					AngryLeaderboards.TryPostRecordTask(record).ContinueWith((t) =>
					{
						if (t.Exception != null)
							Debug.LogException(t.Exception);
						else
							AppendInfoToLeaderboard(t.Result);
					}, TaskScheduler.FromCurrentSynchronizationContext());
				}
			}

			bool isPlayingWithoutGamemode = AngryGamemodeManager.SelectedGamemode == AngryGamemodeManager.Gamemode.None;
			bool secretLevel = AngrySceneManager.currentLevelData.isSecretLevel;

			// Secret levels only have final ranks
			if (secretLevel)
			{
				if (!isPlayingWithoutGamemode)
					return;

				char prevRank = AngrySceneManager.currentLevelContainer.FinalRank;
				if (prevRank != 'P')
					AngrySceneManager.currentLevelContainer.FinalRank = AssistController.Instance.cheatsEnabled ? ' ' : 'P';

				return;
			}

			char currentRank;
			if (secretLevel)
			{
				currentRank = 'P';
			}
			else
			{
				switch (__instance.rankScore)
				{
					case 12:
						currentRank = 'P';
						break;
					case 4:
					case 5:
					case 6:
						currentRank = 'S';
						break;
					case 3:
						currentRank = 'A';
						break;
					case 2:
						currentRank = 'B';
						break;
					case 1:
						currentRank = 'C';
						break;
					default:
						currentRank = 'D';
						break;
				}
			}

			if (AssistController.Instance.cheatsEnabled)
				currentRank = ' ';

			int previousRankScore = AngryRankUtils.GetRankScore(AngrySceneManager.currentLevelContainer.FinalRank);
			int currentRankScore = AngryRankUtils.GetRankScore(currentRank);

			bool usedCheats = AssistController.Instance.cheatsEnabled;
			bool challengeCompletedThisSeason = ChallengeManager.Instance.challengeDone && !ChallengeManager.Instance.challengeFailed;
			bool challengeCompletedBefore = AngrySceneManager.currentLevelContainer.ChallengeDone;
            bool playerBestWithoutCheats = !usedCheats && (currentRankScore > previousRankScore || (currentRankScore == previousRankScore && __instance.seconds < AngrySceneManager.currentLevelContainer.Time));
			bool firstTimeWithCheats = previousRankScore == -1 && usedCheats;

			if (!usedCheats)
			{
                if (!challengeCompletedBefore && AngrySceneManager.currentLevelData.levelChallengeEnabled)
				{
                    AngrySceneManager.currentLevelContainer.ChallengeDone = challengeCompletedThisSeason;
                }
            }

			if ((playerBestWithoutCheats || firstTimeWithCheats) && isPlayingWithoutGamemode)
			{
				AngrySceneManager.currentLevelContainer.Time = __instance.seconds;
				AngrySceneManager.currentLevelContainer.TimeRank = GetRank(__instance.timeRanks, __instance.seconds, true);
				AngrySceneManager.currentLevelContainer.Kills = __instance.kills;
				AngrySceneManager.currentLevelContainer.KillsRank = GetRank(__instance.killRanks, __instance.kills, false);
				AngrySceneManager.currentLevelContainer.Style = __instance.stylePoints;
				AngrySceneManager.currentLevelContainer.StyleRank = GetRank(__instance.styleRanks, __instance.stylePoints, false);

				if (usedCheats)
				{
					AngrySceneManager.currentLevelContainer.FinalRank = ' ';
				}
				else
				{
					AngrySceneManager.currentLevelContainer.FinalRank = currentRank;
				}
			}

			// Set challenge text
			Transform challengeTextRect = __instance.fr.transform.Find("Challenge/ChallengeText");
			if (challengeTextRect != null)
			{
				challengeTextRect.GetComponent<TextMeshProUGUI>().text = AngrySceneManager.currentLevelData.levelChallengeEnabled ? AngrySceneManager.currentLevelData.levelChallengeText : "No challenge available for the level";
			}
			else
				Plugin.logger.LogWarning("Could not find challenge text");

			// Set challenge panel
			if (FinalRank.Instance != null)
			{
				GameObject challengePanel = FinalRank.Instance.transform.Find("Challenge/Panel (1)").gameObject;

				if (challengePanel != null)
				{
					if (AngrySceneManager.currentLevelData.levelChallengeEnabled && (challengeCompletedThisSeason || challengeCompletedBefore))
					{
						Plugin.logger.LogInfo("Enabling challenge panel since it is completed now or before");
						if (usedCheats)
						{
							challengePanel.GetComponent<AudioSource>().volume = 0f;
							
							if (challengeCompletedBefore && !challengeCompletedThisSeason)
							{
								challengePanel.GetComponent<Image>().color = new Color(0, 1, 0, 0.5f);
							}
							else
							{
								challengePanel.GetComponent<Image>().color = new Color(0, 1, 0, 1f);
							}
						}
						else if (challengeCompletedBefore && !challengeCompletedThisSeason)
						{
							challengePanel.GetComponent<Image>().color = new Color(1f, 0.696f, 0f, 0.5f);
							challengePanel.GetComponent<AudioSource>().volume = 1f;
						}
						else
						{
							challengePanel.GetComponent<Image>().color = new Color(1f, 0.696f, 0f, 1f);
							challengePanel.GetComponent<AudioSource>().volume = 1f;
						}
						
						challengePanel.SetActive(true);
					}
					else
					{
						Plugin.logger.LogInfo("Disabling challenge panel since it is not completed now and before");
						challengePanel.SetActive(false);
					}
				}
			}
		}
	}
}
