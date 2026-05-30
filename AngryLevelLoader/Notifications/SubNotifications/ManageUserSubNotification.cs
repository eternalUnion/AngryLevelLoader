using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using AngryUiComponents;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AngryLevelLoader.Notifications.SubNotifications
{
	internal class ManageUserSubNotification
	{
		private readonly AngryManageUserComponent manageUserPanel;
		private readonly UserHistorySubNotification historyPanelNotification;
		internal event Action onExit;

		public ManageUserSubNotification(AngryManageUserComponent manageUserPanel, UserHistorySubNotification historyPanelNotification)
		{
			this.manageUserPanel = manageUserPanel;
			this.historyPanelNotification = historyPanelNotification;
		}

		internal void Show(string steamId)
		{
			Show(steamId, null, null, AngryLeaderboards.RecordCategory.ALL, AngryLeaderboards.RecordDifficulty.STANDARD);
		}

		internal void Show(string steamId, string bundleGuid, string levelId, AngryLeaderboards.RecordCategory category, AngryLeaderboards.RecordDifficulty difficulty)
		{
			manageUserPanel.userInfo.text = $"Show {steamId}";
			if (steamId == Steamworks.SteamClient.SteamId.ToString())
				return;

			manageUserPanel.managePanel.SetActive(true);
			manageUserPanel.resultPanel.SetActive(false);

			manageUserPanel.cancelManage.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			manageUserPanel.cancelManage.onClick.AddListener(() => {
				manageUserPanel.gameObject.SetActive(false);
			});

			manageUserPanel.userInfo.text = "Loading...";
			manageUserPanel.userIcon.texture = AssetManager.unknownProfile;

			manageUserPanel.cencorProfilePicture.isOn = false;
			manageUserPanel.cencorProfileName.isOn = false;
			manageUserPanel.banUser.isOn = false;
			manageUserPanel.removeAll.isOn = false;
			manageUserPanel.banFromReports.isOn = false;
			manageUserPanel.removeAllSentReports.isOn = false;

			manageUserPanel.removeRecord.interactable = bundleGuid != null && levelId != null;
			manageUserPanel.removeRecord.isOn = bundleGuid != null && levelId != null;

			manageUserPanel.removeRecord.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
			manageUserPanel.removeRecord.onValueChanged.AddListener((e) =>
			{
				manageUserPanel.removeAll.interactable = e;
				if (!e) manageUserPanel.removeAll.isOn = false;
			});

			manageUserPanel.removeAll.interactable = bundleGuid != null && levelId != null;

			manageUserPanel.applyManage.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			manageUserPanel.applyManage.onClick.AddListener(() =>
			{
				if (manageUserPanel.removeAll.isOn)
				{
					_ = OpenWarningWindow(steamId, bundleGuid, levelId, category, difficulty);
					return;
				}

				ManageUser(steamId, bundleGuid, levelId, category, difficulty).ContinueWith((res) => {
					manageUserPanel.returnButton.interactable = true;
				}, TaskScheduler.FromCurrentSynchronizationContext());

				manageUserPanel.resultPanel.SetActive(true);
			});

			manageUserPanel.historyButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			manageUserPanel.historyButton.onClick.AddListener(() =>
			{
				manageUserPanel.managePanel.SetActive(false);
				manageUserPanel.gameObject.SetActive(false);
				_ = historyPanelNotification.OpenHistoryWindow(steamId);
			});

			string name = steamId;

			if (ulong.TryParse(steamId, out ulong steamIdNum) && SteamCacheManager.TryGetUser(steamIdNum, out SteamUserCache user))
			{
				name = user.name;
				manageUserPanel.userIcon.texture = manageUserPanel.warningUserIcon.texture = user.profilePicture;
			}

			AngryLeaderboards.GetUserInfoTask(steamId).ContinueWith((res) =>
			{
				if (!res.IsCompletedSuccessfully)
				{
					manageUserPanel.userInfo.text = $"<color=red>Failed to obtain user info!</color>";
					return;
				}

				if (!res.Result.completedSuccessfully || res.Result.status != AngryLeaderboards.GetUserInfoStatus.OK)
				{
					manageUserPanel.userInfo.text = $"<color=red>Failed to obtain user info!\n{res.Result.message}</color>";
					return;
				}

				string bannedText = $"Banned: {(res.Result.response.leaderboardBanned ? "<color=red>yes</color>" : "<color=green>no</color>")}";
				manageUserPanel.userInfo.text = manageUserPanel.warningUserInfo.text = $"{name}\n{bannedText}\nRecord count: {res.Result.response.recordCount}\nReceived reports: {res.Result.response.reportCount}\nRemoved records: {res.Result.response.removedRecordCount}";
			}, TaskScheduler.FromCurrentSynchronizationContext());

			manageUserPanel.gameObject.SetActive(true);
		}

		private async Task OpenWarningWindow(string steamId, string bundleGuid, string levelId, AngryLeaderboards.RecordCategory category, AngryLeaderboards.RecordDifficulty difficulty)
		{
			manageUserPanel.managePanel.SetActive(false);
			manageUserPanel.resultPanel.SetActive(false);

			CancellationTokenSource cancelSource = new CancellationTokenSource();
			cancelSource.Token.ThrowIfCancellationRequested();

			manageUserPanel.warningCancelButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			manageUserPanel.warningCancelButton.onClick.AddListener(() =>
			{
				cancelSource.Cancel();
				manageUserPanel.warningPanel.SetActive(false);
				manageUserPanel.gameObject.SetActive(false);
			});

			manageUserPanel.warningSubmitButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			manageUserPanel.warningSubmitButton.onClick.AddListener(() =>
			{
				ManageUser(steamId, bundleGuid, levelId, category, difficulty).ContinueWith((res) => {
					manageUserPanel.returnButton.interactable = true;
				}, TaskScheduler.FromCurrentSynchronizationContext());

				manageUserPanel.resultPanel.SetActive(true);
			});

			manageUserPanel.warningSubmitButton.interactable = false;
			manageUserPanel.warningPanel.SetActive(true);

			for (int i = 5; i > 0; i--)
			{
				manageUserPanel.warningSubmitButtonText.text = $"Remove All ({i})";

				try
				{
					await Task.Delay(1000, cancelSource.Token);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}

			manageUserPanel.warningSubmitButtonText.text = $"Remove All";
			manageUserPanel.warningSubmitButton.interactable = true;
		}

		private async Task ManageUser(string steamId, string bundleGuid, string levelId, AngryLeaderboards.RecordCategory category, AngryLeaderboards.RecordDifficulty difficulty)
		{
			manageUserPanel.managePanel.SetActive(false);
			manageUserPanel.warningPanel.SetActive(false);

			bool censorIcon = manageUserPanel.cencorProfilePicture.isOn;
			bool censorName = manageUserPanel.cencorProfileName.isOn;
			bool banUser = manageUserPanel.banUser.isOn;
			bool banReports = manageUserPanel.banFromReports.isOn;
			bool removeAllReports = manageUserPanel.removeAllSentReports.isOn;
			bool removeRecord = manageUserPanel.removeRecord.isOn;
			bool removeAll = manageUserPanel.removeAll.isOn;

			manageUserPanel.returnButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			manageUserPanel.returnButton.onClick.AddListener(() =>
			{
				manageUserPanel.resultPanel.SetActive(false);
				manageUserPanel.gameObject.SetActive(false);
				onExit?.Invoke();
			});

			if (!censorIcon && !censorName && !banUser && !removeRecord && !removeAll && !removeAllReports && !banReports)
			{
				manageUserPanel.resultText.text = "Nothing to do";
				manageUserPanel.resultPanel.SetActive(true);
				return;
			}

			manageUserPanel.returnButton.interactable = false;
			manageUserPanel.resultText.text = "";

			if (censorIcon || censorName || banUser || banReports)
			{
				manageUserPanel.resultText.text += "Managing user... ";
				var res = await AngryLeaderboards.ManageUserTask(steamId, censorIcon, censorName, banUser, banReports);

				manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.ManageUserStatus.OK) ? $"<color=red>{res.message}</color>\n" : "<color=green>Success!</color>\n";
			}

			if (removeAllReports)
			{
				manageUserPanel.resultText.text += "Removing all records... ";
				var res = await AngryLeaderboards.ClearReportsTask(senderId: steamId);

				manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.ClearReportsStatus.OK) ? $"<color=red>{res.message}</color>\n" : $"<color=green>Removed {res.response.removedReports} reports!</color>\n";
			}

			if (removeAll)
			{
				manageUserPanel.resultText.text += "Removing all records... ";
				var res = await AngryLeaderboards.ClearRecordsTask(steamId);

				manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.ClearRecordsStatus.OK) ? $"<color=red>{res.message}</color>\n" : $"<color=green>Removed {res.response.removedRecordCount} records!</color>\n";
			}
			else if (removeRecord && bundleGuid != null && levelId != null)
			{
				manageUserPanel.resultText.text += "Removing record... ";
				var res = await AngryLeaderboards.RemoveRecordTask(steamId, bundleGuid, levelId, category, difficulty);

				manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.RemoveRecordStatus.OK) ? $"<color=red>{res.message}</color>\n" : "<color=green>Success!</color>\n";
			}
		}
	}
}
