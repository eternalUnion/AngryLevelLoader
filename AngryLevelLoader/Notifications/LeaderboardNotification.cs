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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

				currentUi.category.value = (int)Plugin.defaultLeaderboardCategory.value;
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

				currentUi.difficulty.value = (int)Plugin.defaultLeaderboardDifficulty.value;
				currentUi.difficulty.onValueChanged.AddListener((index) =>
				{
					currentPage = 0;
					currentUi.pageInput.SetTextWithoutNotify("1");
					totalRecordCount = 0;
					friendRecords = null;
					Reload(true);
				});

				currentUi.group.value = (int)Plugin.defaultLeaderboardFilter.value;
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

			currentUi.manageUserPanel.cancelManage.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.cancelManage.onClick.AddListener(() => {
				currentUi.manageUserPanel.gameObject.SetActive(false);
			});

			currentUi.manageUserPanel.applyManage.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.applyManage.onClick.AddListener(() =>
			{
				ManageUser(steamId, category, difficulty).ContinueWith((res) => {
					currentUi.manageUserPanel.returnButton.interactable = true;
				}, TaskScheduler.FromCurrentSynchronizationContext());

				currentUi.manageUserPanel.resultPanel.SetActive(true);
			});

			string name = steamId;

			if (ulong.TryParse(steamId, out ulong steamIdNum) && SteamCacheManager.TryGetUser(steamIdNum, out SteamUserCache user))
			{
				name = user.name;
				currentUi.manageUserPanel.userIcon.texture = user.profilePicture;
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
				currentUi.manageUserPanel.userInfo.text = $"{name}\n{bannedText}\nRecord count: {res.Result.response.recordCount}\nReceived reports: {res.Result.response.reportCount}";
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
		
		private async Task ManageUser(string steamId, AngryLeaderboards.RecordCategory category, AngryLeaderboards.RecordDifficulty difficulty)
		{
			currentUi.manageUserPanel.managePanel.SetActive(false);

			bool censorIcon = currentUi.manageUserPanel.cencorProfilePicture.isOn;
			bool censorName = currentUi.manageUserPanel.cencorProfileName.isOn;
			bool banUser = currentUi.manageUserPanel.banUser.isOn;
			bool removeRecord = currentUi.manageUserPanel.removeRecord.isOn;

			currentUi.manageUserPanel.returnButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			currentUi.manageUserPanel.returnButton.onClick.AddListener(() =>
			{
				currentUi.manageUserPanel.resultPanel.SetActive(false);
				currentUi.manageUserPanel.gameObject.SetActive(false);
				Reload(false);
			});

			if (!censorIcon && !censorName && !banUser && !removeRecord)
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

			if (removeRecord)
			{
				currentUi.manageUserPanel.resultText.text += "Removing record... ";
				var res = await AngryLeaderboards.RemoveRecordTask(steamId, bundleGuid, levelId, category, difficulty);

				currentUi.manageUserPanel.resultText.text += (!res.completedSuccessfully || res.status != AngryLeaderboards.RemoveRecordStatus.OK) ? $"<color=red>{res.message}</color>\n" : "<color=green>Success!</color>\n";
			}
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
