using AngryLevelLoader.Managers;
using AngryUiComponents;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Notifications
{
	public class ResetUserMapVarNotification : NotificationPanel.Notification
	{
		public const string ASSET_PATH = "AngryLevelLoader/Notifications/ResetUserMapVarNotification.prefab";

		public override void OnUI(RectTransform panel)
		{
			GameObject panelObject = Addressables.InstantiateAsync(ASSET_PATH, panel.transform).WaitForCompletion();
			AngryResetUserMapVarNotificationComponent panelComp = panelObject.GetComponent<AngryResetUserMapVarNotificationComponent>();

			panelComp.exitButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
			panelComp.exitButton.onClick.AddListener(this.Close);

			string searchPath = AngryMapVarManager.GetCurrentUserMapVarsDirectory();
			if (!Directory.Exists(searchPath))
			{
				panelComp.notFoundText.SetActive(true);
				return;
			}

			string[] allFiles = Directory.GetFiles(searchPath);
			if (allFiles.Length == 0)
			{
				panelComp.notFoundText.SetActive(true);
				return;
			}

			foreach (string file in allFiles)
			{
				string id = Path.GetFileName(file);
				id = id.EndsWith(AngryMapVarManager.MAPVAR_FILE_EXTENSION) ? id.Substring(0, id.Length - AngryMapVarManager.MAPVAR_FILE_EXTENSION.Length) : id;

				AngryResetUserMapVarNotificationElementComponent element = panelComp.CreateTemplate();
				element.SetButton();
				element.id.text = id;
				element.onReset = () =>
				{
					if (File.Exists(file))
					{
						File.Delete(file);
						GameObject.Destroy(element.gameObject);
					}
					else
					{
						element.SetButton();
						element.resetButtonText.text = "<color=red>Failed</color>";
					}
				};
			}
		}
	}
}
