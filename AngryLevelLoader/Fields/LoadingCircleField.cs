using PluginConfig.API.Fields;
using PluginConfig.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace AngryLevelLoader.Fields
{
    public class LoadingCircleField : CustomConfigField
    {
        public static Sprite loadingIcon;
        private static bool _spriteInit = false;
        public static void SpriteInit()
        {
            if (_spriteInit)
                return;
            _spriteInit = true;

            UnityWebRequest spriteReq = UnityWebRequestTexture.GetTexture("file://" + Path.Combine(Plugin.workingDir, "loading-icon.png"));
            var handle = spriteReq.SendWebRequest();
            handle.completed += (e) =>
            {
                try
                {
                    if (spriteReq.isHttpError || spriteReq.isNetworkError)
                        return;

                    Texture2D texture = DownloadHandlerTexture.GetContent(spriteReq);
                    loadingIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    if (currentImage != null)
                        currentImage.sprite = loadingIcon;
                }
                finally
                {
                    spriteReq.Dispose();
                }
            };
        }

        private bool initialized = false;
        public LoadingCircleField(ConfigPanel parentPanel) : base(parentPanel, 600, 60)
        {
            initialized = true;

            if (currentContainer != null)
                OnCreateUI(currentContainer);
        }

        internal class LoadingBarSpin : MonoBehaviour
        {
            private void Update()
            {
                transform.Rotate(Vector3.forward, Time.unscaledDeltaTime * 360f);
            }
        }

        private RectTransform currentContainer;
        private GameObject currentUi;
        private static Image currentImage;
		public override void OnCreateUI(RectTransform fieldUI)
        {
            currentContainer = fieldUI;
            if (!initialized)
                return;

            SpriteInit();
            GameObject loadingCircle = currentUi = new GameObject();
            loadingCircle.transform.SetParent(fieldUI);
            RectTransform loadingRect = loadingCircle.AddComponent<RectTransform>();
            loadingRect.localScale = Vector3.one;
            loadingRect.sizeDelta = new Vector2(60, 60);
            loadingRect.anchorMin = loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.pivot = new Vector2(0.5f, 0.5f);
            loadingRect.anchoredPosition = Vector2.zero;

            loadingRect.gameObject.AddComponent<LoadingBarSpin>();
            currentImage = loadingRect.gameObject.AddComponent<Image>();
            currentImage.sprite = loadingIcon;

            if (hierarchyHidden)
                currentContainer.gameObject.SetActive(false);
        }

        public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
        {
            if (currentContainer != null)
                currentContainer.gameObject.SetActive(!hierarchyHidden);
        }
    }
}
