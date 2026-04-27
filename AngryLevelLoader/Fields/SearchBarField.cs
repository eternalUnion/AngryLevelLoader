using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace AngryLevelLoader.Fields
{
	internal class SearchBarField : CustomConfigField
	{
		private class SelectOnEnabled : MonoBehaviour
		{
			public bool selectOnEnable = false;

			private Selectable _selectable;

			private void Awake()
			{
				_selectable = GetComponent<Selectable>();
				if (_selectable == null)
				{
					Debug.LogError($"Object {gameObject.name} has SelectOnEnabled but is not a Selectable!");
					enabled = false;
					return;
				}

				_selectable.Select();
			}

			private void OnEnable()
			{
				if (selectOnEnable && _selectable != null)
					_selectable.Select();
			}
		}

		private const string ASSET_PATH = "AngryLevelLoader/Fields/SearchBar.prefab";

		private string _value;
		public string value
		{
			get => _value;
			set
			{
				_value = value;

				if (currentInput != null)
					currentInput.text = value;
			}
		}

		private RectTransform fieldContainer;
		private TMP_InputField currentInput;
		private SelectOnEnabled currentSelector;
		private Button resetButton;

		public Action<string> onValueChange;
		public Action onReset;
		public Action<bool> onEndEdit;

		private bool _selectOnEnable = true;
		public bool selectOnEnable
		{
			get => _selectOnEnable;
			set {
				_selectOnEnable = value;
				if (currentSelector != null)
					currentSelector.selectOnEnable = value;
			}
		}

		private bool _inited = false;
		public SearchBarField(ConfigPanel rootPanel) : base(rootPanel)
		{
			_inited = true;

			if (fieldContainer != null)
				OnCreateUI(fieldContainer);
		}

		public override void OnCreateUI(RectTransform fieldUI)
		{
			fieldContainer = fieldUI;

			if (!_inited)
				return;

			GameObject uiObject = Addressables.InstantiateAsync(ASSET_PATH, fieldUI).WaitForCompletion();
			fieldUI.sizeDelta = uiObject.GetComponent<RectTransform>().sizeDelta;
			currentInput = uiObject.GetComponent<TMP_InputField>();

			currentInput.text = _value;
			currentInput.onValueChanged.AddListener((string newVal) =>
			{
				_value = newVal;

				if (onValueChange != null)
					onValueChange.Invoke(newVal);
			});
			currentInput.onEndEdit.AddListener((string newVal) =>
			{
				if (onEndEdit != null)
					onEndEdit.Invoke(currentInput.wasCanceled);
			});

			currentSelector = currentInput.gameObject.AddComponent<SelectOnEnabled>();
			currentSelector.selectOnEnable = _selectOnEnable;
			if (_selectOnEnable)
				currentInput.Select();

			resetButton = uiObject.GetComponentInChildren<Button>();
			resetButton.onClick = new Button.ButtonClickedEvent();
			resetButton.onClick.AddListener(() =>
			{
				if (onReset != null)
					onReset.Invoke();
			});

			fieldContainer.gameObject.SetActive(!hidden && !hierarchyHidden);
			currentInput.interactable = interactable && hierarchyInteractable;
			resetButton.interactable = interactable && hierarchyInteractable;
		}

		public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
		{
			if (fieldContainer == null)
				return;

			fieldContainer.gameObject.SetActive(!hidden && !hierarchyHidden);
		}

		public override void OnInteractableChange(bool selfInteractable, bool hierarchyInteractable)
		{
			if (currentInput == null)
				return;

			currentInput.interactable = interactable && hierarchyInteractable;
			resetButton.interactable = interactable && hierarchyInteractable;
		}

		public void SetValueWithoutNotify(string newVal)
		{
			_value = newVal;

			if (currentInput != null)
				currentInput.SetTextWithoutNotify(newVal);
		}

		public bool FocusOnField()
		{
			if (currentInput != null && currentInput.gameObject.activeInHierarchy)
			{
				currentInput.Select();
				return true;
			}

			return false;
		}
	}
}
