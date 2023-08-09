using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public class ScriptWarningNotification : NotificationPanel.Notification
	{
		public string header;
		public string text;
		public string leftButtonName;
		public string rightButtonName;
		public string topButtonName;
		public Action<ScriptWarningNotification> leftButton;
		public Action<ScriptWarningNotification> rightButton;
		public Action<ScriptWarningNotification> topButton;

		public ScriptWarningNotification(string header, string text, string leftButtonName, string rightButtonName, Action<ScriptWarningNotification> leftButton, Action<ScriptWarningNotification> rightButton, string topButtonName, Action<ScriptWarningNotification> topButton)
		{
			this.header = header;
			this.text = text;
			this.leftButtonName = leftButtonName;
			this.rightButtonName = rightButtonName;
			this.topButtonName = topButtonName;
			this.leftButton = leftButton;
			this.rightButton = rightButton;
			this.topButton = topButton;
		}

		public ScriptWarningNotification(string header, string text, string leftButtonName, string rightButtonName, Action<ScriptWarningNotification> leftButton, Action<ScriptWarningNotification> rightButton) : this(header, text, leftButtonName, rightButtonName, leftButton, rightButton, "", null)
		{

		}

		public override void OnUI(RectTransform panel)
		{
			RectTransform header = UIUtils.MakeText(panel, this.header, 30, TextAnchor.UpperCenter);
			header.anchorMin = new Vector2(0, 1);
			header.anchorMax = new Vector2(1, 1);
			header.sizeDelta = new Vector2(0, 70);
			header.pivot = new Vector2(0.5f, 1);
			header.anchoredPosition = new Vector2(0, -200);

			RectTransform body = UIUtils.MakeText(panel, this.text, 24, TextAnchor.UpperLeft);
			body.anchorMin = new Vector2(0.5f, 1);
			body.anchorMax = new Vector2(0.5f, 1);
			body.sizeDelta = new Vector2(600, 500);
			body.pivot = new Vector2(0.5f, 1);
			body.anchoredPosition = new Vector2(0, -250);

			RectTransform leftButton = UIUtils.MakeButton(panel, leftButtonName);
			leftButton.anchorMin = new Vector2(0.5f, 0);
			leftButton.anchorMax = new Vector2(0.5f, 0);
			leftButton.pivot = new Vector2(1, 0);
			leftButton.anchoredPosition = new Vector2(-5, 10);
			leftButton.sizeDelta = new Vector2(295, 60);
			Button left = leftButton.GetComponent<Button>();
			left.onClick.AddListener(() =>
			{
				this.leftButton(this);
			});

			RectTransform rightButton = UIUtils.MakeButton(panel, rightButtonName);
			rightButton.anchorMin = new Vector2(0.5f, 0);
			rightButton.anchorMax = new Vector2(0.5f, 0);
			rightButton.pivot = new Vector2(0, 0);
			rightButton.anchoredPosition = new Vector2(5, 10);
			rightButton.sizeDelta = new Vector2(295, 60);
			Button right = rightButton.GetComponent<Button>();
			right.onClick.AddListener(() =>
			{
				this.rightButton(this);
			});

			if (topButton != null)
			{
				RectTransform topButton = UIUtils.MakeButton(panel, topButtonName);
				topButton.anchorMin = new Vector2(0.5f, 0);
				topButton.anchorMax = new Vector2(0.5f, 0);
				topButton.pivot = new Vector2(0.5f, 0);
				topButton.anchoredPosition = new Vector2(0, 80);
				topButton.sizeDelta = new Vector2(600, 60);
				Button top = topButton.GetComponent<Button>();
				top.onClick.AddListener(() =>
				{
					this.topButton(this);
				});
			}
		}
	}
}
