using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public class OnlineLevelField : CustomConfigField
	{
		public string bundleGuid;
		public string bundleBuildHash;

		private RectTransform currentUI = null;
		private Image uiImage;
		private RawImage currentPreview;

		private Texture2D _previewImage;
		public Texture2D previewImage
		{
			get => _previewImage;
			set
			{
				_previewImage = value;
				if (currentPreview != null)
					currentPreview.texture = _previewImage;
			}
		}

		public void DownloadPreviewImage(string URL)
		{
			UnityWebRequest req = UnityWebRequestTexture.GetTexture(URL);
			var handle = req.SendWebRequest();
			handle.completed += (e) =>
			{
				try
				{
					if (req.isHttpError || req.isNetworkError)
						return;

					previewImage = DownloadHandlerTexture.GetContent(req);
				}
				finally
				{
					req.Dispose();
				}
			};
		}

		private Text currentBundleInfo;
		private string _bundleName;
		public string bundleName
		{
			get => _bundleName;
			set
			{
				_bundleName = value;
				UpdateInfo();
			}
		}
		private string _author;
		public string author
		{
			get => _author;
			set
			{
				_author = value;
				UpdateInfo();
			}
		}
		private int _bundleFileSize;
		public int bundleFileSize
		{
			get => _bundleFileSize;
			set
			{
				_bundleFileSize = value;
				UpdateInfo();
			}
		}
		private string GetFileSizeString()
		{
			const int kilobyteSize = 1024;
			const int megabyteSize = 1024 * 1024;

			if (bundleFileSize >= megabyteSize)
				return $"{((float)_bundleFileSize / megabyteSize).ToString("0.00")} MB";
			if(bundleFileSize >= kilobyteSize)
				return $"{((float)_bundleFileSize / kilobyteSize).ToString("0.00")} KB";
			return $"{_bundleFileSize} B";
		}
		private void UpdateInfo()
		{
			if (currentBundleInfo == null)
				return;
			currentBundleInfo.text = $"{_bundleName}\n<color=#909090>Author: {_author}\nSize: {GetFileSizeString()}</color>";
		}

		private bool inited = false;
		public OnlineLevelField(ConfigPanel parentPanel) : base(parentPanel, 600, 170)
		{
			inited = true;
			if (currentUI != null)
				OnCreateUI(currentUI);
		}

		protected override void OnCreateUI(RectTransform fieldUI)
		{
			currentUI = fieldUI;
			if (!inited)
				return;

			Image bgImage = fieldUI.gameObject.AddComponent<Image>();
			uiImage = bgImage;
			if (LevelField.bgSprite == null)
			{
				LevelField.bgSprite = Resources.FindObjectsOfTypeAll<Image>().Where(i => i.sprite != null && i.sprite.name == "Background").First().sprite;
			}
			bgImage.sprite = LevelField.bgSprite;
			bgImage.type = Image.Type.Sliced;
			bgImage.fillMethod = Image.FillMethod.Radial360;
			bgImage.color = Color.black;

			RectTransform imgRect = LevelField.MakeRect(fieldUI);
			imgRect.anchorMin = new Vector2(0, 0.5f);
			imgRect.anchorMax = new Vector2(0, 0.5f);
			imgRect.pivot = new Vector2(0, 1);
			imgRect.sizeDelta = new Vector2(160, 120);
			imgRect.anchoredPosition = new Vector2(10, 0);
			imgRect.localScale = Vector3.one;
			RawImage img = currentPreview = imgRect.gameObject.AddComponent<RawImage>();
			img.texture = _previewImage;

			Text bundleInfo = currentBundleInfo = LevelField.MakeText(fieldUI);
			RectTransform infoRect = bundleInfo.GetComponent<RectTransform>();
			infoRect.anchorMin = new Vector2(180f / 600f, 0);
			infoRect.anchorMax = new Vector2(1, 1);
			infoRect.anchoredPosition = new Vector2(0, 0);
			infoRect.pivot = Vector2.zero;
			infoRect.sizeDelta = new Vector2(0, 0);
		}
	}
}
