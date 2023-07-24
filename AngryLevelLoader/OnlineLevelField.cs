using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public class OnlineLevelField : CustomConfigField
	{
		public string bundleGuid;
		public string bundleBuildHash;

		private RectTransform currentUI = null;
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

		public void DownloadPreviewImage(string URL, bool force)
		{
			if (_previewImage != null && !force)
				return;

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
		public enum OnlineLevelStatus
		{
			installed,
			notInstalled,
			updateAvailable
		}
		private OnlineLevelStatus _status = OnlineLevelStatus.notInstalled;
		public OnlineLevelStatus status
		{
			get => _status;
			set
			{
				_status = value;
				UpdateInfo();
			}
		}
		private string GetStatusString()
		{
			if (_status == OnlineLevelStatus.notInstalled)
				return $"<color=red>Not installed</color>";
			else if (_status == OnlineLevelStatus.updateAvailable)
				return $"<color=cyan>Update available</color>";
			else
				return $"<color=lime>Installed</color>";
		}
		private void UpdateInfo()
		{
			if (currentBundleInfo == null)
				return;
			currentBundleInfo.text = $"{_bundleName}\n<color=#909090>Author: {_author}\nSize: {GetFileSizeString()}</color>\n{GetStatusString()}";
		}

		private bool installActive = false;
		private Button installButton;
		private Button updateButton;
		private Button cancelButton;
		private RectTransform progressBarBase;
		private RectTransform progressBar;
		private Text downloadProgressText;

		internal class DisableWhenHidden : MonoBehaviour
		{
			void OnDisable()
			{
				gameObject.SetActive(false);
			}
		}

		private static GameObject sampleMenuButton;
		private static RectTransform CreateButton(Transform parent, string text, bool hideWhenUnfocused, GameObject field, Action<BaseEventData> onHover)
		{
			if (sampleMenuButton == null)
			{
				GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
				sampleMenuButton = canvas.transform.Find("OptionsMenu/Gameplay Options/Scroll Rect (1)/Contents/Controller Rumble").gameObject;
			}

			GameObject resetButton = GameObject.Instantiate(sampleMenuButton.transform.Find("Select").gameObject, parent);
			GameObject.Destroy(resetButton.GetComponent<HudOpenEffect>());
			resetButton.transform.Find("Text").GetComponent<Text>().text = text;
			RectTransform resetRect = resetButton.GetComponent<RectTransform>();
			Button resetComp = resetButton.GetComponent<Button>();
			resetComp.onClick = new Button.ButtonClickedEvent();

			if (hideWhenUnfocused)
			{
				resetButton.SetActive(false);
				resetButton.AddComponent<DisableWhenHidden>();
				EventTrigger trigger = field.AddComponent<EventTrigger>();
				EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
				mouseOn.callback.AddListener((e) => onHover(e));
				EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
				mouseOff.callback.AddListener((BaseEventData e) => { resetButton.SetActive(false); });
				trigger.triggers.Add(mouseOn);
				trigger.triggers.Add(mouseOff);
				Utils.AddScrollEvents(trigger, Utils.GetComponentInParent<ScrollRect>(field.transform));
			}

			resetRect.transform.localScale = Vector3.one;
			return resetRect;
		}

		private bool inited = false;
		public OnlineLevelField(ConfigPanel parentPanel) : base(parentPanel, 600, 170)
		{
			inited = true;
			if (currentUI != null)
				OnCreateUI(currentUI);
		}

		private static float progressBarThickness = 2.5f;
		protected override void OnCreateUI(RectTransform fieldUI)
		{
			currentUI = fieldUI;
			if (!inited)
				return;

			Image bgImage = fieldUI.gameObject.AddComponent<Image>();
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
			imgRect.pivot = new Vector2(0, 0.5f);
			imgRect.sizeDelta = new Vector2(160, 120);
			imgRect.anchoredPosition = new Vector2(10, 0);
			imgRect.localScale = Vector3.one;
			RawImage img = currentPreview = imgRect.gameObject.AddComponent<RawImage>();
			img.texture = _previewImage;

			Text bundleInfo = currentBundleInfo = LevelField.MakeText(fieldUI);
			bundleInfo.font = Plugin.gameFont;
			bundleInfo.fontSize = 18;
			bundleInfo.alignment = TextAnchor.UpperLeft;
			RectTransform infoRect = bundleInfo.GetComponent<RectTransform>();
			infoRect.anchorMin = new Vector2(0, 0.5f);
			infoRect.anchorMax = new Vector2(0, 0.5f);
			infoRect.anchoredPosition = new Vector2(180, 60);
			infoRect.pivot = new Vector2(0, 1);
			infoRect.sizeDelta = new Vector2(300, 160);
			UpdateInfo();

			RectTransform install = CreateButton(fieldUI, "Install", true, fieldUI.gameObject, e => { if (installActive) installButton.gameObject.SetActive(true); });
			installButton = install.GetComponent<Button>();
			installButton.onClick.AddListener(StartDownload);
			install.anchorMin = install.anchorMax = new Vector2(1, 0.5f);
			install.pivot = new Vector2(1, 0.5f);
			install.anchoredPosition = new Vector2(-10, 0);
			install.sizeDelta = new Vector2(100, 100);

			RectTransform update = CreateButton(fieldUI, "Update", false, fieldUI.gameObject, e => updateButton.gameObject.SetActive(true));
			updateButton = update.GetComponent<Button>();
			updateButton.onClick.AddListener(StartDownload);
			update.anchorMin = update.anchorMax = new Vector2(1, 0.5f);
			update.pivot = new Vector2(1, 0.5f);
			update.anchoredPosition = new Vector2(-10, 0);
			update.sizeDelta = new Vector2(100, 100);

			RectTransform cancel = CreateButton(fieldUI, "Cancel", false, fieldUI.gameObject, e => updateButton.gameObject.SetActive(true));
			cancelButton = cancel.GetComponent<Button>();
			cancel.anchorMin = cancel.anchorMax = new Vector2(1, 0f);
			cancel.pivot = new Vector2(1, 0f);
			cancel.anchoredPosition = new Vector2(-10, 10);
			cancel.sizeDelta = new Vector2(100, 40);
			cancelButton = cancel.GetComponent<Button>();
			cancelButton.onClick.AddListener(() =>
			{
				if (!downloading || currentRequest == null)
					return;

				currentRequest.Abort();
				downloading = false;
				UpdateUI();
			});

			progressBarBase = new GameObject().AddComponent<RectTransform>();
			progressBarBase.SetParent(fieldUI);
			progressBarBase.anchorMin = progressBarBase.anchorMax = new Vector2(1, 0);
			progressBarBase.pivot = new Vector2(0, 0);
			progressBarBase.anchoredPosition = new Vector2(-110, 60);
			progressBarBase.sizeDelta = new Vector2(100, 40);
			progressBarBase.localScale = Vector3.one;
			Image progressBarBaseImg = progressBarBase.gameObject.AddComponent<Image>();
			progressBarBaseImg.sprite = cancel.GetComponent<Image>().sprite;
			progressBarBaseImg.type = Image.Type.Sliced;
			progressBarBaseImg.fillCenter = false;

			progressBar = new GameObject().AddComponent<RectTransform>();
			progressBar.SetParent(fieldUI);
			progressBar.anchorMin = progressBar.anchorMax = new Vector2(1, 0);
			progressBar.pivot = new Vector2(0, 0);
			progressBar.anchoredPosition = new Vector2(-110, 60);
			progressBar.sizeDelta = new Vector2(100, 40);
			progressBar.localScale = Vector3.one;
			progressBar.anchoredPosition += new Vector2(progressBarThickness, progressBarThickness);
			progressBar.sizeDelta -= new Vector2(progressBarThickness, progressBarThickness) * 2;
			Image progressBarImg = progressBar.gameObject.AddComponent<Image>();
			progressBarImg.type = Image.Type.Filled;
			progressBarImg.fillMethod = Image.FillMethod.Horizontal;

			Text progressText = downloadProgressText = LevelField.MakeText(fieldUI);
			progressText.font = Plugin.gameFont;
			progressText.text = "0/0 MB";
			progressText.alignment = TextAnchor.MiddleCenter;
			progressText.resizeTextForBestFit = true;
			progressText.resizeTextMaxSize = 25;
			RectTransform progressRect = progressText.GetComponent<RectTransform>();
			progressRect.anchorMin = progressRect.anchorMax = new Vector2(1, 0);
			progressRect.sizeDelta = new Vector2(100, 50);
			progressRect.anchoredPosition = new Vector2(-10, 110);
			progressRect.pivot = new Vector2(1, 0);
			progressRect.localScale = Vector3.one;

			if (hierarchyHidden)
				currentUI.gameObject.SetActive(false);

			UpdateUI();
		}

		public void UpdateUI()
		{
			if (currentUI == null)
				return;

			progressBarBase.gameObject.SetActive(downloading);
			progressBar.gameObject.SetActive(downloading);
			cancelButton.gameObject.SetActive(downloading);
			downloadProgressText.gameObject.SetActive(downloading);

			if (!downloading)
			{
				AngryBundleContainer bundle = Plugin.GetAngryBundleByGuid(bundleGuid);
				if (bundle == null)
				{
					status = OnlineLevelStatus.notInstalled;
					installActive = true;
					installButton.gameObject.SetActive(true);
					updateButton.gameObject.SetActive(false);
				}
				else if (bundle.hash != bundleBuildHash)
				{
					status = OnlineLevelStatus.updateAvailable;
					installActive = false;
					installButton.gameObject.SetActive(false);
					updateButton.gameObject.SetActive(true);
				}
				else
				{
					status = OnlineLevelStatus.installed;
					installActive = false;
					installButton.gameObject.SetActive(false);
					updateButton.gameObject.SetActive(false);
				}
			}
			else
			{
				installActive = false;
				installButton.gameObject.SetActive(false);
				updateButton.gameObject.SetActive(false);
			}
		}

		public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
		{
			if (currentUI != null)
				currentUI.gameObject.SetActive(!hierarchyHidden);
		}
		
		public void StartDownload()
		{
			if (downloading || currentRequest != null)
				return;

			OnlineLevelsManager.instance.StartCoroutine(DownloadCoroutine());
		}

		public bool downloading = false;
		public UnityWebRequest currentRequest;
		public IEnumerator DownloadCoroutine()
		{
			if (downloading || currentRequest != null)
				yield break;

			downloading = true;
			try
			{
				string bundleUrl = $"https://raw.githubusercontent.com/eternalUnion/AngryLevels/release/Levels/{bundleGuid}/level.angry";
				string tempDownloadDir = Path.Combine(Plugin.workingDir, "TempDownloads");
				if (!Directory.Exists(tempDownloadDir))
					Directory.CreateDirectory(tempDownloadDir);
				string tempDownloadPath = Path.Combine(tempDownloadDir, $"{bundleGuid}.angry");

				UnityWebRequest req = currentRequest = new UnityWebRequest(bundleUrl);
				req.downloadHandler = new DownloadHandlerFile(tempDownloadPath);
				var handle = req.SendWebRequest();

				installActive = false;
				if (currentUI != null)
				{
					installButton.gameObject.SetActive(false);
					updateButton.gameObject.SetActive(false);
					cancelButton.gameObject.SetActive(true);
					progressBar.gameObject.SetActive(true);
					progressBarBase.gameObject.SetActive(true);
					downloadProgressText.gameObject.SetActive(true);
				}

				string fileMegabytes = (bundleFileSize / (float)(1024 * 1024)).ToString("0.0");
				float progressBarWidth = 100 - 2 * progressBarThickness;

				try
				{
					while (!handle.isDone)
					{
						if (currentUI != null)
						{
							progressBar.sizeDelta = new Vector2(progressBarWidth * handle.progress, progressBar.sizeDelta.y);
							string downloadedFileMegabytes = ((bundleFileSize * handle.progress) / (float)(1024 * 1024)).ToString("0.0");
							downloadProgressText.text = $"{downloadedFileMegabytes}/{fileMegabytes}\nMB";
						}

						yield return new WaitForSeconds(0.5f);
					}

					if (!req.isNetworkError && !req.isHttpError)
					{
						AngryBundleContainer bundle = Plugin.GetAngryBundleByGuid(bundleGuid);
						if (bundle == null)
						{
							string destinationFolder = Path.Combine(Plugin.workingDir, "Levels");
							if (!Directory.Exists(destinationFolder))
								Directory.CreateDirectory(destinationFolder);
							string destinationPath = Path.Combine(destinationFolder, IOUtils.GetUniqueFileName(destinationFolder, bundleName + ".angry"));
							File.Copy(tempDownloadPath, destinationPath, true);

							Plugin.ScanForLevels();
						}
						else
						{
							File.Copy(tempDownloadPath, bundle.pathToAngryBundle, true);
							bundle.UpdateScenes(false);
						}
					}

					if (File.Exists(tempDownloadPath))
						File.Delete(tempDownloadPath);
				}
				finally
				{
					req.Dispose();
				}
			}
			finally
			{
				downloading = false;
				currentRequest = null;
				UpdateUI();
			}
		}
	}
}
