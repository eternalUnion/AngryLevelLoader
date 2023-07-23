using PluginConfig.API;
using PluginConfig.API.Fields;
using RudeLevelScript;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public class LevelField : CustomConfigField
	{
		private bool inited = false;
		internal static Sprite bgSprite;
		public RudeLevelData data;

		public float time = 0;
		public char timeRank = '-';
		public int kills = 0;
		public char killsRank = '-';
		public int style = 0;
		public char styleRank = '-';
		public int secrets = 0;
		public char finalRank = '-';
		public bool challenge = false;
		public bool discovered = true;

		private RectTransform currentUI;

		private Text timeText;
		private Text killsText;
		private Text styleText;
		private Text secretsText;
		private Text finalRankText;
		private Image uiImage;
		private Image statsImage;
		private Image finalRankImage;
		private Image challengePanel;
		private List<Text> headerTexts = new List<Text>();

		public static Color perfectUiColor = new Color(171 / 255f, 108 / 255f, 2 / 255f);
		public static Color perfectStatsColor = new Color(225 / 255f, 154 / 255f, 0);
		public static Color perfectRankColor = new Color(241 / 255f, 168 / 255f, 8 / 255f);
		public static Color colorSilver = new Color(0xc0 / 255f, 0xc0 / 255f, 0xc0 / 255f);

		private Text levelNameText;
		private Image levelPreviewImage;
		public Button levelPreviewButton;
		private Text challengeText;

		public bool playedBefore
		{
			get
			{
				return finalRank != '-';
			}
		}
		public bool locked
		{
			get
			{
				bool locked = false;

				foreach (string reqId in data.requiredCompletedLevelIdsForUnlock)
				{
					LevelContainer reqLevel = Plugin.GetLevel(reqId);
					if (reqLevel == null)
					{
						Debug.LogWarning($"Could not find level unlock requirement id for {data.uniqueIdentifier}, requested id was {reqId}");
						continue;
					}

					if (reqLevel.finalRank.value[0] == '-')
					{
						locked = true;
						break;
					}
				}

				return locked;
			}
		}

		private bool _forceHidden = false;
		public bool forceHidden
		{
			get => _forceHidden;
			set
			{
				_forceHidden = value;
				hidden = hidden;
			}
		}
		public override bool hidden
		{
			get => base.hidden;
			set
			{
				base.hidden = value;
				if (currentUI != null)
					currentUI.gameObject.SetActive(!hierarchyHidden && !forceHidden);
			}
		}

		public delegate void onLevelButtonPressDelegate();
		public event onLevelButtonPressDelegate onLevelButtonPress;
		
		public LevelField(ConfigPanel panel, RudeLevelData data) : base(panel, 600, 170)
		{
			this.data = data;

			inited = true;
			if (currentUI != null)
				OnCreateUI(currentUI);
		}

		public void UpdateUI()
		{
			if (currentUI == null)
				return;

			if (data.isSecretLevel)
			{
				statsImage.gameObject.SetActive(false);
				challengePanel.gameObject.SetActive(false);
			}
			else
			{
				statsImage.gameObject.SetActive(true);
				challengePanel.gameObject.SetActive(true);

				timeText.text = $"{GetTimeStringFromSeconds(time)} {RankUtils.GetFormattedRankText(timeRank)}";
				killsText.text = $"{kills} {RankUtils.GetFormattedRankText(killsRank)}";
				styleText.text = $"{style} {RankUtils.GetFormattedRankText(styleRank)}";
				secretsText.text = $"{secrets} / {data.secretCount}";
				if (secrets == data.secretCount)
					secretsText.text = $"<color=aqua>{secretsText.text}</color>";
				finalRankText.text = RankUtils.GetFormattedRankText(finalRank);
				challengePanel.color = challenge ? new Color(0xff / 255f, 0xa5 / 255f, 0, 0.8f) : new Color(0, 0, 0, 0.8f);
				challengePanel.gameObject.SetActive(data.levelChallengeEnabled);
				challengeText.text = data.levelChallengeEnabled ? data.levelChallengeText : "No challenge available for the level";
			}

			if (finalRank == 'P')
			{
				uiImage.color = perfectUiColor;
				statsImage.color = perfectStatsColor;
				finalRankImage.color = perfectRankColor;
				headerTexts.ForEach(header => header.color = Color.white);
			}
			else
			{
				uiImage.color = Color.black;
				statsImage.color = new Color(0, 0, 0, 0.8f);
				finalRankImage.color = new Color(0, 0, 0, 0.8f);
				headerTexts.ForEach(header => header.color = colorSilver);
			}

			if (locked)
			{
				levelPreviewImage.sprite = Plugin.lockedPreview;
				levelNameText.text = "???";

				statsImage.gameObject.SetActive(false);
				challengePanel.gameObject.SetActive(false);
			}
			else
			{
				levelNameText.text = data.levelName;
				if (!playedBefore)
					levelPreviewImage.sprite = Plugin.notPlayedPreview;
				else
					levelPreviewImage.sprite = data.levelPreviewImage;
			}

			hidden = !discovered && data.hideIfNotPlayed;
		}

		internal static Text MakeText(Transform parent)
		{
			GameObject obj = new GameObject();
			RectTransform rect = obj.AddComponent<RectTransform>();
			rect.SetParent(parent);

			obj.transform.localScale = Vector3.one;

			return obj.AddComponent<Text>();
		}

		internal static RectTransform MakeRect(Transform parent)
		{
			GameObject obj = new GameObject();
			RectTransform rect = obj.AddComponent<RectTransform>();
			rect.SetParent(parent);

			return rect;
		}

		protected override void OnCreateUI(RectTransform fieldUI)
		{
			currentUI = fieldUI;
			if (!inited)
				return;

			Image bgImage = fieldUI.gameObject.AddComponent<Image>();
			uiImage = bgImage;
			if (bgSprite == null)
			{
				bgSprite = Resources.FindObjectsOfTypeAll<Image>().Where(i => i.sprite != null && i.sprite.name == "Background").First().sprite;
			}
			bgImage.sprite = bgSprite;
			bgImage.type = Image.Type.Sliced;
			bgImage.fillMethod = Image.FillMethod.Radial360;
			bgImage.color = Color.black;

			// Level name (header)
			Text levelText = MakeText(fieldUI);
			levelText.font = Plugin.gameFont;
			levelText.text = data == null ? "???" : data.levelName;
			levelText.fontSize = 17;
			RectTransform levelTextRect = levelText.GetComponent<RectTransform>();
			levelTextRect.pivot = new Vector2(0, 1);
			levelTextRect.anchorMin = new Vector2(0, 1);
			levelTextRect.anchorMax = new Vector2(1, 1);
			levelTextRect.anchoredPosition = new Vector2(10, -10);
			levelTextRect.sizeDelta = new Vector2(-10, 18);
			levelTextRect.localScale = Vector3.one;
			this.levelNameText = levelText;

			// White line break
			RectTransform lineBreakRect = MakeRect(fieldUI);
			lineBreakRect.anchorMin = new Vector2(0, 1);
			lineBreakRect.anchorMax = new Vector2(1, 1);
			lineBreakRect.pivot = new Vector2(0, 1);
			lineBreakRect.sizeDelta = new Vector2(-20, 2.5f);
			lineBreakRect.anchoredPosition = new Vector2(10, -30);
			lineBreakRect.gameObject.AddComponent<Image>();
			lineBreakRect.localScale = Vector3.one;

			// Level preview image
			RectTransform imgRect = MakeRect(fieldUI);
			imgRect.anchorMin = new Vector2(0, 1);
			imgRect.anchorMax = new Vector2(0, 1);
			imgRect.pivot = new Vector2(0, 1);
			imgRect.sizeDelta = new Vector2(160, 120);
			imgRect.anchoredPosition = new Vector2(10, -40);
			Image img = imgRect.gameObject.AddComponent<Image>();
			if (data.levelPreviewImage != null)
				img.sprite = data.levelPreviewImage;
			imgRect.localScale = Vector3.one;
			Button levelButton = imgRect.gameObject.AddComponent<Button>();
			levelButton.colors = new ColorBlock()
			{
				disabledColor = Color.white,
				fadeDuration = 0,
				colorMultiplier = 1,
				highlightedColor = Color.white,
				normalColor = Color.white,
				pressedColor = Color.white,
				selectedColor = Color.white
			};
			levelButton.onClick.AddListener(() =>
			{
				if (locked)
					return;

				if (onLevelButtonPress != null)
					onLevelButtonPress.Invoke();
			});
			this.levelPreviewButton = levelButton;
			this.levelPreviewImage = img;

			// Stats container
			RectTransform statsRect = MakeRect(fieldUI);
			statsRect.anchorMin = new Vector2(0, 1);
			statsRect.anchorMax = new Vector2(0, 1);
			statsRect.pivot = new Vector2(0, 1);
			statsRect.sizeDelta = new Vector2(280, 120);
			statsRect.anchoredPosition = new Vector2(180, -40);
			Image statsImg = statsRect.gameObject.AddComponent<Image>();
			statsImage = statsImg;
			statsImg.sprite = bgSprite;
			statsImg.color = new Color(0, 0, 0, 0.8f);
			statsImg.type = Image.Type.Sliced;
			statsRect.localScale = Vector3.one;

			// Time header text
			Text timeHeaderText = MakeText(statsRect);
			timeHeaderText.font = Plugin.gameFont;
			timeHeaderText.text = "Time: ";
			timeHeaderText.fontSize = 15;
			RectTransform timeHeaderTextRect = timeHeaderText.GetComponent<RectTransform>();
			timeHeaderTextRect.pivot = new Vector2(0, 1);
			timeHeaderTextRect.anchorMin = new Vector2(0, 1);
			timeHeaderTextRect.anchorMax = new Vector2(0, 1);
			timeHeaderTextRect.anchoredPosition = new Vector2(10, -10);
			timeHeaderTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			timeHeaderTextRect.localScale = Vector3.one;

			// Time text
			Text timeText = MakeText(statsRect);
			timeText.font = Plugin.gameFont;
			timeText.text = $"{GetTimeStringFromSeconds(time)} {RankUtils.GetFormattedRankText(timeRank)}";
			timeText.alignment = TextAnchor.UpperRight;
			timeText.fontSize = 15;
			RectTransform timeTextRect = timeText.GetComponent<RectTransform>();
			timeTextRect.pivot = new Vector2(1, 1);
			timeTextRect.anchorMin = new Vector2(1, 1);
			timeTextRect.anchorMax = new Vector2(1, 1);
			timeTextRect.anchoredPosition = new Vector2(-120, -10);
			timeTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			timeTextRect.localScale = Vector3.one;
			this.timeText = timeText;

			// Kills header text
			Text killsHeaderText = MakeText(statsRect);
			killsHeaderText.font = Plugin.gameFont;
			killsHeaderText.text = "Kills: ";
			killsHeaderText.fontSize = 15;
			RectTransform killsHeaderTextRect = killsHeaderText.GetComponent<RectTransform>();
			killsHeaderTextRect.pivot = new Vector2(0, 1);
			killsHeaderTextRect.anchorMin = new Vector2(0, 1);
			killsHeaderTextRect.anchorMax = new Vector2(0, 1);
			killsHeaderTextRect.anchoredPosition = new Vector2(10, -40);
			killsHeaderTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			killsHeaderTextRect.localScale = Vector3.one;

			// Kills text
			Text killsText = MakeText(statsRect);
			killsText.font = Plugin.gameFont;
			killsText.text = $"{kills} {RankUtils.GetFormattedRankText(killsRank)}";
			killsText.alignment = TextAnchor.UpperRight;
			killsText.fontSize = 15;
			RectTransform killsTextRect = killsText.GetComponent<RectTransform>();
			killsTextRect.pivot = new Vector2(1, 1);
			killsTextRect.anchorMin = new Vector2(1, 1);
			killsTextRect.anchorMax = new Vector2(1, 1);
			killsTextRect.anchoredPosition = new Vector2(-120, -40);
			killsTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			killsTextRect.localScale = Vector3.one;
			this.killsText = killsText;

			// Style header text
			Text styleHeaderText = MakeText(statsRect);
			styleHeaderText.font = Plugin.gameFont;
			styleHeaderText.text = "Style: ";
			styleHeaderText.fontSize = 15;
			RectTransform styleHeaderTextRect = styleHeaderText.GetComponent<RectTransform>();
			styleHeaderTextRect.pivot = new Vector2(0, 1);
			styleHeaderTextRect.anchorMin = new Vector2(0, 1);
			styleHeaderTextRect.anchorMax = new Vector2(0, 1);
			styleHeaderTextRect.anchoredPosition = new Vector2(10, -70);
			styleHeaderTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			styleHeaderTextRect.localScale = Vector3.one;

			// Style text
			Text styleText = MakeText(statsRect);
			styleText.font = Plugin.gameFont;
			styleText.text = $"{style} {RankUtils.GetFormattedRankText(styleRank)}";
			styleText.alignment = TextAnchor.UpperRight;
			styleText.fontSize = 15;
			RectTransform styleTextRect = styleText.GetComponent<RectTransform>();
			styleTextRect.pivot = new Vector2(1, 1);
			styleTextRect.anchorMin = new Vector2(1, 1);
			styleTextRect.anchorMax = new Vector2(1, 1);
			styleTextRect.anchoredPosition = new Vector2(-120, -70);
			styleTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			styleTextRect.localScale = Vector3.one;
			this.styleText = styleText;

			// Secrets header text
			Text secretsHeaderText = MakeText(statsRect);
			secretsHeaderText.font = Plugin.gameFont;
			secretsHeaderText.text = "Secrets: ";
			secretsHeaderText.fontSize = 15;
			RectTransform secretsHeaderTextRect = secretsHeaderText.GetComponent<RectTransform>();
			secretsHeaderTextRect.pivot = new Vector2(0, 1);
			secretsHeaderTextRect.anchorMin = new Vector2(0, 1);
			secretsHeaderTextRect.anchorMax = new Vector2(0, 1);
			secretsHeaderTextRect.anchoredPosition = new Vector2(10, -100);
			secretsHeaderTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			secretsHeaderTextRect.localScale = Vector3.one;

			// Secrets text
			Text secretsText = MakeText(statsRect);
			secretsText.font = Plugin.gameFont;
			secretsText.text = $"{secrets} / {data.secretCount}";
			if (secrets == data.secretCount)
				secretsText.text = $"<color=aqua>{secretsText.text}</color>";
			secretsText.alignment = TextAnchor.UpperRight;
			secretsText.fontSize = 15;
			RectTransform secretsTextRect = secretsText.GetComponent<RectTransform>();
			secretsTextRect.pivot = new Vector2(1, 1);
			secretsTextRect.anchorMin = new Vector2(1, 1);
			secretsTextRect.anchorMax = new Vector2(1, 1);
			secretsTextRect.anchoredPosition = new Vector2(-120, -100);
			secretsTextRect.sizeDelta = new Vector2(statsRect.sizeDelta.x - 10, 20);
			secretsTextRect.localScale = Vector3.one;
			this.secretsText = secretsText;

			// Total rank container
			RectTransform finalRankPanelRect = MakeRect(statsRect);
			finalRankPanelRect.anchorMin = new Vector2(1, 0);
			finalRankPanelRect.anchorMax = new Vector2(1, 1);
			finalRankPanelRect.pivot = new Vector2(1, 0.5f);
			finalRankPanelRect.sizeDelta = new Vector2(100, -20);
			finalRankPanelRect.anchoredPosition = new Vector2(-10, 0);
			Image finalRankPanel = finalRankPanelRect.gameObject.AddComponent<Image>();
			finalRankImage = finalRankPanel;
			finalRankPanel.sprite = bgSprite;
			finalRankPanel.color = new Color(0, 0, 0, 0.8f);
			finalRankPanel.type = Image.Type.Sliced;
			finalRankPanelRect.localScale = Vector3.one;

			// Total rank text
			Text finalRankText = MakeText(finalRankPanelRect);
			finalRankText.font = Plugin.gameFont;
			finalRankText.text = RankUtils.GetFormattedRankText(finalRank);
			finalRankText.fontSize = 15;
			finalRankText.resizeTextForBestFit = true;
			finalRankText.resizeTextMaxSize = 100;
			finalRankText.alignByGeometry = true;
			finalRankText.alignment = TextAnchor.MiddleCenter;
			RectTransform finalRankTextRect = finalRankText.GetComponent<RectTransform>();
			finalRankTextRect.pivot = new Vector2(0, 0);
			finalRankTextRect.anchorMin = new Vector2(0, 0);
			finalRankTextRect.anchorMax = new Vector2(1, 1);
			finalRankTextRect.anchoredPosition = new Vector2(0, 0);
			finalRankTextRect.sizeDelta = new Vector2(0, 0);
			finalRankTextRect.localScale = Vector3.one;
			this.finalRankText = finalRankText;

			// Challenge container
			RectTransform challengePanelRect = MakeRect(fieldUI);
			challengePanelRect.anchorMin = new Vector2(0, 1);
			challengePanelRect.anchorMax = new Vector2(0, 1);
			challengePanelRect.pivot = new Vector2(0, 1);
			challengePanelRect.sizeDelta = new Vector2(120, 120);
			challengePanelRect.anchoredPosition = new Vector2(470, -40);
			Image challengePanelImg = challengePanelRect.gameObject.AddComponent<Image>();
			challengePanelImg.sprite = bgSprite;
			challengePanelImg.color = challenge ? new Color(0xff / 255f, 0xa5 / 255f, 0, 0.8f) : new Color(0, 0, 0, 0.8f);
			challengePanelImg.type = Image.Type.Sliced;
			challengePanelRect.localScale = Vector3.one;
			this.challengePanel = challengePanelImg;

			// Challenge header
			Text challengeHeaderText = MakeText(challengePanelRect);
			challengeHeaderText.font = Plugin.gameFont;
			challengeHeaderText.text = "CHALLENGE";
			challengeHeaderText.alignment = TextAnchor.UpperCenter;
			challengeHeaderText.fontSize = 17;
			RectTransform challengeHeaderTextRect = challengeHeaderText.GetComponent<RectTransform>();
			challengeHeaderTextRect.pivot = new Vector2(0.5f, 1);
			challengeHeaderTextRect.anchorMin = new Vector2(0, 1);
			challengeHeaderTextRect.anchorMax = new Vector2(1, 1);
			challengeHeaderTextRect.anchoredPosition = new Vector2(0, -5);
			challengeHeaderTextRect.sizeDelta = new Vector2(0, 20);
			challengeHeaderTextRect.localScale = Vector3.one;

			// Challenge line break
			RectTransform challengeLineBreakRect = MakeRect(challengePanelRect);
			challengeLineBreakRect.anchorMin = new Vector2(0, 1);
			challengeLineBreakRect.anchorMax = new Vector2(1, 1);
			challengeLineBreakRect.pivot = new Vector2(0.5f, 1);
			challengeLineBreakRect.sizeDelta = new Vector2(-15, 2.5f);
			challengeLineBreakRect.anchoredPosition = new Vector2(0, -22.5f);
			challengeLineBreakRect.gameObject.AddComponent<Image>();
			challengeLineBreakRect.localScale = Vector3.one;

			// Challenge text
			Text challengeText = MakeText(challengePanelRect);
			challengeText.font = Plugin.gameFont;
			challengeText.text = data.levelChallengeEnabled ? data.levelChallengeText : "No challenge available for the level";
			challengeText.alignment = TextAnchor.UpperLeft;
			challengeText.fontSize = 15;
			RectTransform challengeTextRect = challengeText.GetComponent<RectTransform>();
			challengeTextRect.pivot = new Vector2(0.5f, 0.5f);
			challengeTextRect.anchorMin = new Vector2(0, 0);
			challengeTextRect.anchorMax = new Vector2(1, 1);
			challengeTextRect.anchoredPosition = new Vector2(0, -20);
			challengeTextRect.sizeDelta = new Vector2(-10, -18);
			challengeTextRect.localScale = Vector3.one;
			this.challengeText = challengeText;

			challengePanelRect.gameObject.SetActive(data.levelChallengeEnabled);
			imgRect.SetAsLastSibling();
			headerTexts.Clear();
			headerTexts.Add(timeHeaderText);
			headerTexts.Add(killsHeaderText);
			headerTexts.Add(styleHeaderText);
			headerTexts.Add(secretsHeaderText);
			UpdateUI();
		}

		private static string GetTimeStringFromSeconds(float s)
		{
			float seconds = s % 60;
			int minutes = (int)(s / 60);

			return minutes + ":" + seconds.ToString("00.000");
		}
	}
}
