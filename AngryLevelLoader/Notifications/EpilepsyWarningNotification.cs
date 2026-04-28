using AngryLevelLoader.Managers;
using AngryUiComponents;
using PluginConfig;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Notifications
{
	internal class EpilepsyWarningNotification : NotificationPanel.Notification
	{
		private const string ASSET_PATH = "AngryLevelLoader/Notifications/EpilepsyWarningNotification.prefab";

		private string continueText;
		private string continueAndIgnoreText;
		private Action onContinue;

		public EpilepsyWarningNotification(Action onContinue, string continueText, string continueAndIgnoreText)
		{
			this.onContinue = onContinue;
			this.continueText = continueText;
			this.continueAndIgnoreText = continueAndIgnoreText;
		}

		private AngryEpilepsyWarningNotificationComponent ui = null;

		public override void OnUI(RectTransform panel)
		{
			ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryEpilepsyWarningNotificationComponent>();

			ui.cancelButton.onClick.AddListener(() =>
			{
				Close();
			});

			ui.continueButtonText.text = continueText;
			ui.continueAndIgnoreButtonText.text = continueAndIgnoreText;

			ui.continueButton.onClick.AddListener(() =>
			{
				Close();
				if (onContinue != null)
					onContinue();
			});

			ui.continueAndIgnoreButton.onClick.AddListener(() =>
			{
				Close();
				InternalConfigManager.ignoreEpilepsyWarning.value = true;
				if (onContinue != null)
					onContinue();
			});
		}
	}
}
