using AngryLevelLoader.Managers;
using AngryLevelLoader.Managers.ServerManager;
using AngryUiComponents;
using Discord;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TextCore.Text;

namespace AngryLevelLoader.Notifications
{
    public class LeaderboardNotification : NotificationPanel.Notification
	{
		public readonly string bundleName;
		public readonly string levelName;

		public readonly string bundleGuid;
		public readonly string levelId;

		private Texture2D unknownProfile;

		public LeaderboardNotification(string bundleName, string levelName, string bundleGuid, string levelId)
		{
			this.bundleName = bundleName;
			this.levelName = levelName;
			this.bundleGuid = bundleGuid;
			this.levelId = levelId;
		}

		private class RefreshCircleSpin : MonoBehaviour
		{
			private void Update()
			{
				transform.Rotate(new Vector3(0, 0, Time.unscaledDeltaTime * 360f));
			}
		}

		private class ExitListener : MonoBehaviour
		{
			public LeaderboardNotification callback;

			private void Update()
			{
				if (InputManager.Instance.InputSource.Pause.WasPerformedThisFrame)
				{
					if (callback != null)
						callback.Close();
				}
			}
		}

		private const string ASSET_PATH = "AngryLevelLoader/Notifications/AngryLeaderboardNotification.prefab";

		private AngryLeaderboardNotificationComponent currentUi;

		private int currentPage = 0;
		private int totalRecordCount = 0;
		private AngryLeaderboards.UserRecord[] friendRecords = null;

		public override void OnUI(RectTransform panel)
		{
			try
			{
				currentUi = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryLeaderboardNotificationComponent>();
				currentUi.gameObject.AddComponent<ExitListener>().callback = this;

				currentUi.header.text = $"Leaderboards for\n{bundleName} - {levelName}";

				currentUi.closeButton.onClick.AddListener(() =>
				{
					Close();
				});

				currentUi.refreshCircle.AddComponent<RefreshCircleSpin>();
				currentUi.reportLoadCircle.AddComponent<RefreshCircleSpin>();
				currentUi.historyPanel.loadingCircle.AddComponent<RefreshCircleSpin>();

				currentUi.category.value = (int)ConfigManager.defaultLeaderboardCategory.value;
				currentUi.difficulty.interactable = currentUi.category.value != 3 && currentUi.category.value != 4;
				currentUi.category.onValueChanged.AddListener((index) =>
				{
					currentUi.difficulty.interactable = index != 3 && index != 4;
					currentPage = 0;
					currentUi.pageInput.SetTextWithoutNotify("1");
					totalRecordCount = 0;
					friendRecords = null;
					Reload(true);
				});

				currentUi.difficulty.value = (int)ConfigManager.defaultLeaderboardDifficulty.value;
				currentUi.difficulty.onValueChanged.AddListener((index) =>
				{
					currentPage = 0;
					currentUi.pageInput.SetTextWithoutNotify("1");
					totalRecordCount = 0;
					friendRecords = null;
					Reload(true);
				});

				currentUi.group.value = (int)ConfigManager.defaultLeaderboardFilter.value;
				currentUi.group.onValueChanged.AddListener((index) =>
				{
					currentPage = 0;
					currentUi.pageInput.SetTextWithoutNotify("1");
					totalRecordCount = 0;
					friendRecords = null;
					Reload(true);
				});

				currentUi.nextPage.onClick.AddListener(() =>
				{
					currentPage += 1;
					currentUi.pageInput.SetTextWithoutNotify((currentPage + 1).ToString());
					Reload(false);
				});

				currentUi.prevPage.onClick.AddListener(() =>
				{
					currentPage -= 1;
					currentUi.pageInput.SetTextWithoutNotify((currentPage + 1).ToString());
					Reload(false);
				});

				currentUi.pageInput.onEndEdit.AddListener((newVal) =>
				{
					if (int.TryParse(currentUi.pageInput.text, out int newPage))
					{
						if (newPage < 1)
							newPage = 1;

						currentPage = newPage - 1;
						currentUi.pageInput.SetTextWithoutNotify(newPage.ToString());

						Reload(false);
					}
					else
					{
						currentUi.pageInput.SetTextWithoutNotify(currentPage.ToString());
					}
				});

				currentUi.refreshButton.onClick.AddListener(() =>
				{
					Reload(true);
				});

				currentUi.reportCancel.onClick.AddListener(() =>
				{
					currentUi.reportToggle.SetActive(false);
				});

				currentUi.inappropriateName.onValueChanged.AddListener((newVal) => UpdateReportUI());
				currentUi.inappropriatePicture.onValueChanged.AddListener((newVal) => UpdateReportUI());
				currentUi.cheatedScore.onValueChanged.AddListener((newVal) => UpdateReportUI());
				currentUi.reportValidation.onValueChanged.AddListener((newVal) => UpdateReportUI());
				currentUi.reportReturn.onClick.AddListener(() =>
				{
					currentUi.reportToggle.SetActive(false);
				});

				Reload(true);

				unknownProfile = currentUi.manageUserPanel.userIcon.texture as Texture2D;
			}
			catch (Exception e)
			{
				Plugin.logger.LogError($"Exception thrown while opening leaderboards\n{e}");
				Close();
			}
		}

