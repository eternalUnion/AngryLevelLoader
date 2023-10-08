using AngryLevelLoader.Containers;
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
    public class DeleteBundleNotification : NotificationPanel.Notification
    {
        private const string ASSET_PATH = "AngryLevelLoader/Notifications/BundleDeleteNotification.prefab";

        private class DeleteButtonComponent : MonoBehaviour
        {
            public AngryDeleteBundleNotificationComponent ui;
            private float timeRemaining = 3.99f;

            public void Update()
            {
                timeRemaining = Mathf.MoveTowards(timeRemaining, 0f, Time.unscaledDeltaTime);

                if (timeRemaining == 0f)
                {
                    ui.deleteText.text = "Delete";
                    ui.deleteButton.interactable = true;
                    enabled = false;
                }
                else
                {
                    ui.deleteText.text = $"Delete ({(int)timeRemaining})";
                }
            }
        }

        AngryBundleContainer container;
        public DeleteBundleNotification(AngryBundleContainer container)
        {
            this.container = container;
        }

        public override void OnUI(RectTransform panel)
        {
            AngryDeleteBundleNotificationComponent ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryDeleteBundleNotificationComponent>();

            ui.cancelButton.onClick.AddListener(() =>
            {
                Close();
            });

            ui.deleteButton.onClick.AddListener(() =>
            {
                Close();
                container.DeleteBundle();
            });

            ui.bundleIcon.sprite = container.rootPanel.icon;
            ui.bundleName.text = container.rootPanel.displayName;

            ui.body.text = $"Do you want to delete <color=aqua>{container.bundleData.bundleName}</color>?\n\nFile will be deleted permanently!\n\n(Level ranks will not be affected)";
            ui.gameObject.AddComponent<DeleteButtonComponent>().ui = ui;
        }
    }
}
