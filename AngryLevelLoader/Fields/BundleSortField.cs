using AngryLevelLoader.Managers;
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
	internal class BundleSortField : CustomConfigField
	{
		public const string ASSET_PATH = "AngryLevelLoader/Fields/BundleSortField.prefab";

		// UI
		private RectTransform fieldUi;
		private AngryBundleSortFieldComponent currentUi;

		// Data
		private bool _sortFavourites;
		private bool SortFavourites
		{
			get => _sortFavourites;
			set
			{
				_sortFavourites = value;

				if (currentUi != null)
				{
					currentUi.favSortIcon.sprite = (_sortFavourites) ? AssetManager.favouriteSelected : AssetManager.favouriteUnselected;
				}
			}
		}

		private ConfigManager.BundleSorting _sortingMode;
		private ConfigManager.BundleSorting SortingMode
		{
			get => _sortingMode;
			set
			{
				_sortingMode = value;

				if (currentUi != null)
				{
					switch (_sortingMode)
					{
						case ConfigManager.BundleSorting.Alphabetically:
							currentUi.SetButtonSelected(currentUi.nameButton);
							break;

						case ConfigManager.BundleSorting.Author:
							currentUi.SetButtonSelected(currentUi.authorButton);
							break;

						case ConfigManager.BundleSorting.LastUpdate:
							currentUi.SetButtonSelected(currentUi.lastUpdateButton);
							break;

						case ConfigManager.BundleSorting.LastPlayed:
							currentUi.SetButtonSelected(currentUi.lastPlayedButton);
							break;
					}
				}
			}
		}

		private bool inited = false;
		public BundleSortField(ConfigPanel parentPanel) : base(parentPanel, 600, 40)
		{
			inited = true;

			SortFavourites = ConfigManager.bundleFavSort.value;
			SortingMode = ConfigManager.bundleSortingMode.value;

			ConfigManager.InitializeConfig();
			ConfigManager.bundleFavSort.postValueChangeEvent += (fav) =>
			{
				SortFavourites = fav;
			};
			ConfigManager.bundleSortingMode.postValueChangeEvent += (sortMode) =>
			{
				SortingMode = sortMode;
			};

			if (fieldUi != null)
				OnCreateUI(fieldUi);
		}

		public override void OnCreateUI(RectTransform fieldUI)
		{
			this.fieldUi = fieldUI;
			if (!inited)
				return;

			currentUi = Addressables.InstantiateAsync(ASSET_PATH, fieldUI.transform).WaitForCompletion().GetComponent<AngryBundleSortFieldComponent>();
			RectTransform currentUiRect = currentUi.GetComponent<RectTransform>();
			currentUiRect.anchoredPosition = new Vector2(0, 0);

			SortFavourites = ConfigManager.bundleFavSort.value;
			SortingMode = ConfigManager.bundleSortingMode.value;

			currentUi.favSortButton.onClick.AddListener(() =>
			{
				ConfigManager.bundleFavSort.value = !SortFavourites;
				ConfigManager.bundleFavSort.TriggerPostValueChangeEvent();
			});

			currentUi.nameButton.onClick.AddListener(() =>
			{
				ConfigManager.bundleSortingMode.value = ConfigManager.BundleSorting.Alphabetically;
				ConfigManager.bundleSortingMode.TriggerPostValueChangeEvent();
			});

			currentUi.authorButton.onClick.AddListener(() =>
			{
				ConfigManager.bundleSortingMode.value = ConfigManager.BundleSorting.Author;
				ConfigManager.bundleSortingMode.TriggerPostValueChangeEvent();
			});

			currentUi.lastUpdateButton.onClick.AddListener(() =>
			{
				ConfigManager.bundleSortingMode.value = ConfigManager.BundleSorting.LastUpdate;
				ConfigManager.bundleSortingMode.TriggerPostValueChangeEvent();
			});

			currentUi.lastPlayedButton.onClick.AddListener(() =>
			{
				ConfigManager.bundleSortingMode.value = ConfigManager.BundleSorting.LastPlayed;
				ConfigManager.bundleSortingMode.TriggerPostValueChangeEvent();
			});
		}
	}
}
