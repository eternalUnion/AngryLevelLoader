using AngryLevelLoader.Containers;
using AngryUiComponents;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Notifications
{
    internal class LoadingBarSpin : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.forward, Time.unscaledDeltaTime * 360f);
        }
    }

    class DeleteOldBundlesNotification : NotificationPanel.Notification
    {
        private const string ASSET_PATH = "AngryLevelLoader/Notifications/DeleteOldBundlesNotification.prefab";

        private class DeleteButtonComponent : MonoBehaviour
        {
            public AngryDeleteOldBundlesNotificationComponent ui;
            private float timeRemaining = 3.99f;

            public void Update()
            {
                timeRemaining = Mathf.MoveTowards(timeRemaining, 0f, Time.unscaledDeltaTime);

                if (timeRemaining == 0f)
                {
                    ui.deleteButtonText.text = "Delete";
                    ui.deleteButton.interactable = true;
                    enabled = false;
                }
                else
                {
                    ui.deleteButtonText.text = $"Delete ({(int)timeRemaining})";
                }
            }
        }

        private async Task DeletionTask()
        {
            try
            {
                List<AngryBundleContainer> bundlesToDelete = Plugin.angryBundles.Values
                    .Where(bundle => bundle.bundleData != null && bundle.bundleData.bundleVersion < 6)
                    .ToList();

                int progress = 1;
                foreach (AngryBundleContainer container in bundlesToDelete)
                {
                    string bundleName = container.bundleData.bundleName;
                    ui.deletionProgress.text = $"Deleting bundles... ({progress}/{bundlesToDelete.Count})";

                    try
                    {
                        await container.DeleteBundle();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Could not delete bundle");
                        Debug.LogException(e);
                    }

                    ui.deletionLog.text = $"- Deleted {bundleName}\n" + ui.deletionLog.text;
                    progress += 1;
                }

                ui.loadingCircle.SetActive(false);
                ui.deletionProgress.text = "<color=lime>Done! You can close the window</color>";
            }
            finally
            {
                ui.cancelButton.interactable = true;
            }

            Plugin.ScanForLevels();
        }

        AngryDeleteOldBundlesNotificationComponent ui = null;

        public override void OnUI(RectTransform panel)
        {
            ui = Addressables.InstantiateAsync(ASSET_PATH, panel).WaitForCompletion().GetComponent<AngryDeleteOldBundlesNotificationComponent>();

            ui.cancelButton.onClick.AddListener(() =>
            {
                Close();
            });

            int numberOfBundlesToDelete = Plugin.angryBundles
                .Select(bundle => bundle.Value.bundleData)
                .Where(data => data != null && data.bundleVersion < 6)
                .Count();

            if (numberOfBundlesToDelete == 0)
            {
                Close();
                return;
            }

            ui.body.text = $"Do you want to delete <color=orange>{numberOfBundlesToDelete}</color> old bundles?\n\nThese bundles cannot be played on the latest version of ULTRAKILL.\n\nThey can be played on an older version by downpatching ULTRAKILL to the Violence update and downgrading Angry to version 2.10.1\n\nThe deletion cannot be reverted!";

            ui.deleteButton.interactable = false;
            ui.deleteButton.onClick.AddListener(() =>
            {
                ui.body.gameObject.SetActive(false);
                ui.loadingCircle.SetActive(true);
                ui.deletionProgress.gameObject.SetActive(true);
                ui.cancelButtonText.text = "Close";

                _ = DeletionTask();
            });

            ui.loadingCircle.AddComponent<LoadingBarSpin>();

            ui.gameObject.AddComponent<DeleteButtonComponent>().ui = ui;
        }
    }
}
