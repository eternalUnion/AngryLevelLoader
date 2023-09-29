using AngryUiComponents;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace AngryLevelLoader.Notifications
{
    public class ScriptWarningNotification : NotificationPanel.Notification
    {
        private const string ASSET_PATH = "AngryLevelLoader/ScriptWarningNotification.prefab";

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
            AngryScriptWarningNotificationComponent ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryScriptWarningNotificationComponent>();

            ui.header.text = header;
            ui.body.text = text;

            ui.leftButtonText.text = leftButtonName;
            ui.leftButton.onClick.AddListener(() =>
            {
                leftButton(this);
            });

            ui.rightButtonText.text = rightButtonName;
            ui.rightButton.onClick.AddListener(() =>
            {
                rightButton(this);
            });

            if (topButton != null)
            {
                ui.topButtonText.text = topButtonName;
                ui.topButton.onClick.AddListener(() =>
                {
                    topButton(this);
                });
            }
            else
                ui.topButton.gameObject.SetActive(false);
        }
    }
}