		private Task currentReloadTask;
		private void Reload(bool getLocalUser)
		{
			if (currentReloadTask != null && !currentReloadTask.IsCompleted)
				return;

			currentUi.category.interactable = false;
			currentUi.difficulty.interactable = false;
			currentUi.group.interactable = false;

			currentUi.localUserRecordContainer.SetActive(!getLocalUser);
			currentUi.recordEnabler.SetActive(false);
			int recordCount = currentUi.recordContainer.childCount;
			for (int i = recordCount - 1; i >= 1; i--)
				UnityEngine.Object.Destroy(currentUi.recordContainer.GetChild(i).gameObject);
			currentUi.refreshButton.gameObject.SetActive(false);
			currentUi.failMessage.gameObject.SetActive(false);
			currentUi.refreshCircle.SetActive(true);

			currentUi.nextPage.interactable = false;
			currentUi.prevPage.interactable = false;
			currentUi.pageInput.interactable = false;

			currentReloadTask = ReloadTask(getLocalUser).ContinueWith((task) =>
			{
				if (task.Exception != null)
					Debug.LogException(task.Exception);

				if (currentUi != null)
				{
					currentUi.refreshCircle.SetActive(false);
					currentUi.category.interactable = true;
					currentUi.difficulty.interactable = currentUi.category.value != 3 && currentUi.category.value != 4;
					currentUi.group.interactable = true;
				}
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private static readonly AngryLeaderboards.RecordCategory[] dropdownCategories = new AngryLeaderboards.RecordCategory[]
		{
			AngryLeaderboards.RecordCategory.ALL,
			AngryLeaderboards.RecordCategory.PRANK,
			AngryLeaderboards.RecordCategory.CHALLENGE,
			AngryLeaderboards.RecordCategory.NOMO,
			AngryLeaderboards.RecordCategory.NOMOW,
		};

		private static readonly AngryLeaderboards.RecordDifficulty[] dropdownDifficulties = new AngryLeaderboards.RecordDifficulty[]
		{
			AngryLeaderboards.RecordDifficulty.ANY,
			AngryLeaderboards.RecordDifficulty.HARMLESS,
			AngryLeaderboards.RecordDifficulty.LENIENT,
			AngryLeaderboards.RecordDifficulty.STANDARD,
			AngryLeaderboards.RecordDifficulty.VIOLENT,
			AngryLeaderboards.RecordDifficulty.BRUTAL,
		};

		private static string MillisecondsToString(int milliseconds)
		{
			int minutes = milliseconds / 60000;
			float seconds = (float)(milliseconds - minutes * 60000) / 1000f;
			return string.Format("{0}:{1:00.000}", minutes, seconds);
		}
		private const string NullTime = "--:--.---";

		private async Task ReloadTask(bool getLocalUser)
		{
			AngryLeaderboards.RecordCategory category = dropdownCategories[currentUi.category.value];
			AngryLeaderboards.RecordDifficulty difficulty = dropdownDifficulties[currentUi.difficulty.value];
			if (category == AngryLeaderboards.RecordCategory.NOMO || category == AngryLeaderboards.RecordCategory.NOMOW)
				difficulty = AngryLeaderboards.RecordDifficulty.HARMLESS;
			bool allFilter = currentUi.group.value == 0;

			if (getLocalUser)
			{
				var localUserResult = await AngryLeaderboards.GetUserRecordTask(category, difficulty, bundleGuid, levelId, Steamworks.SteamClient.SteamId.ToString());
				
				if (currentUi != null)
				{
					if (SteamCacheManager.TryGetUser(Steamworks.SteamClient.SteamId, out var localUser))
					{
						currentUi.localUserPfp.texture = localUser.profilePicture;
					}
					else
					{
						currentUi.localUserPfp.texture = (await SteamCacheManager.RequestUser(Steamworks.SteamClient.SteamId)).profilePicture;
					}
					currentUi.localUserName.text = Steamworks.SteamClient.Name;

					if (localUserResult.completedSuccessfully && localUserResult.status == AngryLeaderboards.GetUserRecordStatus.OK)
					{
						currentUi.localUserRank.text = localUserResult.response.ranking == -1 ? "#?" : $"#{localUserResult.response.ranking}";
						if (difficulty == AngryLeaderboards.RecordDifficulty.ANY && localUserResult.response.ranking != -1)
							currentUi.localUserName.text += $"\n<color=grey>{localUserResult.response.difficulty}</color>";
						currentUi.localUserTime.text = localUserResult.response.ranking == -1 ? NullTime : MillisecondsToString(localUserResult.response.time);
					}
					else
					{
						currentUi.localUserRank.text = "#?";
						currentUi.localUserTime.text = NullTime;
					}
				}
			}

			int pageLimit = currentPage;
			if (allFilter)
			{
				var page = await AngryLeaderboards.GetRecordsTask(category, difficulty, bundleGuid, levelId, currentPage * 5, 5);
				totalRecordCount = page.response.totalCount;
				pageLimit = totalRecordCount == 0 ? 0 : (totalRecordCount - 1) / 5;

				if (currentUi != null)
				{
					if (page.completedSuccessfully)
					{
						if (page.status == AngryLeaderboards.GetRecordsStatus.OK)
						{
							if (currentPage > pageLimit)
							{
								currentPage = pageLimit;
								currentUi.pageInput.SetTextWithoutNotify((currentPage + 1).ToString());
								await ReloadTask(false);
								return;
							}

							for (int i = 0; i < page.response.records.Length; i++)
							{
								var entry = UnityEngine.Object.Instantiate(currentUi.recordTemplate, currentUi.recordContainer);
								string entryDifficulty = difficulty == AngryLeaderboards.RecordDifficulty.ANY ? page.response.records[i].difficulty : "";
								entry.rank.text = $"#{page.response.offset + i + 1}";
								entry.time.text = MillisecondsToString(page.response.records[i].time);

								ulong steamId = ulong.Parse(page.response.records[i].steamId);
								string currentUserId = Steamworks.SteamClient.SteamId.ToString();
								if (steamId.ToString() == currentUserId)
									entry.reportButton.interactable = false;

								entry.username.text = "<color=grey>Loading...</color>";
								if (difficulty == AngryLeaderboards.RecordDifficulty.ANY)
									entry.username.text += $"\n<color=grey>{entryDifficulty}</color>";

								AngryLeaderboards.RecordDifficulty actualDifficulty = (difficulty == AngryLeaderboards.RecordDifficulty.ANY) ? AngryLeaderboards.RECORD_DIFFICULTY_REVERSE_DICT[page.response.records[i].difficulty] : difficulty;
								bool isCurrentUser = page.response.records[i].steamId == currentUserId;
								bool censorIcon = !isCurrentUser && page.response.records[i].censorIcon;
								bool censorName = !isCurrentUser && page.response.records[i].censorName;

								SteamCacheManager.RequestUser(steamId, (user) =>
								{
									if (entry == null)
										return;

									entry.username.text = (censorName) ? "(hidden)" : user.name;
									if (difficulty == AngryLeaderboards.RecordDifficulty.ANY)
										entry.username.text += $"\n<color=grey>{entryDifficulty}</color>";
									if (!censorIcon)
										entry.profile.texture = user.profilePicture;

									entry.reportButton.onClick.AddListener(() => {
										if (AngryUser.hasLeaderboardPermissions)
											ManageButton(steamId.ToString(), category, actualDifficulty);
										else
											ReportButton(steamId.ToString(), user.name);
									});
								});

								entry.gameObject.SetActive(true);
							}
						}
						else
						{
							currentUi.refreshButton.gameObject.SetActive(true);

							switch (page.status)
							{
								case AngryLeaderboards.GetRecordsStatus.INVALID_BUNDLE:
								case AngryLeaderboards.GetRecordsStatus.INVALID_ID:
									currentUi.failMessage.text = "Leaderboards disabled for this level";
									currentUi.refreshButton.gameObject.SetActive(false);
									break;

								case AngryLeaderboards.GetRecordsStatus.RATE_LIMITED:
									currentUi.failMessage.text = "Too many requests";
									break;

								default:
									currentUi.failMessage.text = $"Status Error\n{page.status}";
									break;
							}

							currentUi.failMessage.gameObject.SetActive(true);
							return;
						}
					}
					else
					{
						currentUi.failMessage.text = (page.networkError) ? "Network Error\nCheck connection" : "Server Error\nTry again later";
						currentUi.failMessage.gameObject.SetActive(true);
						currentUi.refreshButton.gameObject.SetActive(true);
						return;
					}
				}
			}
			else
			{
				if (friendRecords == null)
				{
					var friendPage = await AngryLeaderboards.GetUserRecordsTask(category, difficulty, bundleGuid, levelId, Steamworks.SteamFriends.GetFriends().Select(f => f.Id.ToString()).Concat(new string[] {Steamworks.SteamClient.SteamId.ToString()}.AsEnumerable()));
					if (friendPage.completedSuccessfully && friendPage.status == AngryLeaderboards.GetUserRecordsStatus.OK)
						friendRecords = friendPage.response.records;

					if (currentUi != null)
					{
						if (friendPage.completedSuccessfully)
						{
							if (friendPage.status != AngryLeaderboards.GetUserRecordsStatus.OK)
							{
								currentUi.refreshButton.gameObject.SetActive(true);

								switch (friendPage.status)
								{
									case AngryLeaderboards.GetUserRecordsStatus.INVALID_BUNDLE:
									case AngryLeaderboards.GetUserRecordsStatus.INVALID_ID:
										currentUi.failMessage.text = "Leaderboards disabled for this level";
										currentUi.refreshButton.gameObject.SetActive(false);
										break;

									case AngryLeaderboards.GetUserRecordsStatus.RATE_LIMITED:
										currentUi.failMessage.text = "Too many requests";
										break;

									default:
										currentUi.failMessage.text = $"Status Error\n{friendPage.status}";
										break;
								}

								currentUi.failMessage.gameObject.SetActive(true);
								return;
							}
						}
						else
						{
							currentUi.failMessage.text = (friendPage.networkError) ? "Network Error\nCheck connection" : "Server Error\nTry again later";
							currentUi.failMessage.gameObject.SetActive(true);
							currentUi.refreshButton.gameObject.SetActive(true);
							return;
						}
					}
				}

				totalRecordCount = friendRecords.Length;
				pageLimit = totalRecordCount == 0 ? 0 : (totalRecordCount - 1) / 5;

				if (currentPage > pageLimit)
				{
					currentPage = pageLimit;
					currentUi.pageInput.SetTextWithoutNotify((currentPage + 1).ToString());
					await ReloadTask(false);
					return;
				}

				if (currentUi != null)
				{
					int limit = Math.Min(5, friendRecords.Length - currentPage * 5);
					for (int i = 0; i < limit; i++)
					{
						int realIndex = i + currentPage * 5;

						var entry = UnityEngine.Object.Instantiate(currentUi.recordTemplate, currentUi.recordContainer);
						string entryDifficulty = difficulty == AngryLeaderboards.RecordDifficulty.ANY ? friendRecords[realIndex].difficulty : "";
						entry.rank.text = $"#{currentPage * 5 + i + 1}";
						entry.time.text = MillisecondsToString(friendRecords[realIndex].time);

						ulong steamId = ulong.Parse(friendRecords[realIndex].steamId);
						if (steamId.ToString() == Steamworks.SteamClient.SteamId.ToString())
							entry.reportButton.interactable = false;

						entry.username.text = $"<color=grey>Loading...</color> <color=silver>(global #{friendRecords[realIndex].globalRank})</color>";
						if (difficulty == AngryLeaderboards.RecordDifficulty.ANY)
							entry.username.text += $"\n<color=grey>{entryDifficulty}</color>";

						SteamCacheManager.RequestUser(steamId, (user) =>
						{
							if (entry == null)
								return;

							entry.username.text = $"{user.name} <color=silver>(global #{friendRecords[realIndex].globalRank})</color>";
							if (difficulty == AngryLeaderboards.RecordDifficulty.ANY)
								entry.username.text += $"\n<color=grey>{entryDifficulty}</color>";
							entry.profile.texture = user.profilePicture;
							entry.reportButton.onClick.AddListener(() => ReportButton(steamId.ToString(), user.name));
						});

						entry.gameObject.SetActive(true);
					}
				}
			}

			if (currentUi != null)
			{
				currentUi.localUserRecordContainer.SetActive(true);
				currentUi.recordEnabler.SetActive(true);

				currentUi.pageInput.interactable = true;
				if (currentPage != 0)
					currentUi.prevPage.interactable = true;
				if (currentPage < pageLimit)
					currentUi.nextPage.interactable = true;
			}
		}

		private void ReportButton(string steamId, string username)
		{
			if (steamId == Steamworks.SteamClient.SteamId.ToString())
				return;

			currentUi.reportFormToggle.gameObject.SetActive(true);
			currentUi.reportResultToggle.gameObject.SetActive(false);
			
			currentUi.reportBody.text = $"Report <color=#00FFFF>{username}</color>?";
			ResetReportUI();

			currentUi.reportSend.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.reportSend.onClick.AddListener(() => SendReport(steamId, username));

			currentUi.reportToggle.SetActive(true);
		}

		private void ManageButton(string steamId, AngryLeaderboards.RecordCategory category, AngryLeaderboards.RecordDifficulty difficulty)
		{
			if (steamId == Steamworks.SteamClient.SteamId.ToString())
				return;

			currentUi.manageUserPanel.managePanel.SetActive(true);
			currentUi.manageUserPanel.resultPanel.SetActive(false);

			currentUi.manageUserPanel.userInfo.text = "Loading...";
			currentUi.manageUserPanel.userIcon.texture = unknownProfile;

			currentUi.manageUserPanel.cencorProfilePicture.isOn = false;
			currentUi.manageUserPanel.cencorProfileName.isOn = false;
			currentUi.manageUserPanel.banUser.isOn = false;
			currentUi.manageUserPanel.removeRecord.isOn = true;
			currentUi.manageUserPanel.removeAll.isOn = false;

			currentUi.manageUserPanel.removeRecord.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
			currentUi.manageUserPanel.removeRecord.onValueChanged.AddListener((e) =>
			{
				currentUi.manageUserPanel.removeAll.interactable = e;
				if (!e) currentUi.manageUserPanel.removeAll.isOn = false;
			});

			currentUi.manageUserPanel.cancelManage.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.cancelManage.onClick.AddListener(() => {
				currentUi.manageUserPanel.gameObject.SetActive(false);
			});

			currentUi.manageUserPanel.applyManage.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.applyManage.onClick.AddListener(() =>
			{
				if (currentUi.manageUserPanel.removeAll.isOn)
				{
					_ = OpenWarningWindow(steamId, category, difficulty);
					return;
				}

				ManageUser(steamId, category, difficulty).ContinueWith((res) => {
					currentUi.manageUserPanel.returnButton.interactable = true;
				}, TaskScheduler.FromCurrentSynchronizationContext());

				currentUi.manageUserPanel.resultPanel.SetActive(true);
			});

			currentUi.manageUserPanel.historyButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.historyButton.onClick.AddListener(() =>
			{
				currentUi.manageUserPanel.managePanel.SetActive(false);
				currentUi.manageUserPanel.gameObject.SetActive(false);
				_ = OpenHistoryWindow(steamId);
			});

			string name = steamId;

			if (ulong.TryParse(steamId, out ulong steamIdNum) && SteamCacheManager.TryGetUser(steamIdNum, out SteamUserCache user))
			{
				name = user.name;
				currentUi.manageUserPanel.userIcon.texture = currentUi.manageUserPanel.warningUserIcon.texture = user.profilePicture;
			}

			AngryLeaderboards.GetUserInfoTask(steamId).ContinueWith((res) =>
			{
				if (!res.IsCompletedSuccessfully)
				{
					currentUi.manageUserPanel.userInfo.text = $"<color=red>Failed to obtain user info!</color>";
					return;
				}

				if (!res.Result.completedSuccessfully || res.Result.status != AngryLeaderboards.GetUserInfoStatus.OK)
				{
					currentUi.manageUserPanel.userInfo.text = $"<color=red>Failed to obtain user info!\n{res.Result.message}</color>";
					return;
				}

				string bannedText = $"Banned: {(res.Result.response.leaderboardBanned ? "<color=red>yes</color>" : "<color=green>no</color>")}";
				currentUi.manageUserPanel.userInfo.text = currentUi.manageUserPanel.warningUserInfo.text = $"{name}\n{bannedText}\nRecord count: {res.Result.response.recordCount}\nReceived reports: {res.Result.response.reportCount}\nRemoved records: {res.Result.response.removedRecordCount}";
			}, TaskScheduler.FromCurrentSynchronizationContext());

			currentUi.manageUserPanel.gameObject.SetActive(true);
		}

		private void SendReport(string steamId, string username)
		{
			currentUi.reportFormToggle.SetActive(false);
			currentUi.reportReturn.interactable = false;
			currentUi.reportLoadCircle.SetActive(true);
			currentUi.reportBody.text = "Sending...";
			currentUi.reportResultToggle.SetActive(true);

			string reason = "";
			if (currentUi.inappropriateName.isOn)
				reason += "INAPPROPRIATE NAME, ";
			if (currentUi.inappropriatePicture.isOn)
				reason += "INAPPROPRIATE PICTURE, ";
			if (currentUi.cheatedScore.isOn)
				reason += "CHEATED SCORE, ";

			var sendReportTask = AngryUser.ReportTask(AngryLeaderboards.RECORD_CATEGORY_DICT[dropdownCategories[currentUi.category.value]], AngryLeaderboards.RECORD_DIFFICULTY_DICT[dropdownDifficulties[currentUi.difficulty.value]], bundleGuid, levelId, steamId, reason);
			sendReportTask.ContinueWith((task) =>
			{
				if (currentUi == null)
					return;

				currentUi.reportLoadCircle.SetActive(false);
				currentUi.reportReturn.interactable = true;
				var result = task.Result;

				if (result.completedSuccessfully)
				{
					switch (result.status)
					{
						case AngryUser.ReportStatus.OK:
							currentUi.reportBody.text = result.response.alreadySent ? "Report updated" : "Report sent successfuly";
							break;

						case AngryUser.ReportStatus.INVALID_ENTRY:
							currentUi.reportBody.text = "Record no longer exists";
							break;

						case AngryUser.ReportStatus.BANNED:
							currentUi.reportBody.text = "You have been banned from sending reports";
							break;

						case AngryUser.ReportStatus.RATE_LIMITED:
							currentUi.reportBody.text = "Too many requests, try again later";
							break;

						case AngryUser.ReportStatus.INVALID_TARGET_ID:
							currentUi.reportBody.text = "Target steam ID not found";
							break;

						default:
							currentUi.reportBody.text = $"Unknown error. Message: '{result.message}'. Status: {result.status}";
							break;
					}
				}
				else if (result.networkError)
				{
					currentUi.reportBody.text = "Network error, check connection";
				}
				else if (result.httpError)
				{
					currentUi.reportBody.text = "Server error, try again later";
				}
				else
				{
					currentUi.reportBody.text = "Could not send the report due to an unknown error";
				}
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}
		
		private async Task OpenWarningWindow(string steamId, AngryLeaderboards.RecordCategory category, AngryLeaderboards.RecordDifficulty difficulty)
		{
			currentUi.manageUserPanel.managePanel.SetActive(false);
			currentUi.manageUserPanel.resultPanel.SetActive(false);

			CancellationTokenSource cancelSource = new CancellationTokenSource();
			cancelSource.Token.ThrowIfCancellationRequested();

			currentUi.manageUserPanel.warningCancelButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.warningCancelButton.onClick.AddListener(() =>
			{
				cancelSource.Cancel();
				currentUi.manageUserPanel.warningPanel.SetActive(false);
				currentUi.manageUserPanel.gameObject.SetActive(false);
			});

			currentUi.manageUserPanel.warningSubmitButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.warningSubmitButton.onClick.AddListener(() =>
			{
				ManageUser(steamId, category, difficulty).ContinueWith((res) => {
					currentUi.manageUserPanel.returnButton.interactable = true;
				}, TaskScheduler.FromCurrentSynchronizationContext());

				currentUi.manageUserPanel.resultPanel.SetActive(true);
			});

			currentUi.manageUserPanel.warningSubmitButton.interactable = false;
			currentUi.manageUserPanel.warningPanel.SetActive(true);

			for (int i = 5; i > 0; i--)
			{
				currentUi.manageUserPanel.warningSubmitButtonText.text = $"Remove All ({i})";

				try
				{
					await Task.Delay(1000, cancelSource.Token);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}

			currentUi.manageUserPanel.warningSubmitButtonText.text = $"Remove All";
			currentUi.manageUserPanel.warningSubmitButton.interactable = true;
		}

		private async Task ManageUser(string steamId, AngryLeaderboards.RecordCategory category, AngryLeaderboards.RecordDifficulty difficulty)
		{
			currentUi.manageUserPanel.managePanel.SetActive(false);
			currentUi.manageUserPanel.warningPanel.SetActive(false);

			bool censorIcon = currentUi.manageUserPanel.cencorProfilePicture.isOn;
			bool censorName = currentUi.manageUserPanel.cencorProfileName.isOn;
			bool banUser = currentUi.manageUserPanel.banUser.isOn;
			bool removeRecord = currentUi.manageUserPanel.removeRecord.isOn;
			bool removeAll = currentUi.manageUserPanel.removeAll.isOn;

			currentUi.manageUserPanel.returnButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.returnButton.onClick.AddListener(() =>
			{
				currentUi.manageUserPanel.resultPanel.SetActive(false);
				currentUi.manageUserPanel.gameObject.SetActive(false);
				Reload(false);
			});

			if (!censorIcon && !censorName && !banUser && !removeRecord && !removeAll)
			{
				currentUi.manageUserPanel.resultText.text = "Nothing to do";
				currentUi.manageUserPanel.resultPanel.SetActive(true);
				return;
			}

			currentUi.manageUserPanel.returnButton.interactable = false;
			currentUi.manageUserPanel.resultText.text = "";

			if (censorIcon || censorName || banUser)
			{
				currentUi.manageUserPanel.resultText.text += "Managing user... ";
				var res = await AngryLeaderboards.ManageUserTask(steamId, censorIcon, censorName, banUser);

				currentUi.manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.ManageUserStatus.OK) ? $"<color=red>{res.message}</color>\n" : "<color=green>Success!</color>\n";
			}

			if (removeAll)
			{
				currentUi.manageUserPanel.resultText.text += "Removing all records... ";
				var res = await AngryLeaderboards.ClearRecordsTask(steamId);

				currentUi.manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.ClearRecordsStatus.OK) ? $"<color=red>{res.message}</color>\n" : $"<color=green>Removed {res.response.removedRecordCount} records!</color>\n";
			}
			else if (removeRecord)
			{
				currentUi.manageUserPanel.resultText.text += "Removing record... ";
				var res = await AngryLeaderboards.RemoveRecordTask(steamId, bundleGuid, levelId, category, difficulty);

				currentUi.manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.RemoveRecordStatus.OK) ? $"<color=red>{res.message}</color>\n" : "<color=green>Success!</color>\n";
			}
		}

		private List<AngryUserRecordEntryComponent> historyEntries = new List<AngryUserRecordEntryComponent>();

		private async Task OpenHistoryWindow(string steamId)
		{
			currentUi.historyPanel.backButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.historyPanel.backButton.onClick.AddListener(() =>
			{
				currentUi.historyPanel.gameObject.SetActive(false);
			});

			currentUi.historyPanel.categoryFilter.onValueChanged = new UnityEngine.UI.Dropdown.DropdownEvent();
			currentUi.historyPanel.difficultyFilter.onValueChanged = new UnityEngine.UI.Dropdown.DropdownEvent();
			currentUi.historyPanel.sortOrder.onValueChanged = new UnityEngine.UI.Dropdown.DropdownEvent();
			currentUi.historyPanel.reverseOrder.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
			currentUi.historyPanel.bundleName.onEndEdit = new UnityEngine.UI.InputField.EndEditEvent();
			currentUi.historyPanel.levelName.onEndEdit = new UnityEngine.UI.InputField.EndEditEvent();

			currentUi.historyPanel.categoryFilter.value = 0;
			currentUi.historyPanel.difficultyFilter.value = 0;
			currentUi.historyPanel.sortOrder.value = 0;
			currentUi.historyPanel.reverseOrder.isOn = false;
			currentUi.historyPanel.bundleName.SetTextWithoutNotify("");
			currentUi.historyPanel.levelName.SetTextWithoutNotify("");

			currentUi.historyPanel.inputGroup.interactable = false;

			historyEntries.ForEach(e => e.gameObject.SetActive(false));
			currentUi.historyPanel.loadingCircle.SetActive(true);

			string steamName = steamId;
			if (ulong.TryParse(steamId, out ulong steamIdNum) && SteamCacheManager.TryGetUser(steamIdNum, out SteamUserCache steamUser))
			{
				steamName = $"{steamUser.name} ({steamId})";
				currentUi.historyPanel.userIcon.texture = steamUser.profilePicture;
			}
			else
			{
				currentUi.historyPanel.userIcon.texture = unknownProfile;
			}

			currentUi.historyPanel.userInfo.text = $"{steamName}\nLoading...";

			currentUi.historyPanel.gameObject.SetActive(true);

			// Get user info

			var res = await AngryLeaderboards.GetUserHistoryTask(steamId);
			currentUi.historyPanel.loadingCircle.SetActive(false);

			if (!res.completedSuccessfully || res.status != AngryLeaderboards.GetUserHistoryStatus.OK)
			{
				currentUi.historyPanel.userInfo.text = $"{steamName}\n<color=red>{res.message}</color>";
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
				currentUi.historyPanel.userInfo.text = $"{steamName}\nTotal records: {history.runHistory.Length}\nTotal filtered: {filteredHistory.Count}";
				historyEntries.ForEach(e => e.gameObject.SetActive(false));

				if (sort)
				{
					switch(currentUi.historyPanel.sortOrder.value)
					{
						case 0:
							if (currentUi.historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => e2[0] - e1[0]);
							else
								filteredHistory.Sort((e1, e2) => e1[0] - e2[0]);
							break;

						case 1:
							if (currentUi.historyPanel.reverseOrder.isOn)
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

							if (currentUi.historyPanel.reverseOrder.isOn)
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

							if (currentUi.historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => levelIdToOrder[e2[2]] - levelIdToOrder[e1[2]]);
							else
								filteredHistory.Sort((e1, e2) => levelIdToOrder[e1[2]] - levelIdToOrder[e2[2]]);
							break;

						case 4:
							if (currentUi.historyPanel.reverseOrder.isOn)
								filteredHistory.Sort((e1, e2) => e2[3] - e1[3]);
							else
								filteredHistory.Sort((e1, e2) => e1[3] - e2[3]);
							break;

						case 5:
							if (currentUi.historyPanel.reverseOrder.isOn)
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
						AngryUserRecordEntryComponent inst = UnityEngine.Object.Instantiate(currentUi.historyPanel.template.gameObject, currentUi.historyPanel.template.transform.parent).GetComponent<AngryUserRecordEntryComponent>();
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
				int categoryFilter = currentUi.historyPanel.categoryFilter.value - 1;
				if (categoryFilter != -1)
					historyIt = historyIt.Where(e => e[3] == categoryFilter);

				// Filter difficulty
				int difficultyFilter = currentUi.historyPanel.difficultyFilter.value - 1;
				if (difficultyFilter != -1)
					historyIt = historyIt.Where(e => e[4] == difficultyFilter);

				char[] whitespaceSeparator = new char[] { ' ' };
				string[] bundleSearchKeys = currentUi.historyPanel.bundleName.text.Split(whitespaceSeparator, StringSplitOptions.RemoveEmptyEntries).Select(keyword => keyword.ToLower()).ToArray();
				string[] levelSearchKeys = currentUi.historyPanel.levelName.text.Split(whitespaceSeparator, StringSplitOptions.RemoveEmptyEntries).Select(keyword => keyword.ToLower()).ToArray();
			
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
				currentUi.historyPanel.pageNumber.SetTextWithoutNotify("1");
				RefreshList(true);
			}

			currentUi.historyPanel.nextPageButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.historyPanel.prevPageButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.historyPanel.pageNumber.onSubmit = new UnityEngine.UI.InputField.SubmitEvent();

			currentUi.historyPanel.nextPageButton.onClick.AddListener(() =>
			{
				pageNum = Math.Clamp(pageNum + 1, 0, Math.Max(0, (filteredHistory.Count - 1) / entriesPerPage));
				currentUi.historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
				RefreshList(false);
			});

			currentUi.historyPanel.prevPageButton.onClick.AddListener(() =>
			{
				pageNum = Math.Clamp(pageNum - 1, 0, Math.Max(0, (filteredHistory.Count - 1) / entriesPerPage));
				currentUi.historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
				RefreshList(false);
			});

			currentUi.historyPanel.pageNumber.onSubmit.AddListener((e) =>
			{
				if (!int.TryParse(e, out int newPageNum))
				{
					currentUi.historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
					return;
				}

				pageNum = Math.Clamp(newPageNum - 1, 0, Math.Max(0, (filteredHistory.Count - 1) / entriesPerPage));
				currentUi.historyPanel.pageNumber.SetTextWithoutNotify((pageNum + 1).ToString());
				RefreshList(false);
			});

			currentUi.historyPanel.categoryFilter.onValueChanged.AddListener((e) => ReloadData());
			currentUi.historyPanel.difficultyFilter.onValueChanged.AddListener((e) => ReloadData());
			currentUi.historyPanel.sortOrder.onValueChanged.AddListener((e) => RefreshList(true));
			currentUi.historyPanel.reverseOrder.onValueChanged.AddListener((e) => RefreshList(true));
			currentUi.historyPanel.bundleName.onEndEdit.AddListener((e) => ReloadData());
			currentUi.historyPanel.levelName.onEndEdit.AddListener((e) => ReloadData());

			currentUi.historyPanel.inputGroup.interactable = true;
			ReloadData();
		}

		private void ResetReportUI()
		{
			currentUi.reportSend.interactable = false;
			currentUi.inappropriateName.SetIsOnWithoutNotify(false);
			currentUi.inappropriatePicture.SetIsOnWithoutNotify(false);
			currentUi.cheatedScore.SetIsOnWithoutNotify(false);
			currentUi.reportValidation.SetIsOnWithoutNotify(false);
		}

		private void UpdateReportUI()
		{
			currentUi.reportSend.interactable = currentUi.reportValidation.isOn && (currentUi.inappropriateName.isOn || currentUi.inappropriatePicture.isOn || currentUi.cheatedScore.isOn);
		}
	}
}
