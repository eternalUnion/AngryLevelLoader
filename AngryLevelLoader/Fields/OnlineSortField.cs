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
	internal class OnlineSortField : CustomConfigField
	{
		public const string ASSET_PATH = "AngryLevelLoader/Fields/OnlineSortField.prefab";

		// UI
		private RectTransform fieldUi;
		private AngryOnlineSortFieldComponent currentUi;

		// Data
		private OnlineLevelsList.SortFilter _sortingMode;
		private OnlineLevelsList.SortFilter SortingMode
		{
			get => _sortingMode;
			set
			{
				_sortingMode = value;

				if (currentUi != null)
				{
					switch (_sortingMode)
					{
						case OnlineLevelsList.SortFilter.Name:
							currentUi.SetButtonSelected(currentUi.nameButton);
							break;

						case OnlineLevelsList.SortFilter.Author:
							currentUi.SetButtonSelected(currentUi.authorButton);
							break;

						case OnlineLevelsList.SortFilter.LastUpdate:
							currentUi.SetButtonSelected(currentUi.lastUpdateButton);
							break;

						case OnlineLevelsList.SortFilter.Votes:
							currentUi.SetButtonSelected(currentUi.votesButton);
							break;
					}
				}
			}
		}

		private bool inited = false;
		public OnlineSortField(ConfigPanel parentPanel) : base(parentPanel, 600, 40)
		{
			inited = true;

			OnlineLevelsList.Init();
			SortingMode = OnlineLevelsList.sortFilter.value;

			OnlineLevelsList.sortFilter.postValueChangeEvent += (mode) =>
			{
				SortingMode = mode;
			};

			if (fieldUi != null)
				OnCreateUI(fieldUi);
		}

		public override void OnCreateUI(RectTransform fieldUI)
		{
			this.fieldUi = fieldUI;
			if (!inited)
				return;

			currentUi = Addressables.InstantiateAsync(ASSET_PATH, fieldUI.transform).WaitForCompletion().GetComponent<AngryOnlineSortFieldComponent>();
			RectTransform currentUiRect = currentUi.GetComponent<RectTransform>();
			currentUiRect.anchoredPosition = new Vector2(0, 0);

			SortingMode = OnlineLevelsList.sortFilter.value;

			currentUi.refreshButton.onClick.AddListener(() =>
			{
				OnlineLevelsList.RefreshAsync();
			});

			currentUi.nameButton.onClick.AddListener(() =>
			{
				OnlineLevelsList.sortFilter.value = OnlineLevelsList.SortFilter.Name;
				OnlineLevelsList.sortFilter.TriggerPostValueChangeEvent();
			});

			currentUi.authorButton.onClick.AddListener(() =>
			{
				OnlineLevelsList.sortFilter.value = OnlineLevelsList.SortFilter.Author;
				OnlineLevelsList.sortFilter.TriggerPostValueChangeEvent();
			});

			currentUi.lastUpdateButton.onClick.AddListener(() =>
			{
				OnlineLevelsList.sortFilter.value = OnlineLevelsList.SortFilter.LastUpdate;
				OnlineLevelsList.sortFilter.TriggerPostValueChangeEvent();
			});

			currentUi.votesButton.onClick.AddListener(() =>
			{
				OnlineLevelsList.sortFilter.value = OnlineLevelsList.SortFilter.Votes;
				OnlineLevelsList.sortFilter.TriggerPostValueChangeEvent();
			});
		}
	}
}
