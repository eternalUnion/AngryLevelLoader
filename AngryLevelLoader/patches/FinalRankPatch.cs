﻿using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.Start))]
    class FinalRank_Start_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(FinalRank __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            // If level is a secret level, alternative level end screen will be shown, which does not contain a leaderboard
            // In that case move the leaderboard from ranked end screen to the secret level end screen, and add it to the toAppear list
            if (__instance.gameObject.GetComponentInChildren<LevelEndLeaderboard>(true) == null)
            {
                LevelEndLeaderboard otherFinalRanksLeaderboard = __instance.transform.parent.GetComponentInChildren<LevelEndLeaderboard>(true);

                if (otherFinalRanksLeaderboard != null)
                {
                    otherFinalRanksLeaderboard.transform.SetParent(__instance.transform);

                    List<GameObject> toAppear = __instance.toAppear.ToList();
                    toAppear.Add(otherFinalRanksLeaderboard.gameObject);
                    __instance.toAppear = toAppear.ToArray();
                }
            }

            bool secretLevel = __instance.transform.Find("Challenge") == null;

            if (secretLevel)
            {
                Transform titleTrans = __instance.transform.Find("Title/Text");
                if (titleTrans != null)
                {
                    titleTrans.GetComponent<TextMeshProUGUI>().text = AngrySceneManager.currentLevelData.levelName;
                }
                else
                {
                    Plugin.logger.LogWarning("Could not find title text under final canvas");
                }

                return true;
            }

            __instance.levelSecrets = StatsManager.instance.secretObjects;
            if (__instance.levelSecrets.Length != AngrySceneManager.currentLevelData.secretCount)
            {
                Plugin.logger.LogWarning($"Inconsistent secrets size, expected {AngrySceneManager.currentLevelData.secretCount}, found {__instance.levelSecrets.Length}");
                __instance.levelSecrets = new GameObject[AngrySceneManager.currentLevelData.secretCount];
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.CountSecrets))]
    class FinalRank_CountSecrets_Patch
    {
        static bool Prefix(FinalRank __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            AngrySceneManager.currentLevelContainer.AssureSecretsSize();
            if (__instance.secretsCheckProgress >= AngrySceneManager.currentLevelData.secretCount)
            {
                __instance.Invoke("Appear", __instance.timeBetween);
                return false;
            }

            if (AngrySceneManager.currentLevelContainer.secrets.value[__instance.secretsCheckProgress] != 'T')
            {
                __instance.secretsInfo[__instance.secretsCheckProgress].color = Color.black;
                __instance.secretsCheckProgress += 1;

                if (__instance.secretsCheckProgress < __instance.levelSecrets.Length)
                {
                    __instance.Invoke("CountSecrets", __instance.timeBetween);
                    return false;
                }
                __instance.Invoke("Appear", __instance.timeBetween);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.LevelChange))]
    class FinalRank_LevelChange_Patch
    {
        [HarmonyPrefix]
        static bool Prefix()
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            //Quit mission if theres no target level
            if (FinalPit_SendInfo_Patch.lastTarget == null || string.IsNullOrEmpty(FinalPit_SendInfo_Patch.lastTarget.targetLevelUniqueId))
            {
                MonoSingleton<OptionsManager>.Instance.QuitMission();
                return false;
            }

            string levelID = FinalPit_SendInfo_Patch.lastTarget.targetLevelUniqueId;

            //Attempt to find the level id from AngrySceneManager, quit mission if it can't be found
            if (!AngrySceneManager.TryFindLevel(levelID, out LevelContainer level))
            {
                Plugin.logger.LogWarning("Could not find target level id " + levelID);
                MonoSingleton<OptionsManager>.Instance.QuitMission();
                return false;
            }

            //Load the level
            AngrySceneManager.LoadLevel(level.container, level, level.data, level.data.scenePath);
            return false;
        }
    }
}
