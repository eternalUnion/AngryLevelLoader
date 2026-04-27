using AngryLevelLoader.Containers;
using AngryUiComponents;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

        BundleContainer container;
        public DeleteBundleNotification(BundleContainer container)
        {
            this.container = container;
        }

        private async Task DeleteBundleTask()
        {
            try
            {
                await container.DeleteBundle();
            }
            finally
            {
                if (ui != null)
                    ui.cancelButton.interactable = true;

                Close();
            }
        }

        private AngryDeleteBundleNotificationComponent ui = null;

        public override void OnUI(RectTransform panel)
        {
            ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryDeleteBundleNotificationComponent>();

            ui.cancelButton.onClick.AddListener(() =>
            {
                Close();
            });

            ui.deleteButton.onClick.AddListener(() =>
            {
                ui.body.text = "Deleting bundle...";
                ui.cancelButton.interactable = false;
                ui.deleteButton.interactable = false;
                _ = DeleteBundleTask();
            });

            ui.bundleIcon.sprite = container.Icon;
            ui.bundleName.text = container.BundleName;

            ui.body.text = $"Do you want to delete <color=aqua>{container.BundleName}</color>?\n\nFile will be deleted permanently!\n\n(Level ranks will not be affected)";
            ui.gameObject.AddComponent<DeleteButtonComponent>().ui = ui;
        }
    }
}
