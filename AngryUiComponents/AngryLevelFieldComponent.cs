using System;
using System.Collections.Generic;
using UnityEngine;
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
