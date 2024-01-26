using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Notifications;
using AngryUiComponents;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using RudeLevelScript;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace AngryLevelLoader.Fields
{
    public class LevelField : CustomConfigField
    {
        private const string ASSET_PATH = "AngryLevelLoader/Fields/LevelField.prefab";

        private bool inited = false;
        public AngryBundleContainer bundleContainer;
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

        private RectTransform container;
        private AngryLevelFieldComponent currentUi;

        public static Color perfectUiColor = new Color(171 / 255f, 108 / 255f, 2 / 255f);
        public static Color perfectStatsColor = new Color(225 / 255f, 154 / 255f, 0);
        public static Color perfectRankColor = new Color(241 / 255f, 168 / 255f, 8 / 255f);
        public static Color colorSilver = new Color(0xc0 / 255f, 0xc0 / 255f, 0xc0 / 255f);

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
                    if (AngrySceneManager.TryFindLevel(reqId, out LevelContainer reqLevel))
                    {
						if (reqLevel.finalRank.value[0] == '-')
						{
							locked = true;
							break;
						}
					}
                    else
                    {
						Plugin.logger.LogWarning($"Could not find level unlock requirement id for {data.uniqueIdentifier}, requested id was {reqId}");
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
                if (container != null)
					container.gameObject.SetActive(!hierarchyHidden && !forceHidden);
            }
        }

        public delegate void onLevelButtonPressDelegate();
        public event onLevelButtonPressDelegate onLevelButtonPress;

        public LevelField(ConfigPanel panel, AngryBundleContainer bundleContainer, RudeLevelData data) : base(panel, 600, 170)
        {
            this.bundleContainer = bundleContainer;
            this.data = data;

            inited = true;
            if (container != null)
                OnCreateUI(container);
        }

        public void UpdateUI()
        {
            if (currentUi == null)
                return;

            if (data.isSecretLevel)
            {
                currentUi.statContainer.gameObject.SetActive(false);
                currentUi.challengeContainer.gameObject.SetActive(false);
            }
            else
            {
                currentUi.statContainer.gameObject.SetActive(true);
                currentUi.challengeContainer.gameObject.SetActive(true);

                currentUi.timeText.text = $"{GetTimeStringFromSeconds(time)} {RankUtils.GetFormattedRankText(timeRank)}";
                currentUi.killText.text = $"{kills} {RankUtils.GetFormattedRankText(killsRank)}";
                currentUi.styleText.text = $"{style} {RankUtils.GetFormattedRankText(styleRank)}";
                currentUi.secretsText.text = $"{secrets} / {data.secretCount}";
                if (secrets == data.secretCount)
                    currentUi.secretsText.text = $"<color=aqua>{currentUi.secretsText.text}</color>";
                currentUi.finalRankText.text = RankUtils.GetFormattedRankText(finalRank);
                currentUi.challengeContainerImage.color = challenge ? new Color(0xff / 255f, 0xa5 / 255f, 0, 0.8f) : new Color(0, 0, 0, 0.8f);
                currentUi.challengeContainer.gameObject.SetActive(data.levelChallengeEnabled);
                currentUi.challengeText.text = data.levelChallengeEnabled ? data.levelChallengeText : "No challenge available for the level";
            }

            if (finalRank == 'P')
            {
                currentUi.fieldImage.color = perfectUiColor;
                currentUi.statContainerImage.color = perfectStatsColor;
                currentUi.finalRankContainerImage.color = perfectRankColor;
                foreach (var header in currentUi.headers)
                    header.color = Color.white;
            }
            else
            {
                currentUi.fieldImage.color = Color.black;
                currentUi.statContainerImage.color = new Color(0, 0, 0, 0.8f);
                currentUi.finalRankContainerImage.color = new Color(0, 0, 0, 0.8f);
                foreach (var header in currentUi.headers)
                    header.color = colorSilver;
            }

            if (locked)
            {
                currentUi.levelThumbnail.sprite = AssetManager.lockedPreview;
                currentUi.levelHeader.text = "???";

                currentUi.statContainer.gameObject.SetActive(false);
                currentUi.challengeContainer.gameObject.SetActive(false);

                currentUi.leaderboardsButton.interactable = false;
            }
            else
            {
                currentUi.levelHeader.text = data.levelName;
                if (!playedBefore)
                    currentUi.levelThumbnail.sprite = AssetManager.notPlayedPreview;
                else
                    currentUi.levelThumbnail.sprite = data.levelPreviewImage;

				currentUi.leaderboardsButton.interactable = true;
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

		public override void OnCreateUI(RectTransform fieldUI)
        {
            container = fieldUI;
            if (!inited)
                return;

            currentUi = Addressables.InstantiateAsync(ASSET_PATH, fieldUI.transform).WaitForCompletion().GetComponent<AngryLevelFieldComponent>();
            RectTransform currentUiRect = currentUi.GetComponent<RectTransform>();
			fieldUI.sizeDelta = currentUiRect.sizeDelta;
			currentUiRect.pivot = new Vector2(0, 1);
			currentUiRect.anchorMin = new Vector2(0, 1);
			currentUiRect.anchorMax = new Vector2(0, 1);
			currentUiRect.anchoredPosition = new Vector2(0, 0);

			currentUi.levelHeader.text = data == null ? "???" : data.levelName;
            if (data.levelPreviewImage != null)
                currentUi.levelThumbnail.sprite = data.levelPreviewImage;

            currentUi.levelButton.onClick.AddListener(() =>
            {
                if (locked)
                    return;

                if (onLevelButtonPress != null)
                    onLevelButtonPress.Invoke();
            });

            currentUi.leaderboardsButton.onClick.AddListener(() =>
            {
                NotificationPanel.Open(new LeaderboardNotification(bundleContainer.bundleData.bundleName, data.levelName, bundleContainer.bundleData.bundleGuid, data.uniqueIdentifier));
            });

            currentUi.timeText.text = $"{GetTimeStringFromSeconds(time)} {RankUtils.GetFormattedRankText(timeRank)}";

            currentUi.killText.text = $"{kills} {RankUtils.GetFormattedRankText(killsRank)}";

            currentUi.styleText.text = $"{style} {RankUtils.GetFormattedRankText(styleRank)}";

            currentUi.secretsText.text = $"{secrets} / {data.secretCount}";
            if (secrets == data.secretCount)
                currentUi.secretsText.text = $"<color=aqua>{currentUi.secretsText.text}</color>";

            currentUi.finalRankText.text = RankUtils.GetFormattedRankText(finalRank);

            currentUi.challengeContainerImage.color = challenge ? new Color(0xff / 255f, 0xa5 / 255f, 0, 0.8f) : new Color(0, 0, 0, 0.8f);

            currentUi.challengeText.text = data.levelChallengeEnabled ? data.levelChallengeText : "No challenge available for the level";

            currentUi.challengeContainer.gameObject.SetActive(data.levelChallengeEnabled);

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
