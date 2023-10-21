using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
    public class AngryOnlineLevelFieldComponent : MonoBehaviour
    {
        public RawImage thumbnail;
        public Text infoText;

        public Button changelog;
        public Button install;
        public Button update;
        public Button cancel;

        public Button upvoteButton;
        public Image upvoteImage;
        public Button downvoteButton;
        public Image downvoteImage;
        public Text votes;

        public RectTransform downloadContainer;
        public Text progressText;
        public RectTransform progressBar;
    }
}
