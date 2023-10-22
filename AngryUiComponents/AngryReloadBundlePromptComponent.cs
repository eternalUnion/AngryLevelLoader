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

		private RectTransform rect;
		public void Awake()
		{
			rect = GetComponent<RectTransform>();
			rect.anchoredPosition = Vector3.zero;

			ignoreButton.onClick = new Button.ButtonClickedEvent();
			ignoreButton.onClick.AddListener(() =>
			{
				reloadButton.onClick = new Button.ButtonClickedEvent();
				GoUp(false);
			});
		}

		public void GoDown(bool resetPosition)
		{
			if (resetPosition)
				rect.anchoredPosition = Vector3.zero;
			division.interactable = true;
			goDown = true;
		}

		public void GoUp(bool resetPosition)
		{
			if (resetPosition)
				rect.anchoredPosition = Vector3.zero;
			division.interactable = false;
			goDown = false;
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

		private bool goDown = false;
		private bool transparent = false;
		private const float TARGET_ALPHA = 0.6f;
		private const float APPEARANCE_TIME = 1f / 0.7f;
		public void Update()
		{
			float targetY = goDown ? -(rect.sizeDelta.y + 10) : 0f;
			if (rect.anchoredPosition.y != targetY)
			{
				float distance = rect.sizeDelta.y + 10;
				float currentY = rect.anchoredPosition.y;

				currentY = Mathf.MoveTowards(currentY, targetY, distance * Time.unscaledDeltaTime * APPEARANCE_TIME);
				rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, currentY);
			}

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
