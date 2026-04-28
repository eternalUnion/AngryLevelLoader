using AngryLevelLoader.Containers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AngryLevelLoader.Managers.ServerManager.AngryLeaderboards;

namespace AngryLevelLoader.Managers
{
	internal static class PendingRecordsManager
	{
		private class RecordInfoJsonWrapper
		{
			public string category { get; set; }
			public string difficulty { get; set; }
			public string bundleGuid { get; set; }
			public string hash { get; set; }
			public string levelId { get; set; }
			public int time { get; set; }

			public RecordInfoJsonWrapper() { }

			public RecordInfoJsonWrapper(PostRecordInfo record)
			{
				category = RECORD_CATEGORY_DICT[record.category];
				difficulty = RECORD_DIFFICULTY_DICT[record.difficulty];
				bundleGuid = record.bundleGuid;
				hash = record.hash;
				levelId = record.levelId;
				time = record.time;
			}

			public bool TryParseRecordInfo(out PostRecordInfo record)
			{
				record = new PostRecordInfo();

				record.category = RECORD_CATEGORY_DICT.FirstOrDefault(i => i.Value == category).Key;
				if (RECORD_CATEGORY_DICT[record.category] != category)
					return false;

				record.difficulty = RECORD_DIFFICULTY_DICT.FirstOrDefault(i => i.Value == difficulty).Key;
				if (RECORD_DIFFICULTY_DICT[record.difficulty] != difficulty)
					return false;

				record.bundleGuid = bundleGuid;
				record.hash = hash;
				record.levelId = levelId;
				record.time = time;
				return true;
			}
		}

		private static Task pendingRecordsTask = null;

		internal static void AddPendingRecord(PostRecordInfo record, bool recursiveCall = false)
		{
			if (pendingRecordsTask != null && !pendingRecordsTask.IsCompleted && !recursiveCall)
			{
				pendingRecordsTask.ContinueWith((task) => AddPendingRecord(record, recursiveCall: true), TaskScheduler.FromCurrentSynchronizationContext());
				return;
			}

			List<RecordInfoJsonWrapper> pendingRecordsList;
			try
			{
				pendingRecordsList = JsonConvert.DeserializeObject<List<RecordInfoJsonWrapper>>(InternalConfigManager.pendingRecordsField.value);
				if (pendingRecordsList == null)
					pendingRecordsList = new List<RecordInfoJsonWrapper>();
			}
			catch (Exception ex)
			{
				Plugin.logger.LogError($"Caught exception while trying to deserialize pending records\n{ex}");
				InternalConfigManager.pendingRecordsField.value = "[]";
				pendingRecordsList = new List<RecordInfoJsonWrapper>();
			}

			pendingRecordsList.Add(new RecordInfoJsonWrapper(record));
			InternalConfigManager.pendingRecordsField.value = JsonConvert.SerializeObject(pendingRecordsList);
			UpdatePendingRecordsUI();
		}

		internal static void ProcessPendingRecords()
		{
			if (pendingRecordsTask != null && !pendingRecordsTask.IsCompleted)
				return;
			ConfigManager.sendPendingRecords.interactable = false;
			pendingRecordsTask = ProcessPendingRecordsTask().ContinueWith((task) => ConfigManager.sendPendingRecords.interactable = true, TaskScheduler.FromCurrentSynchronizationContext());
		}

		internal static void UpdatePendingRecordsUI()
		{
			try
			{
				List<RecordInfoJsonWrapper> pendingRecordsList = JsonConvert.DeserializeObject<List<RecordInfoJsonWrapper>>(InternalConfigManager.pendingRecordsField.value);
				if (pendingRecordsList == null)
					pendingRecordsList = new List<RecordInfoJsonWrapper>();
				ConfigManager.pendingRecords.hidden = pendingRecordsList.Count == 0;
				ConfigManager.pendingRecordsInfo.text = "";

				foreach (var record in pendingRecordsList)
				{
					string bundleName = record.bundleGuid;
					string levelName = record.levelId;

					if (Plugin.TryGetAngryBundleByGuid(bundleName, out BundleContainer bundle))
						bundleName = bundle.BundleName;

					if (Plugin.TryGetAngryLevel(levelName, out LevelContainer level))
						levelName = level.LevelName;
					ConfigManager.pendingRecordsInfo.text += $"Bundle: <color=grey>{bundleName}</color>\nLevel: <color=grey>{levelName}</color>\nCategory: <color=grey>{record.category}</color>\nDifficulty: <color=grey>{record.difficulty}</color>\nTime: <color=grey>{record.time}</color>\n\n\n";
				}
			}
			catch (Exception ex)
			{
				Plugin.logger.LogError($"Caught exception while trying to deserialize pending records\n{ex}");
				InternalConfigManager.pendingRecordsField.value = "[]";
				ConfigManager.pendingRecords.hidden = true;
			}
		}

