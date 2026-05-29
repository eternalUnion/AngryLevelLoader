using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using AngryLevelLoader.Notifications.SubNotifications;
using AngryUiComponents;
using PluginConfig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Notifications
{
	internal class ReportViewNotification : NotificationPanel.Notification
	{
		private class ExitListener : MonoBehaviour
		{
			public ReportViewNotification callback;

			private void Update()
			{
				if (InputManager.Instance.InputSource.Pause.WasPerformedThisFrame)
				{
					if (callback != null)
						callback.Close();
				}
			}
		}

		private class ReceivedReport
		{
			public string receiverId;
			public List<AngryLeaderboards.Report> reports;
		}

		private const string ASSET_PATH = "AngryLevelLoader/Notifications/ReportViewNotification.prefab";

		private CancellationTokenSource cancelRequest = new CancellationTokenSource();
		private AngryReportViewComponent currentUi;
		private UserHistorySubNotification userHistoryNotification;
		private ManageUserSubNotification manageUserNotification;

		private List<ReceivedReport> reports = new List<ReceivedReport>();
		private ReceivedReport currentReport;
		private int currentReportIndex = 0;
		private int currentPage = 0;
		private int totalRecordCount = 0;

		public override void OnUI(RectTransform panel)
		{
			try
			{
				currentUi = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryReportViewComponent>();
				currentUi.gameObject.AddComponent<ExitListener>().callback = this;

				userHistoryNotification = new UserHistorySubNotification(currentUi.historyPanel);
				manageUserNotification = new ManageUserSubNotification(currentUi.manageUserPanel, userHistoryNotification);

				currentUi.backButton.onClick.AddListener(() =>
				{
					Close();
					cancelRequest.Cancel();
				});

				currentUi.nextReportButton.onClick.AddListener(() =>
				{
					ViewReport(++currentReportIndex);
				});

				currentUi.previousReportButton.onClick.AddListener(() =>
				{
					ViewReport(--currentReportIndex);
				});

				currentUi.nextPageButton.onClick.AddListener(() =>
				{
					ViewPage(++currentPage);
				});

				currentUi.prevPageButton.onClick.AddListener(() =>
				{
					ViewPage(--currentPage);
				});

				if (!AngryUser.hasLeaderboardPermissions)
				{
					currentUi.errorText.text = "Permission denied";
					return;
				}

				_ = LoadReports();
			}
			catch (Exception e)
			{
				Plugin.logger.LogError($"Exception thrown while opening report view\n{e}");
				Close();
				cancelRequest.Cancel();
			}
		}

		~ReportViewNotification()
		{
			if (cancelRequest != null)
				cancelRequest.Dispose();
		}

		private async Task LoadReports()
		{
			var getReportsRequest = await AngryLeaderboards.GetAllReportsTask(cancelRequest.Token);
			if (!getReportsRequest.completedSuccessfully)
			{
				currentUi.errorText.text = (getReportsRequest.networkError) ? "Network error, check connection" : "Http error, server may be offline";
				return;
			}

			if (getReportsRequest.status != AngryLeaderboards.GetAllReportsStatus.OK)
			{
				switch (getReportsRequest.status)
				{
					case AngryLeaderboards.GetAllReportsStatus.INTERNAL_ERROR:
						currentUi.errorText.text = "Server encountered an error!";
						break;

					case AngryLeaderboards.GetAllReportsStatus.ACCESS_DENIED:
						currentUi.errorText.text = "Permission denied";
						break;

					case AngryLeaderboards.GetAllReportsStatus.FAILED:
						currentUi.errorText.text = $"Request failed ({getReportsRequest.message})";
						break;

					case AngryLeaderboards.GetAllReportsStatus.RATE_LIMITED:
						currentUi.errorText.text = $"Sent too many requests in a short time";
						break;
				}

				return;
			}

			reports = getReportsRequest.response.reports.Select(pair => new ReceivedReport() { receiverId=pair.Key, reports=pair.Value.ToList() }).ToList();

			if (reports.Count == 0)
			{
				currentUi.errorText.text = "No reports to view!";
				return;
			}

			ViewReport(0);
		}

		private IEnumerator RemoveAllReportsDelay()
		{
			for (int i = 3; i >= 1; i--)
			{
				currentUi.removeAllReportsText.text = $"Remove all received reports ({i})";
				yield return new WaitForSecondsRealtime(1f);
			}

			currentUi.removeAllReportsText.text = "Remove all received reports";
			currentUi.removeAllReportsText.color = Color.red;
			currentUi.removeAllReportsButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.removeAllReportsButton.onClick.AddListener(() =>
			{
				currentUi.removeAllReportsText.text = "Remove all received reports";
				currentUi.removeAllReportsText.color = Color.white;
				currentUi.removeAllReportsButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
				currentUi.removeAllReportsButton.onClick.AddListener(() =>
				{
					currentUi.StartCoroutine(RemoveAllReportsDelay());
					currentUi.removeAllReportsButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
				});

				ReceivedReport targetReport = currentReport;
				string targetId = currentReport.receiverId;

				AngryLeaderboards.ClearReportsTask(receiverId: targetId).ContinueWith((task) =>
				{
					var result = task.Result;

					if (!result.completedSuccessfully)
					{
						Plugin.logger.LogWarning($"Failed to remove record! network error: {result.networkError}, http error: {result.httpError}");
						return;
					}

					if (result.status != AngryLeaderboards.ClearReportsStatus.OK)
					{
						Plugin.logger.LogWarning($"Failed to remove record! status: {result.status}, message: {result.message}");
						return;
					}

					Plugin.logger.LogMessage($"Cleared {result.response.removedReports} reports");

					reports.Remove(targetReport);

					if (currentReport.receiverId == targetId)
					{
						Transform container = currentUi.template.transform.parent;
						for (int i = container.childCount - 1; i > 0; i--)
							GameObject.Destroy(container.GetChild(i).gameObject);
					}

				}, TaskScheduler.FromCurrentSynchronizationContext());
			});
		}

		private void ViewReport(int idx)
		{
			currentUi.StopAllCoroutines();

			currentUi.UIBlockGroup.interactable = false;
			currentUi.userIcon.texture = null;
			currentUi.userInfo.text = "";

			if (reports.Count == 0)
			{
				currentUi.errorText.text = "No reports to view!";
				return;
			}

			idx = Math.Clamp(idx, 0, reports.Count - 1);
			currentReportIndex = idx;
			currentPage = 0;
			currentUi.previousReportButton.interactable = currentReportIndex > 0;
			currentUi.nextReportButton.interactable = currentReportIndex < reports.Count - 1;

			currentReport = reports[idx];

			if (ulong.TryParse(currentReport.receiverId, out ulong receiverSteamId))
			{
				currentUi.userInfo.text = $"loading... ({currentReport.receiverId})\nReceived reports: {currentReport.reports.Count}";
				SteamCacheManager.RequestUser(receiverSteamId, (userCache) =>
				{
					if (currentReport.receiverId != receiverSteamId.ToString())
						return;

					currentUi.userIcon.texture = userCache.profilePicture;
					currentUi.userInfo.text = $"{userCache.name} ({currentReport.receiverId})\nReceived reports: {currentReport.reports.Count}";
				});
			}
			else
			{
				currentUi.userInfo.text = $"<failed to get name> ({currentReport.receiverId})\nReceived reports: {currentReport.reports.Count}";
			}

			currentUi.viewUserHistoryButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.viewUserHistoryButton.onClick.AddListener(() =>
			{
				_ = userHistoryNotification.OpenHistoryWindow(currentReport.receiverId);
			});

			currentUi.removeAllReportsText.text = "Remove all received reports";
			currentUi.removeAllReportsText.color = Color.white;
			currentUi.removeAllReportsButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.removeAllReportsButton.onClick.AddListener(() =>
			{
				currentUi.removeAllReportsButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
				currentUi.StartCoroutine(RemoveAllReportsDelay());
			});

			ViewPage(0);
			currentUi.UIBlockGroup.interactable = true;
		}

		private void ViewPage(int pageIdx)
		{
			Transform container = currentUi.template.transform.parent;
			for (int i = container.childCount - 1; i > 0; i--)
				GameObject.Destroy(container.GetChild(i).gameObject);

			if (currentReport.reports.Count == 0)
			{
				currentUi.prevPageButton.interactable = false;
				currentUi.nextPageButton.interactable = false;
				return;
			}

			currentPage = Math.Clamp(pageIdx, 0, currentReport.reports.Count / 100);
			currentUi.prevPageButton.interactable = currentPage > 0;
			currentUi.nextPageButton.interactable = currentPage < currentReport.reports.Count / 100 - 1;

			for (int i = currentPage * 100; i < Math.Min(currentReport.reports.Count, currentPage * 100 + 100); i++)
			{
				AngryLeaderboards.Report entry = currentReport.reports[i];
				ReceivedReport receiver = currentReport;
				AngryReportEntryComponent entryObj = GameObject.Instantiate(currentUi.template.gameObject, container).GetComponent<AngryReportEntryComponent>();

				if (ulong.TryParse(entry.sender, out ulong senderSteamId))
				{
					entryObj.senderInfo.text = $"Sent by <loading...> ({senderSteamId})";
					SteamCacheManager.RequestUser(senderSteamId, (senderCache) =>
					{
						entryObj.senderInfo.text = $"Sent by {senderCache.name} ({senderSteamId})";
					});
				}
				else
				{
					entryObj.senderInfo.text = $"Sent by {senderSteamId}";
				}

				var catalog = OnlineCatalogManager.Catalog;
				if (catalog != null)
				{
					var bundle = catalog.Levels.FirstOrDefault(b => b.Guid == entry.reportObject.bundleGuid);
					var level = (bundle == null) ? null : bundle.Levels.FirstOrDefault(l => l.LevelId == entry.reportObject.levelId);

					if (bundle == null && OnlineCatalogManagerV1.Catalog != null)
						bundle = OnlineCatalogManagerV1.Catalog.Levels.FirstOrDefault(b => b.Guid == entry.reportObject.bundleGuid);

					if (level == null && bundle != null)
						level = (bundle == null) ? null : bundle.Levels.FirstOrDefault(l => l.LevelId == entry.reportObject.levelId);

					string bundleName = (bundle == null) ? entry.reportObject.bundleGuid : bundle.Name;
					string levelName = (level == null) ? entry.reportObject.levelId : level.LevelName;
					entryObj.bundleInfo.text = $"{bundleName} -- {levelName}";
				}
				else
				{
					entryObj.bundleInfo.text = $"{entry.reportObject.bundleGuid} -- {entry.reportObject.levelId}";
				}

				AngryOnlineThumbnailCache.GetThumbnail(entry.reportObject.bundleGuid, (bundleTexture) =>
				{
					if (entryObj == null)
						return;
					entryObj.bundleIcon.texture = bundleTexture;
				});

				AngryLevelThumbnailCache.GetThumbnail(entry.reportObject.bundleGuid, entry.reportObject.levelId, (levelTexture) =>
				{
					if (entryObj == null)
						return;
					entryObj.levelIcon.texture = levelTexture;
				});

				int minutes = entry.reportObject.time / 60000;
				float seconds = (float)(entry.reportObject.time - minutes * 60000) / 1000f;
				entryObj.time.text = string.Format("{0}:{1:00.000}", minutes, seconds);

				entryObj.manageReceiver.onClick.AddListener(() =>
				{
					manageUserNotification.Show(currentReport.receiverId, entry.reportObject.bundleGuid, entry.reportObject.levelId, entry.reportObject.category, entry.reportObject.difficulty);
				});

				entryObj.manageSender.onClick.AddListener(() =>
				{
					manageUserNotification.Show(entry.sender);
				});

				entryObj.markAsDone.onClick.AddListener(() =>
				{
					entryObj.markAsDone.interactable = false;

					AngryLeaderboards.RemoveReportTask(entry.sender, entry.targetId, entry.reportObject.bundleGuid, entry.reportObject.levelId, entry.reportObject.category, entry.reportObject.difficulty).ContinueWith(task =>
					{
						if (entryObj == null)
							return;

						var result = task.Result;
						if (!result.completedSuccessfully)
						{
							Plugin.logger.LogWarning($"Failed to remove record! network error: {result.networkError}, http error: {result.httpError}");
							entryObj.markAsDone.interactable = true;
							return;
						}

						if (result.status != AngryLeaderboards.RemoveReportStatus.OK)
						{
							Plugin.logger.LogWarning($"Failed to remove record! status: {result.status}, message: {result.message}");
							entryObj.markAsDone.interactable = true;
							return;
						}

						if (!result.response.removedReport)
							Plugin.logger.LogWarning("Requested to remove a report, but no such report existed!");

						GameObject.Destroy(entryObj.gameObject);
						receiver.reports.Remove(entry);
						if (receiver.reports.Count == 0)
							reports.Remove(receiver);

					}, TaskScheduler.FromCurrentSynchronizationContext());
				});

				entryObj.recordInfo.text = $"Reason: {entry.reason}\nCategory: {entry.reportObject.category}\nDifficulty: {entry.reportObject.difficulty}";
				
				entryObj.gameObject.SetActive(true);
			}
		}
	}
}
