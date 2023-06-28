using BepInEx;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.TextCore;
using Sandbox;

namespace AngryLevelLoader
{
    public class SpaceField : CustomConfigField
    {
        public SpaceField(ConfigPanel parentPanel, float space) : base(parentPanel, 60, space)
        {

        }
    }

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(PluginConfig.PluginConfiguratorController.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_NAME = "AngryLevelLoader";
        public const string PLUGIN_GUID = "com.eternalUnion.angryLevelLoader";
        public const string PLUGIN_VERSION = "1.0.0";

        public static PluginConfigurator config;
        public static Dictionary<string, AngryBundleContainer> angryBundles = new Dictionary<string, AngryBundleContainer>();

        public static PropertyInfo p_SceneHelper_CurrentScene = typeof(SceneHelper).GetProperty("CurrentScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        public static PropertyInfo p_SceneHelper_LastScene = typeof(SceneHelper).GetProperty("LastScene", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        public static void ReplaceShaders()
        {
			foreach (Renderer rnd in Resources.FindObjectsOfTypeAll(typeof(Renderer)))
			{
                if (rnd.transform.parent != null && rnd.transform.parent.name == "Virtual Camera")
                    continue;

				foreach (Material mat in rnd.materials)
				{
					if (Plugin.shaderDictionary.TryGetValue(mat.shader.name, out Shader shader))
					{
						mat.shader = shader;
					}
				}
			}
		}

		public static void LinkMixers()
        {
            if (AudioMixerController.instance == null)
                return;

            AudioMixer[] realMixers = new AudioMixer[5]
            {
				AudioMixerController.instance.allSound,
				AudioMixerController.instance.musicSound,
				AudioMixerController.instance.goreSound,
				AudioMixerController.instance.doorSound,
				AudioMixerController.instance.unfreezeableSound
			};

            AudioMixer[] allMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();

			Dictionary<AudioMixerGroup, AudioMixerGroup> groupConversionMap = new Dictionary<AudioMixerGroup, AudioMixerGroup>();
			foreach (AudioMixer mixer in allMixers.Where(_mixer => _mixer.name.EndsWith("_rude")).AsEnumerable())
			{
                AudioMixerGroup rudeGroup = mixer.FindMatchingGroups("")[0];

                string realMixerName = mixer.name.Substring(0, mixer.name.Length - 5);
                AudioMixer realMixer = realMixers.Where(mixer => mixer.name == realMixerName).First();
                AudioMixerGroup realGroup = realMixer.FindMatchingGroups("")[0];

                groupConversionMap[rudeGroup] = realGroup;
                Debug.Log($"{mixer.name} => {realMixer.name}");
            }

			foreach (AudioSource source in Resources.FindObjectsOfTypeAll<AudioSource>())
            {
                if (source.outputAudioMixerGroup != null && groupConversionMap.TryGetValue(source.outputAudioMixerGroup, out AudioMixerGroup realGroup))
                {
                    source.outputAudioMixerGroup = realGroup;
                }
            }
        }

		public class LevelField : CustomConfigField
		{
            private bool inited = false;
            private static Sprite bgSprite;
            public RudeLevelScript.RudeLevelData data;

            public float time = 0;
            public char timeRank = '-';
            public int kills = 0;
            public char killsRank = '-';
            public int style = 0;
            public char styleRank = '-';
            public int secrets = 0;
            public char finalRank = '-';
            public bool challenge = false;

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

			public override bool hidden { get => base.hidden;
                set
                {
					base.hidden = value;
                    if (currentUI != null)
                        currentUI.gameObject.SetActive(!hierarchyHidden);
				}
            }

			public delegate void onLevelButtonPressDelegate();
			public event onLevelButtonPressDelegate onLevelButtonPress;

            public void UpdateUI()
            {
                if (currentUI == null)
                    return;

                timeText.text = $"{GetTimeStringFromSeconds(time)} {GetFormattedRankText(timeRank)}";
                killsText.text = $"{kills} {GetFormattedRankText(killsRank)}";
                styleText.text = $"{style} {GetFormattedRankText(styleRank)}";
                secretsText.text = $"{secrets} / {data.secretCount}";
                if (secrets == data.secretCount)
                    secretsText.text = $"<color=aqua>{secretsText.text}</color>";
                finalRankText.text = GetFormattedRankText(finalRank);
                challengePanel.color = challenge ? new Color(0xff / 255f, 0xa5 / 255f, 0, 0.8f) : new Color(0, 0, 0, 0.8f);
                challengePanel.gameObject.SetActive(data.levelChallengeEnabled);
                challengeText.text = data.levelChallengeEnabled ? data.levelChallengeText : "No challenge available for the level";

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
            }

			public void UpdateData()
			{
                if (currentUI == null)
                    return;

                levelNameText.text = data.levelName;
                levelPreviewImage.sprite = data.levelPreviewImage ?? levelPreviewImage.sprite;
                UpdateUI();
            }

			public LevelField(ConfigPanel panel, RudeLevelScript.RudeLevelData data) : base(panel, 600, 170)
			{
                this.data = data;

                inited = true;
                if (currentUI != null)
                    OnCreateUI(currentUI);
			}

            private static Text MakeText(Transform parent)
            {
                GameObject obj = new GameObject();
                RectTransform rect = obj.AddComponent<RectTransform>();
                rect.SetParent(parent);

                obj.transform.localScale = Vector3.one;

                return obj.AddComponent<Text>();
            }

			private static RectTransform MakeRect(Transform parent)
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
                levelText.font = gameFont;
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
				timeHeaderText.font = gameFont;
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
				timeText.font = gameFont;
				timeText.text = $"{GetTimeStringFromSeconds(time)} {GetFormattedRankText(timeRank)}";
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
				killsHeaderText.font = gameFont;
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
				killsText.font = gameFont;
				killsText.text = $"{kills} {GetFormattedRankText(killsRank)}";
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
				styleHeaderText.font = gameFont;
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
				styleText.font = gameFont;
				styleText.text = $"{style} {GetFormattedRankText(styleRank)}";
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
				secretsHeaderText.font = gameFont;
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
				secretsText.font = gameFont;
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
				finalRankText.font = gameFont;
				finalRankText.text = GetFormattedRankText(finalRank);
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
				challengeHeaderText.font = gameFont;
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
				challengeText.font = gameFont;
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
                UpdateUI();
			}

            private static string GetTimeStringFromSeconds(float s)
            {
				float seconds = s;
                int minutes = (int)(seconds / 60);
                seconds %= 60;

				return minutes + ":" + seconds.ToString("00.000");
			}

            private static Dictionary<char, Color> rankColors = new Dictionary<char, Color>()
            {
                { 'D', new Color(0, 0x94 / 255f, 0xFF / 255f) },
                { 'C', new Color(0x4C / 255f, 0xFF / 255f, 0) },
                { 'B', new Color(0xFF / 255f, 0xD8 / 255f, 0) },
                { 'A', new Color(0xFF / 255f, 0x6A / 255f, 0) },
                { 'S', Color.red },
                { 'P', Color.white },
                { '-', Color.gray }
            };

            private static string GetFormattedRankText(char rank)
            {
                Color textColor;
                if (!rankColors.TryGetValue(rank, out textColor))
                    textColor = Color.gray;

                return $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{rank}</color>";
            }
		}

		public class LevelContainer
        {
            public LevelField field;

            public delegate void onLevelButtonPressDelegate();
            public event onLevelButtonPressDelegate onLevelButtonPress;
            public bool TriggerLevelButtonPress()
            {
                if (onLevelButtonPress == null)
                    return false;

                onLevelButtonPress.Invoke();
                return true;
			}

            public bool hidden
            {
                get => field.hidden;
                set => field.hidden = value;
            }

            public FloatField time;
            public StringField timeRank;
            public IntField kills;
            public StringField killsRank;
            public IntField style;
            public StringField styleRank;
            public StringField finalRank;
            public StringField secrets;
            public BoolField challenge;

            public void UpdateUI()
            {
                field.time = time.value;
                field.timeRank = timeRank.value[0];
                field.kills = kills.value;
                field.killsRank = killsRank.value[0];
                field.style = style.value;
                field.styleRank = styleRank.value[0];

                field.finalRank = finalRank.value[0];
                field.secrets = secrets.value.ToCharArray().Count(c => c == 'T');
                field.challenge = challenge.value;

                field.UpdateUI();
            }

            public void AssureSecretsSize()
            {
				int currentSecretCount = secrets.value.Length;
				if (currentSecretCount != field.data.secretCount)
				{
					Debug.LogWarning("Inconsistent secrets data detected");
					string secretsStr = secrets.value;

					if (currentSecretCount < field.data.secretCount)
					{
						while (secretsStr.Length != field.data.secretCount)
							secretsStr += 'F';
						secrets.value = secretsStr;
						secrets.defaultValue = secretsStr.Replace('T', 'F');
					}
					else
					{
						secrets.value = secrets.value.Substring(0, field.data.secretCount);
						secrets.defaultValue = secrets.value.Replace('T', 'F');
					}
				}
			}

            public void UpdateData(RudeLevelScript.RudeLevelData data)
            {
                field.data = data;

                AssureSecretsSize();
                field.UpdateData();
            }

			public LevelContainer(ConfigPanel panel, RudeLevelScript.RudeLevelData data)
            {
                field = new LevelField(panel, data);
                field.onLevelButtonPress += () =>
                {
                    if (onLevelButtonPress != null)
                        onLevelButtonPress.Invoke();
                };

                time = new FloatField(panel, "", $"l_{data.uniqueIdentifier}_time", 0) { hidden = true, presetLoadPriority = -1 };
                timeRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_timeRank", "-") { hidden = true };
				kills = new IntField(panel, "", $"l_{data.uniqueIdentifier}_kills", 0) { hidden = true };
				killsRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_killsRank", "-") { hidden = true };
				style = new IntField(panel, "", $"l_{data.uniqueIdentifier}_style", 0) { hidden = true };
				styleRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_styleRank", "-") { hidden = true };

				finalRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_finalRank", "-") { hidden = true };

                string defSecretText = "";
                for (int i = 0; i < data.secretCount; i++)
                    defSecretText += 'F';
                secrets = new StringField(panel, "", $"l_{data.uniqueIdentifier}_secrets", defSecretText, true) { hidden = true };
                if (secrets.value.Length != data.secretCount)
                {
                    Debug.LogWarning($"Secret orb count does not match for {data.scenePath}, resetting");
                    secrets.value = defSecretText;
                }

                challenge = new BoolField(panel, "", $"l_{data.uniqueIdentifier}_challenge", false) { hidden = true };

                UpdateUI();

                time.onValueChange += (e) =>
                {
                    AssureSecretsSize();
                    UpdateUI();
                };
            }
        }

		public class AngryBundleContainer
        {
            public List<AssetBundle> bundles = new List<AssetBundle>();
            public string pathToAngryBundle;

            public static string lastLoadedScenePath = "";

            public ConfigPanel rootPanel;
            public ButtonField reloadButton;
            public ConfigHeader statusText;
            public ConfigDivision sceneDiv;
            public Dictionary<string, LevelContainer> levels = new Dictionary<string, LevelContainer>();

            public IEnumerable<string> GetAllScenePaths()
            {
                foreach (AssetBundle bundle in bundles)
                    foreach (string path in bundle.GetAllScenePaths())
                        yield return path;
            }

            public IEnumerable<RudeLevelScript.RudeLevelData> GetAllLevelData()
            {
                foreach (AssetBundle bundle in bundles)
                {
                    if (bundle.GetAllScenePaths().Length != 0)
                        continue;

                    foreach (RudeLevelScript.RudeLevelData data in bundle.LoadAllAssets<RudeLevelScript.RudeLevelData>())
                        yield return data;
                }
            }

            private void LoadBundle(string path)
            {
				using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read))
				using (BinaryReader br = new BinaryReader(fs))
				{
					int bundleCount = br.ReadInt32();
					int currentOffset = 0;

					for (int i = 0; i < bundleCount; i++)
					{
						fs.Seek(4 + i * 4, SeekOrigin.Begin);
						int bundleLen = br.ReadInt32();

                        byte[] bundleData = new byte[bundleLen];
                        fs.Seek(4 + bundleCount * 4 + currentOffset, SeekOrigin.Begin);
                        fs.Read(bundleData, 0, bundleLen);
                        bundles.Add(AssetBundle.LoadFromMemory(bundleData));

						currentOffset += bundleLen;
					}
				}
			}

