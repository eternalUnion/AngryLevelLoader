using AngryUiComponents;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Fields
{
	public class DifficultyField : CustomConfigField
	{
		public const string ASSET_PATH = "AngryLevelLoader/Fields/DifficultyField.prefab";

		private bool inited = false;
		private RectTransform fieldUi;
		private AngryDifficultyFieldComponent currentUi;

		private StringListField internalDifficultyField = null;
		public string difficultyListValue
		{
			get => internalDifficultyField.value;
			set
			{
				internalDifficultyField.value = value;

				if (currentUi != null)
				{
					currentUi.difficultyList.SetValueWithoutNotify(internalDifficultyField.valueIndex);
				}
			}
		}
		public int difficultyListValueIndex
		{
			get => internalDifficultyField.valueIndex;
			set
			{
				internalDifficultyField.valueIndex = value;

				if (currentUi != null)
				{
					currentUi.difficultyList.SetValueWithoutNotify(internalDifficultyField.valueIndex);
				}
			}
		}
		public void ForceSetDifficultyUI(int index)
		{
			if (currentUi != null)
				currentUi.difficultyList.SetValueWithoutNotify(index);
		}

		private bool _difficultyInteractable = true;
		public bool difficultyInteractable
		{
			get => _difficultyInteractable;
			set
			{
				_difficultyInteractable = value;

				if (currentUi != null)
					currentUi.difficultyList.interactable = value;
			}
		}
		public StringListField.PostStringListValueChangeEvent postDifficultyChange;
		public void TriggerPostDifficultyChangeEvent()
		{
			postDifficultyChange.Invoke(internalDifficultyField.value, internalDifficultyField.valueIndex);
		}

		private StringListField internalGamemodeField = null;
		public string gamemodeListValue
		{
			get => internalGamemodeField.value;
			set
			{
				internalGamemodeField.value = value;

				if (currentUi != null)
				{
					currentUi.difficultyList.SetValueWithoutNotify(internalGamemodeField.valueIndex);
				}
			}
		}
		public int gamemodeListValueIndex
		{
			get => internalGamemodeField.valueIndex;
			set
			{
				internalGamemodeField.valueIndex = value;

				if (currentUi != null)
				{
					currentUi.difficultyList.SetValueWithoutNotify(internalGamemodeField.valueIndex);
				}
			}
		}

		private bool _gamemodeInteractable = true;
		public bool gamemodeInteractable
		{
			get => _gamemodeInteractable;
			set
			{
				_gamemodeInteractable = value;

				if (currentUi != null)
					currentUi.gamemodeList.interactable = value;
			}
		}
		public StringListField.PostStringListValueChangeEvent postGamemodeChange;
		public void TriggerPostGamemodeChangeEvent()
		{
			postGamemodeChange.Invoke(internalGamemodeField.value, internalGamemodeField.valueIndex);
		}

		public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
		{
			if (currentUi != null)
				currentUi.gameObject.SetActive(!hierarchyHidden);
		}

		public override void OnInteractableChange(bool selfInteractable, bool hierarchyInteractable)
		{
			if (currentUi != null)
				currentUi.group.interactable = hierarchyInteractable;
		}
		
		public DifficultyField(ConfigPanel rootPanel) : base(rootPanel)
		{
			inited = true;

			internalDifficultyField = new StringListField(Plugin.internalConfig.rootPanel, "Difficulty", "difficultySelect", Plugin.difficultyList.ToArray(), "VIOLENT");
			internalGamemodeField = new StringListField(Plugin.internalConfig.rootPanel, "Gamemode", "gamemode", Plugin.gamemodeList, "None");

			if (fieldUi != null)
				OnCreateUI(fieldUi);
		}

		public override void OnCreateUI(RectTransform fieldUI)
		{
			this.fieldUi = fieldUI;
			if (!inited)
				return;

			Transform container = fieldUi.transform.parent;
			UnityEngine.Object.DestroyImmediate(fieldUi.gameObject);
			currentUi = Addressables.InstantiateAsync(ASSET_PATH, container).WaitForCompletion().GetComponent<AngryDifficultyFieldComponent>();

			currentUi.difficultyList.AddOptions(Plugin.difficultyList);
			currentUi.gamemodeList.AddOptions(Plugin.gamemodeList);

			currentUi.difficultyList.value = internalDifficultyField.valueIndex;
			currentUi.gamemodeList.value = internalGamemodeField.valueIndex;

			currentUi.difficultyList.onValueChanged.AddListener((newIndex) =>
			{
				internalDifficultyField.valueIndex = newIndex;

				if (postDifficultyChange != null)
					postDifficultyChange.Invoke(internalDifficultyField.value, internalDifficultyField.valueIndex);
			});

			currentUi.gamemodeList.onValueChanged.AddListener((newIndex) =>
			{
				internalGamemodeField.valueIndex = newIndex;

				if (postGamemodeChange != null)
					postGamemodeChange.Invoke(internalGamemodeField.value, internalGamemodeField.valueIndex);
			});

			currentUi.difficultyList.interactable = _difficultyInteractable;
			currentUi.gamemodeList.interactable = _gamemodeInteractable;
			currentUi.group.interactable = base.hierarchyInteractable;

			currentUi.gameObject.SetActive(!base.hierarchyHidden);
		}
	}
}
