using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryResetUserMapVarNotificationElementComponent : MonoBehaviour
	{
		public TextMeshProUGUI id;
		public Button resetButton;
		public TextMeshProUGUI resetButtonText;

		public Action onReset;

		public void SetButton()
		{
			resetButtonText.text = "Reset";

			resetButton.onClick = new Button.ButtonClickedEvent();
			resetButton.onClick.AddListener(() =>
			{
				StartCoroutine(ResetButtonCoroutine(resetButton, resetButtonText, onReset));
			});
		}

		private IEnumerator ResetButtonCoroutine(Button btn, TextMeshProUGUI txt, Action cb)
		{
			btn.interactable = false;

			for (int i = 3; i >= 1; i--)
			{
				txt.text = $"Are you sure? ({i})";
				yield return new WaitForSecondsRealtime(1f);
			}

			txt.text = "<color=red>Are you sure?</color>";

			btn.interactable = true;
			btn.onClick = new Button.ButtonClickedEvent();
			btn.onClick.AddListener(() =>
			{
				if (cb != null)
					cb.Invoke();
			});
		}
	}
}
