using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using UnityEngine;

namespace AngryLevelLoader
{
    public class PluginVersion
    {
        public string version { get; set; }
        public string updateText { get; set; }
    }

    public class PluginInfoJson
    {
        public string latestVersion { get; set; }
        public List<PluginVersion> updates;
    }

    public class PluginUpdateNotification : NotificationPanel.Notification
    {
        private PluginInfoJson json;

        public PluginUpdateNotification(PluginInfoJson json)
        {
            this.json = json;
        }

        public override void OnUI(RectTransform panel)
        {
            string headerText = "<color=cyan>Changelog</color>";
            if (new Version(Plugin.PLUGIN_VERSION) < new Version(json.latestVersion))
                headerText = "<color=lime>UPDATE AVAILABLE</color>";

            RectTransform header = UIUtils.MakeText(panel, headerText, 30, TextAnchor.UpperCenter);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.sizeDelta = new Vector2(0, 70);
            header.pivot = new Vector2(0.5f, 1);
            header.anchoredPosition = new Vector2(0, -50);

            RectTransform updatePanel = UIUtils.MakePanel(panel, 5);

            StringBuilder updateTextBuilder = new StringBuilder();
            bool firstTime = true;
            int currentVersion = json.updates.Count - 1;
            for (; currentVersion >= 0; currentVersion--)
            {
                string version = json.updates[currentVersion].version;

                if (!firstTime)
                {
                    if (version == Plugin.PLUGIN_VERSION)
                        updateTextBuilder.Append($"\n\nV{version} <color=yellow>Current Version</color>");
                    else
                        updateTextBuilder.Append($"\n\nV{version} <color=#b2b2b2>Past Version</color>");
                }
                else
                {
                    updateTextBuilder.Append($"V{version} <color=lime>Latest Version</color>");
                }

                updateTextBuilder.Append("<size=18>\n");
                updateTextBuilder.Append(json.updates[currentVersion].updateText.Replace(@"\n", "\n"));
                updateTextBuilder.Append("</size>");

                firstTime = false;
            }

            RectTransform updateText = UIUtils.MakeText(updatePanel, updateTextBuilder.ToString(), 28, TextAnchor.UpperLeft);
            updateText.anchorMin = new Vector2(0, 1);
            updateText.anchorMax = new Vector2(0, 1);
            updateText.sizeDelta = new Vector2(600, updateText.GetComponent<Text>().preferredHeight);
            updateText.pivot = new Vector2(0, 1);
            updateText.anchoredPosition = new Vector2(0, 0);

            LayoutRebuilder.ForceRebuildLayoutImmediate(updatePanel);

            RectTransform cancelButton = UIUtils.MakeButton(panel, "Close");
            cancelButton.anchorMin = new Vector2(0.5f, 0);
            cancelButton.anchorMax = new Vector2(0.5f, 0);
            cancelButton.pivot = new Vector2(1, 0);
            cancelButton.anchoredPosition = new Vector2(-5, 10);
            cancelButton.sizeDelta = new Vector2(295, 60);
            Button cancel = cancelButton.GetComponent<Button>();
            cancel.onClick.AddListener(() =>
            {
                Close();
                Plugin.lastVersion.value = Plugin.PLUGIN_VERSION;
            });

            RectTransform updateButton = UIUtils.MakeButton(panel, "Ignore Until Next Update");
            updateButton.anchorMin = new Vector2(0.5f, 0);
            updateButton.anchorMax = new Vector2(0.5f, 0);
            updateButton.pivot = new Vector2(0, 0);
            updateButton.anchoredPosition = new Vector2(5, 10);
            updateButton.sizeDelta = new Vector2(295, 60);
            Button update = updateButton.GetComponent<Button>();
            update.onClick.AddListener(() =>
            {
                Close();
                Plugin.lastVersion.value = Plugin.PLUGIN_VERSION;
                Plugin.ignoreUpdates.value = true;
            });
        }
    }
}
