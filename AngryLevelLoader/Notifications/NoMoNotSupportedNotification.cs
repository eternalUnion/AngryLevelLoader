using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryUiComponents;
using PluginConfig;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Notifications
{
	internal class NoMoNotSupportedNotification : NotificationPanel.Notification
	{
		private const string ASSET_PATH = "AngryLevelLoader/Notifications/NoMoNotSupportedNotification.prefab";

		private string continueText;
		private string continueAndIgnoreText;
		private Action onContinue;
		private LevelContainer levelContainer;

		public NoMoNotSupportedNotification(Action onContinue, string continueText, string continueAndIgnoreText, LevelContainer levelContainer)
		{
			this.onContinue = onContinue;
			this.continueText = continueText;
			this.continueAndIgnoreText = continueAndIgnoreText;
			this.levelContainer = levelContainer;
		}

		private AngryNoMoNotSupportedNotificationComponent ui = null;

		public override void OnUI(RectTransform panel)
		{
			ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryNoMoNotSupportedNotificationComponent>();

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
				if (!string.IsNullOrEmpty(InternalConfigManager.ignoreNoMoWarning.value))
					InternalConfigManager.ignoreNoMoWarning.value += '\n';
				InternalConfigManager.ignoreNoMoWarning.value += levelContainer.levelId;
				if (onContinue != null)
					onContinue();
			});
		}
	}
}
