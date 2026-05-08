using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Notifications;
using AngryLevelLoader.Utils;
using AngryUiComponents;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Fields
{
    internal class LevelField : CustomConfigField
    {
        private const string ASSET_PATH = "AngryLevelLoader/Fields/LevelField.prefab";

        private bool inited = false;
        public readonly BundleContainer bundleContainer;
        public readonly string levelId;

        // Properties

        private string _levelName;
        public string LevelName
        {
            get => _levelName;
            set
            {
                _levelName = value;

                if (currentUi != null)
					currentUi.levelHeader.text = (Locked) ? "???" : _levelName;
            }
        }

        private Sprite _previewImage = null;
        public Sprite PreviewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;

                if (currentUi != null)
                {
                    if (Locked)
                        return;

                    if (!PlayedBefore && !DoNotHideLevelPreviewWhenNotCompleted)
                        return;

					currentUi.levelThumbnail.sprite = _previewImage;
				}
            }
        }

		public bool PlayedBefore
		{
			get
			{
				return FinalRank != '-';
			}
		}

		private bool _discovered = true;
        public bool Discovered
        {
            get => _discovered;
            set
            {
                _discovered = value;
                hidden = !_discovered && HideIfNotPlayed;
			}
        }

		private bool _hideIfNotPlayed = false;
        public bool HideIfNotPlayed
        {
            get => _hideIfNotPlayed;
            set
            {
                _hideIfNotPlayed = value;
				hidden = !Discovered && _hideIfNotPlayed;
			}
        }

        private bool _doNotHideLevelPreviewWhenNotCompleted = false;
        public bool DoNotHideLevelPreviewWhenNotCompleted
        {
            get => _doNotHideLevelPreviewWhenNotCompleted;
            set
            {
                _doNotHideLevelPreviewWhenNotCompleted = value;

                if (currentUi != null)
                {
                    if (Locked)
                        return;

					if (!PlayedBefore && !_doNotHideLevelPreviewWhenNotCompleted)
						currentUi.levelThumbnail.sprite = AssetManager.notPlayedPreview;
					else
						currentUi.levelThumbnail.sprite = PreviewImage;
				}
			}
		}

        private bool _locked = false;
		public bool Locked
		{
			get => _locked;
			set
			{
				_locked = value;

				if (currentUi != null)
				{
					if (_locked)
					{
						currentUi.levelThumbnail.sprite = AssetManager.lockedPreview;
						currentUi.levelHeader.text = "???";

						currentUi.statContainer.gameObject.SetActive(false);
						currentUi.challengeContainer.gameObject.SetActive(false);

						currentUi.leaderboardsButton.interactable = false;
						currentUi.openSettingsButton.interactable = false;
						currentUi.settingsPanel.SetActive(false);
					}
					else
					{
						currentUi.levelHeader.text = LevelName;
						if (!PlayedBefore && !DoNotHideLevelPreviewWhenNotCompleted)
							currentUi.levelThumbnail.sprite = AssetManager.notPlayedPreview;
						else
							currentUi.levelThumbnail.sprite = PreviewImage;

						currentUi.leaderboardsButton.interactable = true;
						currentUi.openSettingsButton.interactable = true;
					}
				}
			}
		}

		private bool _isSecretLevel = false;
        public bool IsSecretLevel
        {
            get => _isSecretLevel;
            set
            {
                _isSecretLevel = value;

                if (currentUi != null)
                {
					currentUi.statContainer.gameObject.SetActive(!_isSecretLevel);
					currentUi.challengeContainer.gameObject.SetActive(!_isSecretLevel);
				}
            }
        }

		private float _time = 0;
        public float Time
        {
            get => _time;
            set
            {
                _time = value;

                if (currentUi != null)
					currentUi.timeText.text = $"{GetTimeStringFromSeconds(_time)} {AngryRankUtils.GetFormattedRankText(TimeRank)}";
			}
        }

        private char _timeRank = '-';
        public char TimeRank
        {
            get => _timeRank;
            set
            {
                _timeRank = value;

                if (currentUi != null)
					currentUi.timeText.text = $"{GetTimeStringFromSeconds(Time)} {AngryRankUtils.GetFormattedRankText(_timeRank)}";
			}
        }

        private int _kills = 0;
        public int Kills
        {
            get => _kills;
            set
            {
                _kills = value;

                if (currentUi != null)
					currentUi.killText.text = $"{_kills} {AngryRankUtils.GetFormattedRankText(KillsRank)}";
			}
        }

		private char _killsRank = '-';
        public char KillsRank
        {
            get => _killsRank;
            set
            {
                _killsRank = value;

				if (currentUi != null)
					currentUi.killText.text = $"{Kills} {AngryRankUtils.GetFormattedRankText(_killsRank)}";
			}
        }

        private int _style = 0;
        public int Style
        {
            get => _style;
            set
            {
                _style = value;

                if (currentUi != null)
				    currentUi.styleText.text = $"{_style} {AngryRankUtils.GetFormattedRankText(StyleRank)}";
			}
        }

        private char _styleRank = '-';
        public char StyleRank
        {
            get => _styleRank;
            set
            {
                _styleRank = value;

				if (currentUi != null)
					currentUi.styleText.text = $"{Style} {AngryRankUtils.GetFormattedRankText(_styleRank)}";
			}
        }

		private char _finalRank = '-';
        public char FinalRank
        {
            get => _finalRank;
            set
            {
                _finalRank = value;
                
                if (currentUi != null)
                {
					currentUi.finalRankText.text = AngryRankUtils.GetFormattedRankText(_finalRank);

					if (_finalRank == 'P')
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
				}
            }
        }

		public int SecretCount
        {
            get => SecretsString.Length;
        }

        private string _secretsString = "";
        public string SecretsString
        {
            get => _secretsString;
            set
            {
                _secretsString = value;

                if (currentUi != null)
                {
					if (SecretCount == 0)
					{
						currentUi.secretsHeader.gameObject.SetActive(false);
						currentUi.secretsText.gameObject.SetActive(false);
						currentUi.secretsIconContainer.gameObject.SetActive(false);
					}
					else if (SecretCount >= 1 && SecretCount <= 5)
					{
						currentUi.secretsHeader.gameObject.SetActive(true);
						currentUi.secretsText.gameObject.SetActive(false);
						currentUi.secretsIconContainer.gameObject.SetActive(true);

						for (int i = 0; i < SecretCount; i++)
						{
							currentUi.secretsIcons[i].gameObject.SetActive(true);
							currentUi.secretsIcons[i].fillCenter = _secretsString[i] == 'T';
						}

						for (int i = SecretCount; i < 5; i++)
						{
							currentUi.secretsIcons[i].gameObject.SetActive(false);
						}
					}
					else
					{
						currentUi.secretsHeader.gameObject.SetActive(true);
						currentUi.secretsText.gameObject.SetActive(true);
						currentUi.secretsIconContainer.gameObject.SetActive(false);
                        
						currentUi.secretsText.text = $"{DiscoveredSecrets} / {SecretCount}";
						if (DiscoveredSecrets == SecretCount)
							currentUi.secretsText.text = $"<color=aqua>{currentUi.secretsText.text}</color>";
					}
				}
            }
        }

        public int DiscoveredSecrets
        {
            get => SecretsString.ToCharArray().Count(c => c == 'T');
        }

        private bool _challengeEnabled = false;
        public bool ChallengeEnabled
        {
            get => _challengeEnabled;
            set
            {
                _challengeEnabled = value;

                if (currentUi != null)
					currentUi.challengeContainer.gameObject.SetActive(_challengeEnabled);
			}
        }

        private string _challengeText = "";
        public string ChallengeText
        {
            get => _challengeText;
            set
            {
                _challengeText = value;

                if (currentUi != null)
					currentUi.challengeText.text = ChallengeEnabled ? _challengeText : "No challenge available for the level";
			}
        }
        
        private bool _challengeDone = false;
        public bool ChallengeDone
        {
            get => _challengeDone;
            set
            {
                _challengeDone = value;

				if (currentUi != null)
					currentUi.challengeContainerImage.color = _challengeDone ? new Color(0xff / 255f, 0xa5 / 255f, 0, 0.8f) : new Color(0, 0, 0, 0.8f);
			}
        }

        internal Action onResetStats;
        internal Action onResetSecrets;
        internal Action onResetChallenge;

        private RectTransform container;
        private AngryLevelFieldComponent currentUi;

        private static Color perfectUiColor = new Color(171 / 255f, 108 / 255f, 2 / 255f);
        private static Color perfectStatsColor = new Color(225 / 255f, 154 / 255f, 0);
        private static Color perfectRankColor = new Color(241 / 255f, 168 / 255f, 8 / 255f);
        private static Color colorSilver = new Color(0xc0 / 255f, 0xc0 / 255f, 0xc0 / 255f);

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

		internal delegate void onLevelButtonPressDelegate();
		internal event onLevelButtonPressDelegate onLevelButtonPress;

        internal LevelField(ConfigPanel panel, BundleContainer parentContainer, string levelId) : base(panel, 600, 170)
        {
            this.bundleContainer = parentContainer;
            this.levelId = levelId;

            inited = true;
            if (container != null)
                OnCreateUI(container);
        }

        private static GameObject lastActiveSettingsPanel = null;
        private void UpdateUI()
        {
            if (currentUi == null)
                return;

			currentUi.statContainer.gameObject.SetActive(!IsSecretLevel);
			currentUi.challengeContainer.gameObject.SetActive(!IsSecretLevel);

            currentUi.timeText.text = $"{GetTimeStringFromSeconds(Time)} {AngryRankUtils.GetFormattedRankText(TimeRank)}";
            currentUi.killText.text = $"{Kills} {AngryRankUtils.GetFormattedRankText(KillsRank)}";
            currentUi.styleText.text = $"{Style} {AngryRankUtils.GetFormattedRankText(StyleRank)}";

            if (SecretCount == 0)
            {
                currentUi.secretsHeader.gameObject.SetActive(false);
                currentUi.secretsText.gameObject.SetActive(false);
                currentUi.secretsIconContainer.gameObject.SetActive(false);
            }
            else if (SecretCount >= 1 && SecretCount <= 5)
            {
				currentUi.secretsHeader.gameObject.SetActive(true);
				currentUi.secretsText.gameObject.SetActive(false);
				currentUi.secretsIconContainer.gameObject.SetActive(true);

                for (int i = 0; i < SecretCount; i++)
                {
                    currentUi.secretsIcons[i].gameObject.SetActive(true);
                    currentUi.secretsIcons[i].fillCenter = SecretsString[i] == 'T';
				}

				for (int i = SecretCount; i < 5; i++)
                {
					currentUi.secretsIcons[i].gameObject.SetActive(false);
				}
			}
            else
            {
				currentUi.secretsHeader.gameObject.SetActive(true);
				currentUi.secretsText.gameObject.SetActive(true);
				currentUi.secretsIconContainer.gameObject.SetActive(false);

                currentUi.secretsText.text = $"{DiscoveredSecrets} / {SecretCount}";
                if (DiscoveredSecrets == SecretCount)
                    currentUi.secretsText.text = $"<color=aqua>{currentUi.secretsText.text}</color>";
			}

            currentUi.finalRankText.text = AngryRankUtils.GetFormattedRankText(FinalRank);
            
            currentUi.challengeContainerImage.color = ChallengeDone ? new Color(0xff / 255f, 0xa5 / 255f, 0, 0.8f) : new Color(0, 0, 0, 0.8f);
            currentUi.challengeContainer.gameObject.SetActive(ChallengeEnabled);
            currentUi.challengeText.text = ChallengeEnabled ? ChallengeText : "No challenge available for the level";
            
            if (FinalRank == 'P')
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

            if (Locked)
            {
                currentUi.levelThumbnail.sprite = AssetManager.lockedPreview;
                currentUi.levelHeader.text = "???";

                currentUi.statContainer.gameObject.SetActive(false);
                currentUi.challengeContainer.gameObject.SetActive(false);

                currentUi.leaderboardsButton.interactable = false;
                currentUi.openSettingsButton.interactable = false;
                currentUi.settingsPanel.SetActive(false);
            }
            else
            {
                currentUi.levelHeader.text = LevelName;
                if (!PlayedBefore && !DoNotHideLevelPreviewWhenNotCompleted)
                    currentUi.levelThumbnail.sprite = AssetManager.notPlayedPreview;
                else
                    currentUi.levelThumbnail.sprite = PreviewImage;

				currentUi.leaderboardsButton.interactable = true;
				currentUi.openSettingsButton.interactable = true;
			}

			container.gameObject.SetActive(!hidden && !forceHidden);
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

            currentUi.levelButton.onClick.AddListener(() =>
            {
                if (Locked)
                    return;

                if (onLevelButtonPress != null)
                    onLevelButtonPress.Invoke();
            });

            currentUi.leaderboardsButton.onClick.AddListener(() =>
            {
                NotificationPanel.Open(new LeaderboardNotification(bundleContainer.BundleName, LevelName, bundleContainer.bundleGuid, levelId));
            });

            currentUi.openSettingsButton.onClick.AddListener(() =>
            {
                if (lastActiveSettingsPanel != null)
                    lastActiveSettingsPanel.SetActive(false);

                lastActiveSettingsPanel = currentUi.settingsPanel;
                currentUi.settingsPanel.SetActive(true);

                currentUi.StopAllCoroutines();

				currentUi.resetStatsText.text = "Reset Stats";
				currentUi.resetSecretsText.text = "Reset Secrets";
				currentUi.resetChallengeText.text = "Reset Challenge";

                currentUi.resetLevelVarsText.text = "Reset Level Variables";
				currentUi.resetBundleVarsText.text = "Reset Bundle Variables";
				currentUi.resetUserVarsText.text = "Reset User Variables";

				currentUi.resetStatsButton.interactable = FinalRank != '-';
				currentUi.resetSecretsButton.interactable = !IsSecretLevel && SecretCount != 0 && DiscoveredSecrets != 0;
				currentUi.resetChallengeButton.interactable = !IsSecretLevel && ChallengeEnabled && ChallengeDone;

                string levelMapVarFilePath = Path.Combine(AngryMapVarManager.GetCurrentMapVarsDirectory(), AngryMapVarManager.BUNDLES_DIRECTORY, bundleContainer.bundleGuid, AngryMapVarManager.LEVELS_DIRECTORY, levelId + AngryMapVarManager.MAPVAR_FILE_EXTENSION);
				string bundleMapVarFilePath = Path.Combine(AngryMapVarManager.GetCurrentMapVarsDirectory(), AngryMapVarManager.BUNDLES_DIRECTORY, bundleContainer.bundleGuid, bundleContainer.bundleGuid + AngryMapVarManager.MAPVAR_FILE_EXTENSION);

				currentUi.resetLevelVarsButton.interactable = File.Exists(levelMapVarFilePath);
                currentUi.resetBundleVarsButton.interactable = File.Exists(bundleMapVarFilePath);
                currentUi.resetUserVarsButton.interactable = true;

				currentUi.ResetSettingsButtons();
			});

            currentUi.closeSettingsButton.onClick.AddListener(() =>
            {
                currentUi.settingsPanel.SetActive(false);
            });

            currentUi.onResetStats = () =>
            {
                currentUi.resetStatsButton.interactable = false;
				currentUi.resetStatsText.text = "Reset Stats";

                if (onResetStats != null)
                    onResetStats();
			};

			currentUi.onResetSecrets = () =>
			{
				currentUi.resetSecretsButton.interactable = false;
				currentUi.resetSecretsText.text = "Reset Secrets";

				if (onResetSecrets != null)
					onResetSecrets();
			};

			currentUi.onResetChallenge = () =>
			{
				currentUi.resetChallengeButton.interactable = false;
				currentUi.resetChallengeText.text = "Reset Challenge";

				if (onResetChallenge != null)
					onResetChallenge();
			};

            currentUi.onResetLevelVars = () =>
            {
				currentUi.resetLevelVarsButton.interactable = false;
				currentUi.resetLevelVarsText.text = "Reset Level Variables";

				string levelMapVarFilePath = Path.Combine(AngryMapVarManager.GetCurrentMapVarsDirectory(), AngryMapVarManager.BUNDLES_DIRECTORY, bundleContainer.bundleGuid, AngryMapVarManager.LEVELS_DIRECTORY, levelId + AngryMapVarManager.MAPVAR_FILE_EXTENSION);
                if (File.Exists(levelMapVarFilePath))
                    File.Delete(levelMapVarFilePath);
            };

            currentUi.onResetBundleVars = () =>
            {
				currentUi.resetBundleVarsButton.interactable = false;
				currentUi.resetBundleVarsText.text = "Reset Bundle Variables";

				string bundleMapVarFilePath = Path.Combine(AngryMapVarManager.GetCurrentMapVarsDirectory(), AngryMapVarManager.BUNDLES_DIRECTORY, bundleContainer.bundleGuid, bundleContainer.bundleGuid + AngryMapVarManager.MAPVAR_FILE_EXTENSION);
			    if (File.Exists(bundleMapVarFilePath))
                    File.Delete(bundleMapVarFilePath);
            };

            currentUi.onResetUserVars = () =>
            {
				currentUi.resetUserVarsText.text = "Reset User Variables";

				NotificationPanel.Open(new ResetUserMapVarNotification());
            };

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
