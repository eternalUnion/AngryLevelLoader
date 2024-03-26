using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AngryUiComponents
{
    public class AngryLevelFieldComponent : MonoBehaviour
    {
        public Text levelHeader;

        public Image fieldImage;

        public RectTransform statContainer;
        public Image statContainerImage;

        public Text[] headers;

        public Text timeText;
        public Text killText;
        public Text styleText;
        public Text secretsText;

        public Text secretsHeader;
        public GameObject secretsIconContainer;
        public Image[] secretsIcons;

        public GameObject settingsPanel;
        public Button openSettingsButton;
        public Button closeSettingsButton;
        public Button resetStatsButton;
        public TextMeshProUGUI resetStatsText;
		public Button resetSecretsButton;
		public TextMeshProUGUI resetSecretsText;
		public Button resetChallengeButton;
		public TextMeshProUGUI resetChallengeText;
        public Button resetLevelVarsButton;
        public TextMeshProUGUI resetLevelVarsText;
        public Button resetBundleVarsButton;
        public TextMeshProUGUI resetBundleVarsText;
        public Button resetUserVarsButton;
        public TextMeshProUGUI resetUserVarsText;

        public Action onResetStats;
        public Action onResetSecrets;
        public Action onResetChallenge;

        public Action onResetLevelVars;
        public Action onResetBundleVars;
        public Action onResetUserVars;

        private class OnDisableCallback : MonoBehaviour
        {
            public Action callback;

			private void OnDisable()
            {
                if (callback != null)
                    callback();
            }
        }

        private void Awake()
        {
			OnDisableCallback cb = settingsPanel.AddComponent<OnDisableCallback>();
            cb.callback = () =>
            {
                StopAllCoroutines();
            };

            ResetSettingsButtons();
		}

        public void ResetSettingsButtons()
        {
            resetStatsButton.onClick = new Button.ButtonClickedEvent();
            resetStatsButton.onClick.AddListener(OnResetStats);

            resetSecretsButton.onClick = new Button.ButtonClickedEvent();
            resetSecretsButton.onClick.AddListener(OnResetSecrets);

            resetChallengeButton.onClick = new Button.ButtonClickedEvent();
            resetChallengeButton.onClick.AddListener(OnResetChallenge);

            resetLevelVarsButton.onClick = new Button.ButtonClickedEvent();
            resetLevelVarsButton.onClick.AddListener(OnResetLevelVars);

            resetBundleVarsButton.onClick = new Button.ButtonClickedEvent();
            resetBundleVarsButton.onClick.AddListener(OnResetBundleVars);

            resetUserVarsButton.onClick = new Button.ButtonClickedEvent();
            resetUserVarsButton.onClick.AddListener(onResetUserVars.Invoke);
        }

        public void OnResetStats()
        {
			resetStatsButton.interactable = false;
			StartCoroutine(ResetButtonCoroutine(resetStatsButton, resetStatsText, onResetStats));
        }

        public void OnResetSecrets()
        {
			resetSecretsButton.interactable = false;
			StartCoroutine(ResetButtonCoroutine(resetSecretsButton, resetSecretsText, onResetSecrets));
		}

		public void OnResetChallenge()
		{
			resetChallengeButton.interactable = false;
			StartCoroutine(ResetButtonCoroutine(resetChallengeButton, resetChallengeText, onResetChallenge));
		}

        public void OnResetLevelVars()
        {
            resetLevelVarsButton.interactable = false;
            StartCoroutine(ResetButtonCoroutine(resetLevelVarsButton, resetLevelVarsText, onResetLevelVars));
        }

        public void OnResetBundleVars()
        {
            resetBundleVarsButton.interactable = false;
            StartCoroutine(ResetButtonCoroutine(resetBundleVarsButton, resetBundleVarsText, onResetBundleVars));
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

		public Image finalRankContainerImage;
        public Text finalRankText;

        public RectTransform challengeContainer;
        public Image challengeContainerImage;
        public Text challengeText;

        public Image levelThumbnail;
        public Button levelButton;
        public Button leaderboardsButton;
    }
}
