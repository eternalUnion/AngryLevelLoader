using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryLevelLoader.Utils;
using PluginConfig.API;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace AngryLevelLoader.Fields
{
    internal class ConfigPanelForBundles : ConfigPanel
    {
        private class HideOnDisable : MonoBehaviour
        {
            void OnDisable()
            {
                gameObject.SetActive(false);
            }
        }

		private class ConditionalHideOnDisable : MonoBehaviour
		{
            public Func<bool> condition;

			void OnDisable()
			{
                if (condition != null && condition())
				    gameObject.SetActive(false);
			}
		}

		private const string ASSET_PATH_FAV_BUTTON = "AngryLevelLoader/Fields/BundlePanelPrefabs/FavButton.prefab";
		private const string ASSET_PATH_RANK_ICON = "AngryLevelLoader/Fields/BundlePanelPrefabs/RankIcon.prefab";
        private const string ASSET_PATH_DELETE_BUTTON = "AngryLevelLoader/Fields/BundlePanelPrefabs/DeleteButton.prefab";

        public readonly BundleContainer callback;

        protected Button favButton;
        protected Image favIcon;
        protected Text rank;
        protected Image rankBg;
        protected Button deleteButton;

        private bool _favourite = false;
        public bool Favourite
        {
            get => _favourite;
            set
            {
                _favourite = value;

                if (favIcon != null)
                {
                    favIcon.sprite = (_favourite) ? AssetManager.favouriteSelected : AssetManager.favouriteUnselected;
                    if (_favourite)
                        favButton.gameObject.SetActive(true);
                }
            }
        }

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

        private bool _forceHidden = false;
        public bool forceHidden
        {
            get => _forceHidden;
            set
            {
                _forceHidden = value;
                base.hidden = forceHidden || _hidden;
            }
        }

        private new bool _hidden = false;
        public override bool hidden
        {
            get => base.hidden;
            set
            {
                _hidden = value;
                base.hidden = forceHidden || _hidden;
            }
        }

        public ConfigPanelForBundles(BundleContainer creator, ConfigPanel parentPanel, string name, string guid) : base(parentPanel, name, guid, PanelFieldType.StandardWithBigIcon)
        {
            callback = creator;
        }

        public override GameObject CreateUI(Transform content)
        {
            base.CreateUI(content);

            if (currentMenu != null)
            {
                RectTransform button = currentMenu.button.GetComponent<RectTransform>();
                button.anchorMin = button.anchorMax = new Vector2(1, 0.5f);
                button.pivot = new Vector2(1, 0.5f);
                button.anchoredPosition = new Vector2(-150, 0);

                favButton = Addressables.InstantiateAsync(ASSET_PATH_FAV_BUTTON, currentMenu.transform).WaitForCompletion().GetComponentInChildren<Button>();
                favIcon = favButton.gameObject.GetComponent<Image>();
				favIcon.sprite = (Favourite) ? AssetManager.favouriteSelected : AssetManager.favouriteUnselected;
				AngryUIUtils.AddMouseEvents(currentMenu.gameObject, favButton,
					(e) => favButton.gameObject.SetActive(true),
					(e) => favButton.gameObject.SetActive(Favourite));
				favButton.gameObject.SetActive(Favourite);
				favButton.gameObject.AddComponent<ConditionalHideOnDisable>().condition = () => !Favourite;
				favButton.onClick.AddListener(() =>
                {
                    Favourite = !Favourite;
                    callback.Favourite = Favourite;
                });

                currentMenu.icon.GetComponent<RectTransform>().anchoredPosition += Vector2.right * 15;
				currentMenu.name.GetComponent<RectTransform>().anchoredPosition += Vector2.right * 15;

				rank = Addressables.InstantiateAsync(ASSET_PATH_RANK_ICON, currentMenu.transform).WaitForCompletion().GetComponentInChildren<Text>();
                rank.text = _rankText;
                rank.color = _rankTextColor;
                rank.alignByGeometry = true;
                rankBg = rank.transform.parent.GetComponent<Image>();
                rankBg.color = _rankBgColor;
                rankBg.fillCenter = _fillBgCenter;

                deleteButton = Addressables.InstantiateAsync(ASSET_PATH_DELETE_BUTTON, currentMenu.transform).WaitForCompletion().GetComponent<Button>();
                AngryUIUtils.AddMouseEvents(currentMenu.gameObject, deleteButton,
                    (e) => deleteButton.gameObject.SetActive(true),
                    (e) => deleteButton.gameObject.SetActive(false));
                deleteButton.gameObject.SetActive(false);
                deleteButton.gameObject.AddComponent<HideOnDisable>();
                deleteButton.onClick.AddListener(() =>
                {
                    if (callback != null)
                        callback.OpenDeletePanel();
                    else
                        Plugin.logger.LogError("Delete bundle button pressed but callback is null");
                });

                return currentMenu.gameObject;
            }

            return null;
        }
    }
}
