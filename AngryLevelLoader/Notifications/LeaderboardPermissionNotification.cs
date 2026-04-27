using AngryLevelLoader.Managers;
using AngryUiComponents;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Notifications
{
	internal class LeaderboardPermissionNotification : NotificationPanel.Notification
	{
		private const string ASSET_PATH = "AngryLevelLoader/Notifications/LeaderboardPermissionNotification.prefab";

		private AngryLeaderboardPermissionNotificationComponent currentUi;

		public override void OnUI(RectTransform panel)
		{
			currentUi = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryLeaderboardPermissionNotificationComponent>();

			currentUi.okButton.onClick.AddListener(() =>
			{
				Close();
				InternalConfigManager.askedPermissionForLeaderboards.value = true;
				InternalConfigManager.leaderboardToggle.value = true;
			});

			currentUi.cancelButton.onClick.AddListener(() =>
			{
				Close();
				InternalConfigManager.askedPermissionForLeaderboards.value = true;
			});
		}
	}
}
