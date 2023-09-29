using AngryLevelLoader.Containers;
using PluginConfig.API;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace AngryLevelLoader.Fields
{
    public class ConfigPanelForBundles : ConfigPanel
    {
        private class HideOnDisable : MonoBehaviour
        {
            void OnDisable()
            {
                gameObject.SetActive(false);
            }
        }

        private const string ASSET_PATH_RANK_ICON = "AngryLevelLoader/RankIcon.prefab";
        private const string ASSET_PATH_DELETE_BUTTON = "AngryLevelLoader/DeleteButton.prefab";

        public readonly AngryBundleContainer callback;

        protected Text rank;
        protected Image rankBg;
        protected Button deleteButton;

        private string _rankText = " ";
        public string rankText
        {
            get => _rankText;
            set
            {
                _rankText = value;
                if (rank != null)
                    rank.text = _rankText;
            }
        }

        private Color _rankTextColor = Color.white;
        public Color rankTextColor
        {
            get => _rankTextColor;
            set
            {
                _rankTextColor = value;
                if (rank != null)
                    rank.color = _rankTextColor;
            }
        }

        private Color _rankBgColor = Color.white;
        public Color rankBgColor
        {
            get => _rankBgColor;
            set
            {
                _rankBgColor = value;
                if (rankBg != null)
                    rankBg.color = value;
            }
        }

        private bool _fillBgCenter = false;
        public bool fillBgCenter
        {
            get => _fillBgCenter;
            set
            {
                _fillBgCenter = value;
                if (rankBg == null)
                    return;

                rankBg.fillCenter = value;
            }
        }

        public ConfigPanelForBundles(AngryBundleContainer creator, ConfigPanel parentPanel, string name, string guid) : base(parentPanel, name, guid, PanelFieldType.StandardWithBigIcon)
        {
            callback = creator;
        }

        protected override GameObject CreateUI(Transform content)
        {
            base.CreateUI(content);

            if (currentMenu != null)
            {
                RectTransform button = currentMenu.button.GetComponent<RectTransform>();
                button.anchorMin = button.anchorMax = new Vector2(1, 0.5f);
                button.pivot = new Vector2(1, 0.5f);
                button.anchoredPosition = new Vector2(-150, 0);

                rank = Addressables.InstantiateAsync(ASSET_PATH_RANK_ICON, currentMenu.transform).WaitForCompletion().GetComponentInChildren<Text>();
                rank.text = _rankText;
                rank.color = _rankTextColor;
                rank.alignByGeometry = false;
                rankBg = rank.transform.parent.GetComponent<Image>();
                rankBg.color = _rankBgColor;
                rankBg.fillCenter = _fillBgCenter;

                deleteButton = Addressables.InstantiateAsync(ASSET_PATH_DELETE_BUTTON, currentMenu.transform).WaitForCompletion().GetComponent<Button>();
                UIUtils.AddMouseEvents(currentMenu.gameObject, deleteButton,
                    (e) => deleteButton.gameObject.SetActive(true),
                    (e) => deleteButton.gameObject.SetActive(false));
                deleteButton.gameObject.SetActive(false);
                deleteButton.gameObject.AddComponent<HideOnDisable>();
                deleteButton.onClick.AddListener(() =>
                {
                    if (callback != null)
                        callback.Delete();
                    else
                        Debug.LogError("Delete bundle button pressed but callback is null");
                });

                return currentMenu.gameObject;
            }

            return null;
        }
    }
}
