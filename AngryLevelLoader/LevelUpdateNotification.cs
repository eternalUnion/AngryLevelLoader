using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using UnityEngine;

namespace AngryLevelLoader
{
	public class LevelUpdateNotification : NotificationPanel.Notification
	{
		public string currentHash;
		public LevelInfo onlineInfo;
		public OnlineLevelField callback;

		public override void OnUI(RectTransform panel)
		{
			RectTransform header = UIUtils.MakeText(panel, "<color=cyan>Update Info</color>", 30, TextAnchor.UpperCenter);
			header.anchorMin = new Vector2(0, 1);
			header.anchorMax = new Vector2(1, 1);
			header.sizeDelta = new Vector2(0, 70);
			header.pivot = new Vector2(0.5f, 1);
			header.anchoredPosition = new Vector2(0, -50);

			RectTransform updatePanel = UIUtils.MakePanel(panel, 5);

			StringBuilder updateTextBuilder = new StringBuilder();
			bool firstTime = true;
			int currentLevel = onlineInfo.Updates.Count - 1;
			for (; currentLevel >= 0 && onlineInfo.Updates[currentLevel].Hash != currentHash; currentLevel--)
			{
				if (!firstTime)
				{
					updateTextBuilder.Append("\n\n<color=#b2b2b2>Past Version</color>");
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

			if (currentLevel >= 0)
			{
				updateTextBuilder.Append("\n\n<color=yellow>Current Version</color>");
				updateTextBuilder.Append("<size=18>\n");
				updateTextBuilder.Append(onlineInfo.Updates[currentLevel].Message.Replace(@"\n", "\n"));
				updateTextBuilder.Append("</size>");
			}
			else
			{
				updateTextBuilder.Append("\n\n<color=red>End of updates, unknown version</color>");
			}

			RectTransform updateText = UIUtils.MakeText(updatePanel, updateTextBuilder.ToString(), 28, TextAnchor.UpperLeft);
			updateText.anchorMin = new Vector2(0, 1);
			updateText.anchorMax = new Vector2(0, 1);
			updateText.sizeDelta = new Vector2(600, updateText.GetComponent<Text>().preferredHeight);
			updateText.pivot = new Vector2(0, 1);
			updateText.anchoredPosition = new Vector2(0, 0);

			LayoutRebuilder.ForceRebuildLayoutImmediate(updatePanel);

			RectTransform cancelButton = UIUtils.MakeButton(panel, "Cancel");
			cancelButton.anchorMin = new Vector2(0.5f, 0);
			cancelButton.anchorMax = new Vector2(0.5f, 0);
			cancelButton.pivot = new Vector2(1, 0);
			cancelButton.anchoredPosition = new Vector2(-5, 10);
			cancelButton.sizeDelta = new Vector2(295, 60);
			Button cancel = cancelButton.GetComponent<Button>();
			cancel.onClick.AddListener(() =>
			{
				Close();
			});

			RectTransform updateButton = UIUtils.MakeButton(panel, "Update");
			updateButton.anchorMin = new Vector2(0.5f, 0);
			updateButton.anchorMax = new Vector2(0.5f, 0);
			updateButton.pivot = new Vector2(0, 0);
			updateButton.anchoredPosition = new Vector2(5, 10);
			updateButton.sizeDelta = new Vector2(295, 60);
			Button update = updateButton.GetComponent<Button>();
			update.onClick.AddListener(() =>
			{
				callback.StartDownload();
				Close();
			});
		}
	}
}
