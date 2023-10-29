using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using UnityEngine;
using AngryUiComponents;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Notifications
{

#pragma warning disable IDE1006
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
#pragma warning restore IDE1006

    public class PluginUpdateNotification : NotificationPanel.Notification
    {
        private const string ASSET_PATH = "AngryLevelLoader/Notifications/PluginUpdateNotification.prefab";

        private PluginInfoJson json;

        public PluginUpdateNotification(PluginInfoJson json)
        {
            this.json = json;
        }

        public override void OnUI(RectTransform panel)
        {
            if (json == null)
            {
                Plugin.logger.LogError("Closed plugin changelog panel because passed json is null");
                Close();
                return;
            }

            AngryPluginChangelogNotificationComponent ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryPluginChangelogNotificationComponent>();

            ui.cancel.onClick.AddListener(() =>
            {
                Close();
                Plugin.lastVersion.value = Plugin.PLUGIN_VERSION;
            });

            ui.ignoreUpdate.onClick.AddListener(() =>
            {
                Close();
                Plugin.lastVersion.value = Plugin.PLUGIN_VERSION;
                Plugin.ignoreUpdates.value = true;
            });

            ui.header.text = "<color=cyan>Changelog</color>";
            if (new Version(Plugin.PLUGIN_VERSION) < new Version(json.latestVersion))
                ui.header.text = "<color=lime>UPDATE AVAILABLE</color>";

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

                updateTextBuilder.Append("<size=18>\n\n");
                updateTextBuilder.Append(json.updates[currentVersion].updateText.Replace(@"\n", "\n"));
                updateTextBuilder.Append("</size>");

                firstTime = false;
            }

            ui.text.text = updateTextBuilder.ToString();
        }
    }
}