            public void UpdateScenes()
            {
                sceneDiv.interactable = false;
                sceneDiv.hidden = false;
                statusText.text = "Reloading scenes...";
                statusText.hidden = false;

                foreach (AssetBundle bundle in bundles)
                {
                    try
                    {
                        bundle.Unload(false);
                    }
                    catch (Exception) { }
                }
                bundles.Clear();

                if (!File.Exists(pathToAngryBundle))
                {
                    statusText.text = "Could not find the file";
                    sceneDiv.hidden = true;
                    return;
                }

                LoadBundle(pathToAngryBundle);

                // Disable all level interfaces
                foreach (KeyValuePair<string, LevelContainer> pair in levels)
                    pair.Value.hidden = true;

                foreach (string scenePath in GetAllScenePaths())
                {
                    if (levels.TryGetValue(scenePath, out LevelContainer container))
                    {
                        container.hidden = false;
                        container.UpdateData(GetAllLevelData().Where(data => data.scenePath == scenePath).First());
                    }
                    else
                    {
                        RudeLevelScript.RudeLevelData data = GetAllLevelData().Where(data => data.scenePath == scenePath).First();
                        LevelContainer levelContainer = new LevelContainer(sceneDiv, data);
                        levelContainer.onLevelButtonPress += () =>
                        {
							MonoSingleton<PrefsManager>.Instance.SetInt("difficulty", selectedDifficulty);
							SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
							p_SceneHelper_LastScene.SetValue(null, p_SceneHelper_CurrentScene.GetValue(null) as string);
							p_SceneHelper_CurrentScene.SetValue(null, scenePath);
						};

                        SceneManager.sceneLoaded += (scene, mode) =>
                        {
                            if (levelContainer.hidden)
                                return;

                            if (scene.path == scenePath)
                            {
                                ReplaceShaders();
                                LinkMixers();
                                lastLoadedScenePath = scenePath;

                                string secretString = levelContainer.secrets.value;
                                foreach (Bonus bonus in Resources.FindObjectsOfTypeAll<Bonus>())
                                {
                                    if (bonus.gameObject.scene.path != scenePath)
                                        continue;

                                    if (bonus.secretNumber >= 0 && secretString[bonus.secretNumber] == 'T')
                                    {
                                        bonus.beenFound = true;
                                        bonus.BeenFound();
                                    }
                                }
                            }
						};

                        levels[scenePath] = levelContainer;
                    }
                }

                statusText.hidden = true;
                sceneDiv.interactable = true;
            }

