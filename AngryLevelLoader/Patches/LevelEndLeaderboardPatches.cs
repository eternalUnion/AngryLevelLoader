using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using HarmonyLib;
using Steamworks.Data;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SocialPlatforms.Impl;
using System.Linq;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(LevelEndLeaderboard))]
	public static class LevelEndLeaderboardPatches
	{
		[HarmonyPatch(nameof(LevelEndLeaderboard.OnEnable))]
		[HarmonyPrefix]
		public static bool EnableCustomLeaderboard(LevelEndLeaderboard __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			if (AngrySceneManager.currentLevelData.isSecretLevel)
			{
				if (!Plugin.showLeaderboardOnSecretLevelEnd.value)
				{
					__instance.gameObject.SetActive(false);
					return false;
				}
			}
			else
			{
				if (!Plugin.showLeaderboardOnLevelEnd.value)
				{
					__instance.gameObject.SetActive(false);
					return false;
				}
			}

			// UI setup
			Text loadingText = __instance.loadingPanel.gameObject.GetComponent<Text>();
			loadingText.text = "CONNECTING TO\nANGRY SERVER";

			__instance.displayPRank = (MonoSingleton<StatsManager>.Instance.rankScore == 12);
			__instance.StartCoroutine(CustomFetch(__instance));

			return false;
		}

		public static IEnumerator CustomFetch(LevelEndLeaderboard instance)
		{
			instance.ResetEntries();
			Text loadingText = instance.loadingPanel.gameObject.GetComponent<Text>();
			instance.container.gameObject.SetActive(false);
			instance.loadingPanel.SetActive(true);
			instance.leaderboardType.text = (instance.displayPRank ? "P RANK" : "ANY RANK");

			while (AngryLeaderboards.postRecordTasks.Count != 0)
			{
				loadingText.text = "POSTING RECORD";
				Task postRecordTask = AngryLeaderboards.postRecordTasks.Where(task => !task.IsCompleted).FirstOrDefault();

				if (postRecordTask == null)
					break;

				while (!postRecordTask.IsCompleted)
					yield return null;
			}

			loadingText.text = "CONNECTING TO\nANGRY SERVER";

			AngryLeaderboards.RecordDifficulty difficulty = AngryLeaderboards.DifficultyFromInteger(PrefsManager.Instance.GetInt("difficulty", -1));

			// Get top 10 records
			Task<AngryLeaderboards.GetRecordsResult> getRecordsTask = AngryLeaderboards.GetRecordsTask(
				instance.displayPRank ? AngryLeaderboards.RecordCategory.PRANK : AngryLeaderboards.RecordCategory.ALL,
				difficulty,
				AngrySceneManager.currentBundleContainer.bundleData.bundleGuid,
				AngrySceneManager.currentLevelData.uniqueIdentifier,
				0,
				10);

			while (!getRecordsTask.IsCompleted)
				yield return null;

			AngryLeaderboards.GetRecordsResult result = getRecordsTask.Result;
			if (result.networkError)
			{
				Plugin.logger.LogError("Could not get level records (NETWORK_ERROR), check connection");
				loadingText.text = "NETWORK ERROR,\nCHECK CONNECTION";
				yield break;
			}
			if (result.httpError)
			{
				Plugin.logger.LogError("Could not get level records (HTTP_ERROR), check server");
				loadingText.text = "SERVER ERROR,\nTRY AGAIN LATER";
				yield break;
			}
			if (result.status != AngryLeaderboards.GetRecordsStatus.OK)
			{
				Plugin.logger.LogError($"Status error while getting all records. message: {result.message}, status: {result.status}");
				
				switch (result.status)
				{
					case AngryLeaderboards.GetRecordsStatus.INVALID_BUNDLE:
					case AngryLeaderboards.GetRecordsStatus.INVALID_ID:
						loadingText.text = "LEADERBOARDS\nDISABLED FOR LEVEL";
						break;

					case AngryLeaderboards.GetRecordsStatus.RATE_LIMITED:
						loadingText.text = "TOO MANY REQUESTS,\nTRY AGAIN LATER";
						break;

					default:
						loadingText.text = $"STATUS ERROR:\n{result.status}";
						break;
				}

				yield break;
			}

			// Display top 10 records
			int order = 1;
			foreach (var record in result.response.records)
			{
				if (!ulong.TryParse(record.steamId, out ulong steamIdNumeric))
				{
					Plugin.logger.LogError($"Failed to parse steam id {record.steamId}");
					continue;
				}

				int minutes = record.time / 60000;
				float seconds = (float)(record.time - minutes * 60000) / 1000f;
				instance.templateTime.text = string.Format("{0}:{1:00.000}", minutes, seconds);

				Text text = instance.templateUsername;
				text.text = "<unknown>";
				instance.templateDifficulty.text = $"#{order++}";

				if (SteamCacheManager.TryGetUser(steamIdNumeric, out SteamUserCache cachedSteamUser))
				{
					text.text = cachedSteamUser.name;

					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(instance.template, instance.container);
					gameObject.SetActive(true);

					RawImage profilePicture = gameObject.GetComponentInChildren<RawImage>();
					profilePicture.texture = cachedSteamUser.profilePicture;
				}
				else
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(instance.template, instance.container);
					gameObject.SetActive(true);

					Transform newUsernameFieldTrans = gameObject.transform.Find("Username Field");
					Text newUsernameField = null;
					if (newUsernameFieldTrans != null)
						newUsernameField = newUsernameFieldTrans.GetComponent<Text>();
					RawImage profilePicture = gameObject.GetComponentInChildren<RawImage>();

					var userRequest = SteamCacheManager.RequestUser(steamIdNumeric);
					userRequest.ContinueWith((task) =>
					{
						var result = task.Result;

						if (newUsernameField != null)
							newUsernameField.text = result.name;

						if (result.profilePicture != null && profilePicture != null)
							profilePicture.texture = result.profilePicture;
					}, TaskScheduler.FromCurrentSynchronizationContext());
				}
			}

			instance.loadingPanel.SetActive(false);
			instance.container.gameObject.SetActive(true);

			yield break;
		}

		private class InstantReturnEnumerator : IEnumerator
		{
			public object Current => null;

			public bool MoveNext()
			{
				return false;
			}

			public void Reset()
			{

			}
		}

		[HarmonyPatch(nameof(LevelEndLeaderboard.Fetch))]
		[HarmonyPrefix]
		public static bool RouteFetchToCustom(LevelEndLeaderboard __instance, ref IEnumerator __result)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			__result = new InstantReturnEnumerator();
			__instance.StartCoroutine(CustomFetch(__instance));
			return false;
		}
	}
}