		private static async Task ProcessPendingRecordsTask()
		{
			ConfigManager.pendingRecordsStatus.text = "";

			List<RecordInfoJsonWrapper> pendingRecordsList;
			try
			{
				pendingRecordsList = JsonConvert.DeserializeObject<List<RecordInfoJsonWrapper>>(InternalConfigManager.pendingRecordsField.value);
				if (pendingRecordsList == null)
					pendingRecordsList = new List<RecordInfoJsonWrapper>();
			}
			catch (Exception ex)
			{
				ConfigManager.pendingRecordsStatus.text = $"<color=red>Exception thrown while deserializing pending records, discarding\n\n{ex}</color>";
				InternalConfigManager.pendingRecordsField.value = "[]";
				UpdatePendingRecordsUI();
				return;
			}

			List<RecordInfoJsonWrapper> failedToSend = new List<RecordInfoJsonWrapper>();
			foreach (var record in pendingRecordsList)
			{
				string bundleName = record.bundleGuid;
				string levelName = record.levelId;

				if (Plugin.TryGetAngryBundleByGuid(bundleName, out BundleContainer bundle))
					bundleName = bundle.BundleName;

				if (Plugin.TryGetAngryLevel(levelName, out LevelContainer level))
					levelName = level.LevelName;

				if (!record.TryParseRecordInfo(out PostRecordInfo parsedRecord))
				{
					ConfigManager.pendingRecordsStatus.text += $"<color=red>Failed to parse record info for level {levelName} in bundle {bundleName}. Discarded.</color>\n\n";
					continue;
				}

				ConfigManager.pendingRecordsStatus.text += $"Posting record for level <color=grey>{levelName}</color> in bundle <color=grey>{bundleName}</color>...\n";

				var postResult = await PostRecordTask(parsedRecord.category, parsedRecord.difficulty, parsedRecord.bundleGuid, parsedRecord.hash, parsedRecord.levelId, parsedRecord.time);
				if (postResult.completedSuccessfully)
				{
					if (postResult.status == PostRecordStatus.OK)
					{
						ConfigManager.pendingRecordsStatus.text += $"<color=#00FF00>Record posted successfully!</color> Ranking: #{postResult.response.ranking}, New Best: {postResult.response.newBest}\n\n";
					}
					else
					{
						switch (postResult.status)
						{
							case PostRecordStatus.BANNED:
								ConfigManager.pendingRecordsStatus.text += "<color=red>User banned from the leaderboards. Discarded.</color>\n\n";
								break;

							case PostRecordStatus.INVALID_BUNDLE:
							case PostRecordStatus.INVALID_ID:
								ConfigManager.pendingRecordsStatus.text += "<color=red>Level's leaderboards are not enabled. Discarded.</color>\n\n";
								break;

							case PostRecordStatus.RATE_LIMITED:
								ConfigManager.pendingRecordsStatus.text += "<color=red>Too many requests sent. Returning record to the pending list</color>\n\n";
								failedToSend.Add(record);
								break;

							case PostRecordStatus.INVALID_HASH:
								ConfigManager.pendingRecordsStatus.text += "<color=red>Record bundle version is not up to date with the leaderboard. Discarded.</color>\n\n";
								break;

							case PostRecordStatus.INVALID_TIME:
								ConfigManager.pendingRecordsStatus.text += $"<color=red>Angry server rejected the sent time {record.time}. Discarded.</color>\n\n";
								break;

							default:
								ConfigManager.pendingRecordsStatus.text += $"<color=red>Encountered an unknown error while posting record. Status: {postResult.status}, Message: '{postResult.message}'. Returning record to the pending list</color>\n\n";
								failedToSend.Add(record);
								break;
						}
					}
				}
				else
				{
					ConfigManager.pendingRecordsStatus.text += $"<color=red>Encountered a network error while posting record. Returning record to the pending list</color>\n\n";
					failedToSend.Add(record);
				}
			}

			ConfigManager.pendingRecordsStatus.text += $"<color=#00FF00>Done!</color>";
			InternalConfigManager.pendingRecordsField.value = JsonConvert.SerializeObject(failedToSend);
			UpdatePendingRecordsUI();
		}
	}
}