            public AngryBundleContainer(string path)
            {
                this.pathToAngryBundle = path;

                rootPanel = new ConfigPanel(config.rootPanel, Path.GetFileNameWithoutExtension(path), Path.GetFileName(path));
                
                reloadButton = new ButtonField(rootPanel, "Reload File", "reloadButton");
                reloadButton.onClick += UpdateScenes;
                
                new SpaceField(rootPanel, 5);

                new ConfigHeader(rootPanel, "Scenes");
                statusText = new ConfigHeader(rootPanel, "", 16, TextAnchor.MiddleLeft);
                statusText.hidden = true;
                sceneDiv = new ConfigDivision(rootPanel, "sceneDiv_" + rootPanel.guid);
            }
        }

        private static Dictionary<string, AngryBundleContainer> failedBundles = new Dictionary<string, AngryBundleContainer>();

        public static void ReloadBundles()
        {
            foreach (KeyValuePair<string, AngryBundleContainer> pair in angryBundles)
                pair.Value.rootPanel.interactable = false;

            string bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Levels");
            if (Directory.Exists(bundlePath))
            {
                foreach (string path in Directory.GetFiles(bundlePath))
                {
                    if (angryBundles.TryGetValue(path, out AngryBundleContainer levelAsset))
                    {
                        levelAsset.rootPanel.interactable = true;
                        levelAsset.rootPanel.hidden = false;
                        continue;
                    }
                    else if (failedBundles.TryGetValue(path, out AngryBundleContainer failedBundle))
                    {
                        try
                        {
                            failedBundle.UpdateScenes();
                        }
						catch (Exception e)
                        {
							Debug.LogWarning($"Exception thrown while loading level bundle: {e}");
                            continue;
						}

                        failedBundle.rootPanel.interactable = true;
                        failedBundle.rootPanel.hidden = false;
                        failedBundles.Remove(path);
                        angryBundles[path] = failedBundle;
                        continue;
					}

                    AngryBundleContainer level = null;

                    try
                    {
                        level = new AngryBundleContainer(path);
                        level.UpdateScenes();
                    }
                    catch(Exception e)
                    {
                        Debug.LogWarning($"Exception thrown while loading level bundle: {e}");
                        
                        if (level != null)
                        {
                            level.rootPanel.hidden = true;
                            failedBundles[path] = level;
                        }
                        continue;
                    }

                    angryBundles[path] = level;
                }
            }
        }

        public static Harmony harmony;

        public static Font gameFont;

        public static bool isInCustomScene = false;
        public static RudeLevelScript.RudeLevelData currentLevelData;
        public static LevelContainer currentLevelContainer;
        public static int selectedDifficulty;

        public static void CheckIsInCustomScene(Scene current)
        {
			foreach (AngryBundleContainer container in angryBundles.Values)
			{
				if (container.GetAllScenePaths().Contains(current.path))
				{
					isInCustomScene = true;
					currentLevelData = container.GetAllLevelData().Where(data => data.scenePath == current.path).First();
					currentLevelContainer = container.levels[current.path];

					return;
				}
			}

			isInCustomScene = false;
			currentLevelData = null;
			currentLevelContainer = null;
		}

        private static string[] difficultyArr = new string[] { "HARMLESS", "LENIENT", "STANDARD", "VIOLENT" };

		private void Awake()
        {
            // Plugin startup logic
            config = PluginConfigurator.Create("Angry Level Loader", PLUGIN_GUID);
            harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
            InitShaderDictionary();

            SceneManager.activeSceneChanged += (before, after) =>
            {
                CheckIsInCustomScene(after);
			};

            gameFont = LoadObject<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf");

            ButtonField reloadButton = new ButtonField(config.rootPanel, "Scan For Levels", "refreshButton");
            reloadButton.onClick += ReloadBundles;
            StringListField difficultySelect = new StringListField(config.rootPanel, "Difficulty", "difficultySelect", difficultyArr, "VIOLENT");
            difficultySelect.onValueChange += (e) =>
            {
                selectedDifficulty = Array.IndexOf(difficultyArr, e.value);
                if (selectedDifficulty == -1)
                {
                    Debug.LogWarning("Invalid difficulty, setting to violent");
                    selectedDifficulty = 3;
                }
            };
            difficultySelect.TriggerValueChangeEvent();

			new ConfigHeader(config.rootPanel, "Level Bundles");
            ReloadBundles();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

		public static ResourceLocationMap resourceMap = null;
        private static void InitResourceMap()
        {
			if (resourceMap == null)
			{
				Addressables.InitializeAsync().WaitForCompletion();
				resourceMap = Addressables.ResourceLocators.First() as ResourceLocationMap;
			}
		}

		public static T LoadObject<T>(string path)
		{
            InitResourceMap();

			Debug.Log($"Loading {path}");
			KeyValuePair<object, IList<IResourceLocation>> obj;

			try
			{
				obj = resourceMap.Locations.Where(
					(KeyValuePair<object, IList<IResourceLocation>> pair) =>
					{
						return (pair.Key as string) == path;
						//return (pair.Key as string).Equals(path, StringComparison.OrdinalIgnoreCase);
					}).First();
			}
			catch (Exception) { return default(T); }

			return Addressables.LoadAsset<T>(obj.Value.First()).WaitForCompletion();
		}

		public static Dictionary<string, Shader> shaderDictionary = new Dictionary<string, Shader>();
        private void InitShaderDictionary()
        {
            InitResourceMap();
            foreach (KeyValuePair<object, IList<IResourceLocation>> pair in resourceMap.Locations)
            {
                string path = pair.Key as string;
                if (!path.EndsWith(".shader"))
                    continue;

                Shader shader = LoadObject<Shader>(path);
                shaderDictionary[shader.name] = shader;
            }

            shaderDictionary.Remove("ULTRAKILL/PostProcessV2");
        }
    }

    [HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Shader))]
    public static class MaterialShaderPatch_Ctor0
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Shader __0)
        {
            if (__0 != null && __0.name != null && Plugin.shaderDictionary.TryGetValue(__0.name, out Shader shader))
            {
                __0 = shader;
            }

			return true;
        }
    }

	[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Material))]
	public static class MaterialShaderPatch_Ctor1
	{
		[HarmonyPostfix]
		public static void Postfix(Material __instance)
		{
            if (__instance.shader != null && Plugin.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
            {
                __instance.shader = shader;
			}
		}
	}

    [HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(string))]
    public static class MaterialShaderPatch_Ctor2
    {
	    [HarmonyPostfix]
	    public static void Postfix(Material __instance)
	    {
            if (__instance.shader != null && Plugin.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
            {
                __instance.shader = shader;
			}
		}
    }

    [HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.RestartScene))]
    public static class SceneHelperRestart_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SceneManager.GetActiveScene().path != Plugin.AngryBundleContainer.lastLoadedScenePath)
                return true;

			foreach (MonoBehaviour monoBehaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
			{
				if (!(monoBehaviour == null) && !(monoBehaviour.gameObject.scene.name == "DontDestroyOnLoad"))
				{
					monoBehaviour.enabled = false;
				}
			}

            SceneManager.LoadScene(Plugin.AngryBundleContainer.lastLoadedScenePath);

			return false;
        }
    }
}
