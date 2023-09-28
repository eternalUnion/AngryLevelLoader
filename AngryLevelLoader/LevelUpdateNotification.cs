using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using UnityEngine;
using AngryUiComponents;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader
{
	public class LevelUpdateNotification : NotificationPanel.Notification
	{
		private const string ASSET_PATH = "AngryLevelLoader/LevelUpdateNotification.prefab";

		public string currentHash;
		public LevelInfo onlineInfo;
		public OnlineLevelField callback;

		public override void OnUI(RectTransform panel)
		{
			AngryLevelUpdateNotificationComponent ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryLevelUpdateNotificationComponent>();

			StringBuilder updateTextBuilder = new StringBuilder();
			bool firstTime = true;

			for (int currentLevel = onlineInfo.Updates.Count - 1; currentLevel >= 0; currentLevel--)
			{
				if (!firstTime)
				{
					if (onlineInfo.Updates[currentLevel].Hash != currentHash)
						updateTextBuilder.Append("\n\n<color=#b2b2b2>Past Version</color>");
					else
                        updateTextBuilder.Append("\n\n<color=yellow>Current Version</color>");
                }
				else
				{
					updateTextBuilder.Append("<color=lime>Latest Version</color>");
                }

				updateTextBuilder.Append("<size=18>\n");
				updateTextBuilder.Append(onlineInfo.Updates[currentLevel].Message.Replace(@"\n", "\n"));
				updateTextBuilder.Append("</size>");

				firstTime = false;
			}

			// if (!currentVersionFound)
			//	updateTextBuilder.Append("\n\n<color=red>End of updates, current version unknown</color>");

			ui.body.text = updateTextBuilder.ToString();
			ui.cancel.onClick.AddListener(() =>
			{
				Close();
			});
			ui.update.onClick.AddListener(() =>
			{
				callback.StartDownload();
				Close();
			});
			if (onlineInfo.Hash == currentHash)
				ui.update.interactable = false;
		}
	}
}
