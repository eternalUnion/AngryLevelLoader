using AngryLevelLoader.Managers;
using AngryLevelLoader.UserInterface;
using AngryUiComponents;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Fields
{
	internal class OnlineStatusFilterField : CustomConfigField
	{
		public const string ASSET_PATH = "AngryLevelLoader/Fields/OnlineStatusFilter.prefab";

		// UI
		private RectTransform fieldUi;
		private AngryOnlineStatusFilterComponent currentUi;

		// Data
		private bool _showInstalled;
		private bool ShowInstalled
		{
			get => _showInstalled;
			set
			{
				_showInstalled = value;
				if (currentUi != null)
					currentUi.installed.Set(_showInstalled, false);
			}
		}

		private bool _showNotInstalled;
		private bool ShowNotInstalled
		{
			get => _showNotInstalled;
			set
			{
				_showNotInstalled = value;
				if (currentUi != null)
					currentUi.notInstalled.Set(_showNotInstalled, false);
			}
		}

		private bool _showUpdateAvailable;
		private bool ShowUpdateAvailable
		{
			get => _showUpdateAvailable;
			set
			{
				_showUpdateAvailable = value;
				if (currentUi != null)
					currentUi.updateAvailable.Set(_showUpdateAvailable, false);
			}
		}
		
		private bool inited = false;
		public OnlineStatusFilterField(ConfigPanel parentPanel) : base(parentPanel, 600, 40)
		{
			inited = true;

			OnlineLevelsList.Init();
			ShowInstalled = OnlineLevelsList.showInstalledLevels.value;
			ShowNotInstalled = OnlineLevelsList.showNotInstalledLevels.value;
			ShowUpdateAvailable = OnlineLevelsList.showUpdateAvailableLevels.value;

			OnlineLevelsList.showInstalledLevels.postValueChangeEvent += (val) =>
			{
				ShowInstalled = val;
			};
			OnlineLevelsList.showNotInstalledLevels.postValueChangeEvent += (val) =>
			{
				ShowNotInstalled = val;
			};
			OnlineLevelsList.showUpdateAvailableLevels.postValueChangeEvent += (val) =>
			{
				ShowUpdateAvailable = val;
			};

			if (fieldUi != null)
				OnCreateUI(fieldUi);
		}

		public override void OnCreateUI(RectTransform fieldUI)
		{
			this.fieldUi = fieldUI;
			if (!inited)
				return;

			currentUi = Addressables.InstantiateAsync(ASSET_PATH, fieldUI.transform).WaitForCompletion().GetComponent<AngryOnlineStatusFilterComponent>();
			RectTransform currentUiRect = currentUi.GetComponent<RectTransform>();
			currentUiRect.anchoredPosition = new Vector2(0, 0);

			ShowInstalled = OnlineLevelsList.showInstalledLevels.value;
			ShowNotInstalled = OnlineLevelsList.showNotInstalledLevels.value;
			ShowUpdateAvailable = OnlineLevelsList.showUpdateAvailableLevels.value;

			currentUi.installed.onValueChanged.AddListener((val) =>
			{
				OnlineLevelsList.showInstalledLevels.value = val;
				OnlineLevelsList.showInstalledLevels.TriggerPostValueChangeEvent();
			});

			currentUi.notInstalled.onValueChanged.AddListener((val) =>
			{
				OnlineLevelsList.showNotInstalledLevels.value = val;
				OnlineLevelsList.showNotInstalledLevels.TriggerPostValueChangeEvent();
			});

			currentUi.updateAvailable.onValueChanged.AddListener((val) =>
			{
				OnlineLevelsList.showUpdateAvailableLevels.value = val;
				OnlineLevelsList.showUpdateAvailableLevels.TriggerPostValueChangeEvent();
			});
		}
	}
}
