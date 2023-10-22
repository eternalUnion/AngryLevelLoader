using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryReloadBundlePromptComponent : MonoBehaviour
	{
		public CanvasGroup division;
		public AudioSource audio;
		public Text text;
		public Button ignoreButton;
		public Button reloadButton;

		public void Awake()
		{
			ignoreButton.onClick = new Button.ButtonClickedEvent();
			ignoreButton.onClick.AddListener(() =>
			{
				reloadButton.onClick = new Button.ButtonClickedEvent();
				gameObject.SetActive(false);
			});
		}

		public void MakeTransparent(bool instant)
		{
			transparent = true;
			if (instant)
				division.alpha = TARGET_ALPHA;
		}

		public void MakeOpaque(bool instant)
		{
			transparent = false;
			if (instant)
				division.alpha = 1f;
		}

		private bool transparent = false;
		private const float TARGET_ALPHA = 0.6f;
		public void Update()
		{
			if (transparent && division.alpha != TARGET_ALPHA)
			{
				division.alpha = Mathf.MoveTowards(division.alpha, TARGET_ALPHA, Time.unscaledDeltaTime * 2);
			}
			else if (!transparent && division.alpha != 1f)
			{
				division.alpha = Mathf.MoveTowards(division.alpha, 1f, Time.unscaledDeltaTime * 2);
			}
		}
	}
}
