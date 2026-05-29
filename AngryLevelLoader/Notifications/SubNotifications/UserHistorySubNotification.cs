using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using AngryUiComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace AngryLevelLoader.Notifications.SubNotifications
{
	internal class RefreshCircleSpin : MonoBehaviour
	{
		private void Update()
		{
			transform.Rotate(new Vector3(0, 0, Time.unscaledDeltaTime * 360f));
		}
	}

	internal class UserHistorySubNotification
	{
		private readonly AngryUserHistoryPanelComponent historyPanel;
		private List<AngryUserRecordEntryComponent> historyEntries = new List<AngryUserRecordEntryComponent>();

		public UserHistorySubNotification(AngryUserHistoryPanelComponent historyPanel)
		{
			this.historyPanel = historyPanel;
			this.historyPanel.loadingCircle.AddComponent<RefreshCircleSpin>();
		}

		private static string MillisecondsToString(int milliseconds)
		{
			int minutes = milliseconds / 60000;
			float seconds = (float)(milliseconds - minutes * 60000) / 1000f;
			return string.Format("{0}:{1:00.000}", minutes, seconds);
		}

		internal async Task OpenHistoryWindow(string steamId)
		{
			historyPanel.backButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			historyPanel.backButton.onClick.AddListener(() =>
			{
				historyPanel.gameObject.SetActive(false);
			});

			historyPanel.categoryFilter.onValueChanged = new UnityEngine.UI.Dropdown.DropdownEvent();
			historyPanel.difficultyFilter.onValueChanged = new UnityEngine.UI.Dropdown.DropdownEvent();
			historyPanel.sortOrder.onValueChanged = new UnityEngine.UI.Dropdown.DropdownEvent();
			historyPanel.reverseOrder.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
			historyPanel.bundleName.onEndEdit = new UnityEngine.UI.InputField.EndEditEvent();
			historyPanel.levelName.onEndEdit = new UnityEngine.UI.InputField.EndEditEvent();

			historyPanel.categoryFilter.value = 0;
			historyPanel.difficultyFilter.value = 0;
			historyPanel.sortOrder.value = 0;
			historyPanel.reverseOrder.isOn = false;
			historyPanel.bundleName.SetTextWithoutNotify("");
			historyPanel.levelName.SetTextWithoutNotify("");

			historyPanel.inputGroup.interactable = false;

			historyEntries.ForEach(e => e.gameObject.SetActive(false));
			historyPanel.loadingCircle.SetActive(true);

			string steamName = steamId;
			if (ulong.TryParse(steamId, out ulong steamIdNum) && SteamCacheManager.TryGetUser(steamIdNum, out SteamUserCache steamUser))
			{
				steamName = $"{steamUser.name} ({steamId})";
				historyPanel.userIcon.texture = steamUser.profilePicture;
			}

			historyPanel.userInfo.text = $"{steamName}\nLoading...";

			historyPanel.gameObject.SetActive(true);

			// Get user info

			var res = await AngryLeaderboards.GetUserHistoryTask(steamId);
			historyPanel.loadingCircle.SetActive(false);

			if (!res.completedSuccessfully || res.status != AngryLeaderboards.GetUserHistoryStatus.OK)
			{
				historyPanel.userInfo.text = $"{steamName}\n<color=red>{res.message}</color>";
				return;
			}

			// Display

			Regex richText = new Regex(@"<[^>]*>");

			var history = res.response;
			int pageNum = 0;
			const int entriesPerPage = 100;

			Dictionary<int, string> bundlePkToGuid = history.bundleGuidPK
				.ToDictionary(pair => pair.Value, pair => pair.Key);
			Dictionary<int, string> levelPkToId = history.levelIdPK
				.ToDictionary(pair => pair.Value, pair => pair.Key);
			Dictionary<string, string> bundleGuidToName = history.bundleGuidPK
				.ToDictionary(pair => pair.Key, pair =>
				{
					var bundle = OnlineCatalogManager.Catalog.Levels.Where(b => b.Guid == pair.Key).FirstOrDefault();
					return bundle == null ? pair.Key : bundle.Name;
				});
			Dictionary<string, string> levelIdToName = history.levelIdPK
				.ToDictionary(pair => pair.Key, pair =>
				{
					var level = OnlineCatalogManager.Catalog.Levels.Select(b => b.Levels).SelectMany(x => x).Where(l => l.LevelId == pair.Key).FirstOrDefault();
					return level == null ? pair.Key : level.LevelName;
				});
			Dictionary<int, string> categoryMap = new Dictionary<int, string>()
			{
				{ 0, "all" },
				{ 1, "p rank" },
				{ 2, "challenge" },
				{ 3, "no monsters" },
				{ 4, "no monsters & weapons" },
			};
			Dictionary<int, string> difficultyMap = new Dictionary<int, string>()
			{
				{ 0, "harmless" },
				{ 1, "lenient" },
				{ 2, "standard" },
				{ 3, "violent" },
				{ 4, "brutal" },
			};

			List<int[]> filteredHistory = new List<int[]>();

			/*
				    z.number(), // 0 date
					z.number(), // 1 bundleGuidId
					z.number(), // 2 levelIdId
					z.number(), // 3 category
					z.number(), // 4 difficulty
					z.number(), // 5 time
			*/

			void RefreshList(bool sort)
			{
				historyPanel.userInfo.text = $"{steamName}\nTotal records: {history.runHistory.Length}\nTotal filtered: {filteredHistory.Count}";
				historyEntries.ForEach(e => e.gameObject.SetActive(false));

				if (sort)
				{
					switch (historyPanel.sortOrder.value)
					{
						case 0:
							if (historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => e2[0] - e1[0]);
							else
								filteredHistory.Sort((e1, e2) => e1[0] - e2[0]);
							break;

						case 1:
							if (historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => e2[5] - e1[5]);
							else
								filteredHistory.Sort((e1, e2) => e1[5] - e2[5]);
							break;

						case 2:
							Dictionary<int, int> bundleIdToOrder = history.bundleGuidPK.Select(pair =>
							{
								var bundle = OnlineCatalogManager.Catalog.Levels.Where(b => b.Guid == pair.Key).FirstOrDefault();
								return new KeyValuePair<int, string>(pair.Value, bundle == null ? null : richText.Replace(bundle.Name, string.Empty).ToLower());
							}).Where(pair => pair.Value != null)
							.OrderBy(pair => pair.Value)
							.Select((pair, i) => new KeyValuePair<int, int>(pair.Key, i))
							.ToDictionary(pair => pair.Key, pair => pair.Value);

							if (historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => bundleIdToOrder[e2[1]] - bundleIdToOrder[e1[1]]);
							else
								filteredHistory.Sort((e1, e2) => bundleIdToOrder[e1[1]] - bundleIdToOrder[e2[1]]);
							break;

						case 3:
							Dictionary<int, int> levelIdToOrder = history.levelIdPK.Select(pair =>
							{
								var level = OnlineCatalogManager.Catalog.Levels.Select(b => b.Levels).SelectMany(x => x).Where(l => l.LevelId == pair.Key).FirstOrDefault();
								return new KeyValuePair<int, string>(pair.Value, level == null ? null : richText.Replace(level.LevelName, string.Empty).ToLower());
							}).Where(pair => pair.Value != null)
							.OrderBy(pair => pair.Value)
							.Select((pair, i) => new KeyValuePair<int, int>(pair.Key, i))
							.ToDictionary(pair => pair.Key, pair => pair.Value);

							if (historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => levelIdToOrder[e2[2]] - levelIdToOrder[e1[2]]);
							else
								filteredHistory.Sort((e1, e2) => levelIdToOrder[e1[2]] - levelIdToOrder[e2[2]]);
							break;

						case 4:
							if (historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => e2[3] - e1[3]);
							else
								filteredHistory.Sort((e1, e2) => e1[3] - e2[3]);
							break;

						case 5:
							if (historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => e2[4] - e1[4]);
							else
								filteredHistory.Sort((e1, e2) => e1[4] - e2[4]);
							break;
					}
				}

				int baseIndex = Math.Max(0, pageNum * entriesPerPage);
				for (int i = 0; i < entriesPerPage; i++)
				{
					int offset = baseIndex + i;
					if (offset >= filteredHistory.Count)
						break;

					if (offset >= historyEntries.Count)
					{
						AngryUserRecordEntryComponent inst = UnityEngine.Object.Instantiate(historyPanel.template.gameObject, historyPanel.template.transform.parent).GetComponent<AngryUserRecordEntryComponent>();
						historyEntries.Add(inst);
					}

					var entry = historyEntries[i];
					var record = filteredHistory[offset];

					string bundleGuid = bundlePkToGuid[record[1]];
					string levelId = levelPkToId[record[2]];

					DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					updateTime = updateTime.AddSeconds(record[0]);
					string updateTimeString = updateTime.ToString("d");

					entry.bundleInfo.text = $"{bundleGuidToName[bundleGuid]} -- {levelIdToName[levelId]}";
					entry.recordInfo.text = $"<color=grey>Posted on {updateTimeString}</color>\nCategory: {categoryMap[record[3]]}\nDifficulty: {difficultyMap[record[4]]}";
					entry.time.text = MillisecondsToString(record[5]);

					AngryOnlineThumbnailCache.GetThumbnail(bundleGuid).ContinueWith(res =>
					{
						entry.bundleIcon.texture = res.Result;
					}, TaskScheduler.FromCurrentSynchronizationContext());

					AngryLevelThumbnailCache.GetThumbnail(bundleGuid, levelId).ContinueWith(res =>
					{
						entry.levelIcon.texture = res.Result;
					}, TaskScheduler.FromCurrentSynchronizationContext());

					entry.gameObject.SetActive(true);
				}
			}

			void ReloadData()
			{
				IEnumerable<int[]> historyIt = history.runHistory;

				// Filter category
				int categoryFilter = historyPanel.categoryFilter.value - 1;
				if (categoryFilter != -1)
					historyIt = historyIt.Where(e => e[3] == categoryFilter);

				// Filter difficulty
				int difficultyFilter = historyPanel.difficultyFilter.value - 1;
				if (difficultyFilter != -1)
					historyIt = historyIt.Where(e => e[4] == difficultyFilter);

				char[] whitespaceSeparator = new char[] { ' ' };
				string[] bundleSearchKeys = historyPanel.bundleName.text.Split(whitespaceSeparator, StringSplitOptions.RemoveEmptyEntries).Select(keyword => keyword.ToLower()).ToArray();
				string[] levelSearchKeys = historyPanel.levelName.text.Split(whitespaceSeparator, StringSplitOptions.RemoveEmptyEntries).Select(keyword => keyword.ToLower()).ToArray();

				// Filter bundles
				if (bundleSearchKeys.Length > 0)
				{
					HashSet<int> allowedBundles = new HashSet<int>();

					foreach (var bundle in OnlineCatalogManager.Catalog.Levels)
					{
						string bundleName = richText.Replace(bundle.Name, string.Empty).ToLower();
						if (bundleSearchKeys.Any(key => bundleName.IndexOf(key) == -1))
							continue;

						if (history.bundleGuidPK.TryGetValue(bundle.Guid, out int bundleKey))
							allowedBundles.Add(bundleKey);
					}

					historyIt = historyIt.Where(e => allowedBundles.Contains(e[1]));
				}

				// Filter levels
				if (levelSearchKeys.Length > 0)
				{
					HashSet<int> allowedLevels = new HashSet<int>();

					foreach (var bundle in OnlineCatalogManager.Catalog.Levels)
					{
						foreach (var level in bundle.Levels)
						{
							string levelName = richText.Replace(level.LevelName, string.Empty).ToLower();
							if (levelSearchKeys.Any(key => levelName.IndexOf(key) == -1))
								continue;

							if (history.levelIdPK.TryGetValue(level.LevelId, out int levelKey))
								allowedLevels.Add(levelKey);
						}
					}

					historyIt = historyIt.Where(e => allowedLevels.Contains(e[2]));
				}

				filteredHistory.Clear();
				filteredHistory.AddRange(historyIt);
				pageNum = 0;
				historyPanel.pageNumber.SetTextWithoutNotify("1");
				RefreshList(true);
			}

			historyPanel.nextPageButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			historyPanel.prevPageButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			historyPanel.pageNumber.onSubmit = new UnityEngine.UI.InputField.SubmitEvent();

			historyPanel.nextPageButton.onClick.AddListener(() =>
			{
				pageNum = Math.Clamp(pageNum + 1, 0, Math.Max(0, (filteredHistory.Count - 1) / entriesPerPage));
				historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
				RefreshList(false);
			});

			historyPanel.prevPageButton.onClick.AddListener(() =>
			{
				pageNum = Math.Clamp(pageNum - 1, 0, Math.Max(0, (filteredHistory.Count - 1) / entriesPerPage));
				historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
				RefreshList(false);
			});

			historyPanel.pageNumber.onSubmit.AddListener((e) =>
			{
				if (!int.TryParse(e, out int newPageNum))
				{
					historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
					return;
				}

				pageNum = Math.Clamp(newPageNum - 1, 0, Math.Max(0, (filteredHistory.Count - 1) / entriesPerPage));
				historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
				RefreshList(false);
			});

			historyPanel.categoryFilter.onValueChanged.AddListener((e) => ReloadData());
			historyPanel.difficultyFilter.onValueChanged.AddListener((e) => ReloadData());
			historyPanel.sortOrder.onValueChanged.AddListener((e) => RefreshList(true));
			historyPanel.reverseOrder.onValueChanged.AddListener((e) => RefreshList(true));
			historyPanel.bundleName.onEndEdit.AddListener((e) => ReloadData());
			historyPanel.levelName.onEndEdit.AddListener((e) => ReloadData());

			historyPanel.inputGroup.interactable = true;
			ReloadData();
		}
	}
}
