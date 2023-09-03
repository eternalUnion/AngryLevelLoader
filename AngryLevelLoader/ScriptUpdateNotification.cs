using PluginConfig;
using RudeLevelScript;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public class ScriptUpdateNotification : NotificationPanel.Notification
	{
		private List<ScriptUpdateProgressField> fields = new List<ScriptUpdateProgressField>();

		class ScriptUpdateProgressField
		{
			public string scriptName;
			public string fileSizeText = "";
			public ScriptUpdateNotification caller;

			public enum ScriptStatus
			{
				Download,
				NotFound,
				Update
			}

			public ScriptStatus scriptStatus;

			public bool downloaded = false;
			public bool downloadError = false;

			public Text currentTextComp;
			public void SetStatusText()
			{
				if (currentTextComp == null)
					return;

				string currentText = scriptName + '\n';

				if (downloading)
				{
					currentText += $"Downloading... {(int)(currentDllRequest.downloadProgress * 100)} %";
				}
				else
				{
					if (downloaded)
					{
						if (Plugin.ScriptLoaded(scriptName))
							currentText += "<color=red>RESTART REQUIRED</color>";
						else
							currentText += "<color=lime>Installed!</color>";
					}
					else
					{
						if (scriptStatus == ScriptStatus.NotFound)
							currentText += "<color=red>Not Available Online</color>";
						else if (scriptStatus == ScriptStatus.Update)
							currentText += $"<color=cyan>Update Available</color> ({fileSizeText})";
						else if (scriptStatus == ScriptStatus.Download)
							currentText += $"<color=orange>Available online</color> ({fileSizeText})";

						if (downloadError)
							currentText += "\n<color=red>Download error</color>";
					}
				}

				currentTextComp.text = currentText;
			}

			public void OnUI(RectTransform content)
			{
				RectTransform container = UIUtils.MakeSimpleRect(content);
				container.anchorMin = new Vector2(0, 1);
				container.anchorMax = new Vector2(0, 1);
				container.sizeDelta = new Vector2(600, 80);
				container.pivot = new Vector2(0.5f, 1);
				container.anchoredPosition = new Vector2(0, 0);
				container.gameObject.AddComponent<Image>().color = Color.black;

				RectTransform statusText = UIUtils.MakeText(container, "", 16, TextAnchor.UpperLeft);
				statusText.anchorMin = new Vector2(0, 0.5f);
				statusText.anchorMax = new Vector2(0, 0.5f);
				statusText.sizeDelta = new Vector2(580, 30);
				statusText.pivot = new Vector2(0, 0.5f);
				statusText.anchoredPosition = new Vector2(10, 0);
				currentTextComp = statusText.GetComponent<Text>();

				if (string.IsNullOrEmpty(fileSizeText))
				{
					if (ScriptCatalogLoader.TryGetScriptInfo(scriptName, out ScriptInfo info))
					{
						string prefix = "B";
						float size = info.Size;
						if (size >= 1024)
						{
							size /= 1024;
							prefix = "KB";
						}
						if (size >= 1024)
						{
							size /= 1024;
							prefix = "MB";
						}

						fileSizeText = $"{size.ToString("0.0")} {prefix}";
					}
					else
					{
						fileSizeText = "? MB";
					}
				}
				SetStatusText();
			}
		
			public bool downloading { get; private set; }

			public void StartDownload()
			{
				if (downloading || downloaded || scriptStatus == ScriptStatus.NotFound)
					return;

				downloading = true;
				OnlineLevelsManager.instance.StartCoroutine(DownloadTask());
			}

			UnityWebRequest currentDllRequest;
			UnityWebRequest currentCertRequest;
			private IEnumerator DownloadTask()
			{
				downloading = true;
				downloadError = false;

				try
				{
					currentDllRequest = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevels, $"Scripts/{scriptName}"));
					currentCertRequest = new UnityWebRequest(OnlineLevelsManager.GetGithubURL(OnlineLevelsManager.Repo.AngryLevels, $"Scripts/{scriptName}.cert"));

					string tempPath = Path.Combine(Plugin.workingDir, "TempDownloads");
					if (!Directory.Exists(tempPath))
						Directory.CreateDirectory(tempPath);

					string tempDllPath = Path.Combine(tempPath, scriptName);
					string tempCertPath = Path.Combine(tempPath, scriptName + ".cert");

					currentDllRequest.downloadHandler = new DownloadHandlerFile(tempDllPath);
					currentCertRequest.downloadHandler = new DownloadHandlerFile(tempCertPath);

					currentDllRequest.SendWebRequest();
					currentCertRequest.SendWebRequest();

					while (true)
					{
						if (currentDllRequest.isDone && currentCertRequest.isDone)
							break;
						
						SetStatusText();

						yield return new WaitForSecondsRealtime(0.5f);
					}

					if (currentDllRequest.isNetworkError || currentDllRequest.isHttpError
						|| currentCertRequest.isNetworkError || currentCertRequest.isHttpError)
					{
						downloadError = true;
					}
					else
					{
						downloaded = true;

						File.Copy(tempDllPath, Path.Combine(Plugin.workingDir, "Scripts", scriptName), true);
						File.Copy(tempCertPath, Path.Combine(Plugin.workingDir, "Scripts", scriptName + ".cert"), true);
					}

					if (File.Exists(tempDllPath))
						File.Delete(tempDllPath);
					if (File.Exists(tempCertPath))
						File.Delete(tempCertPath);

					currentDllRequest.Dispose();
					currentCertRequest.Dispose();
				}
				finally
				{
					downloading = false;
					currentDllRequest = null;
					currentCertRequest = null;

					if (caller != null)
						caller.CheckContinueButtonInteractable();

					SetStatusText();
				}
			}

			public void StopDownload()
			{
				if (!downloading)
					return;

				if (currentDllRequest != null)
					currentDllRequest.Abort();
				if (currentCertRequest != null)
					currentCertRequest.Abort();
			}

			public bool isDone
			{
				get
				{
					return downloaded || scriptStatus == ScriptStatus.NotFound;
				}
			}
		}

		public List<string> scripts;
		public AngryBundleContainer bundleContainer;
		public LevelContainer levelContainer;
		public RudeLevelData levelData;
		public string levelName;

		public ScriptUpdateNotification(IEnumerable<string> scriptsToDownload, List<string> scripts, AngryBundleContainer bundleContainer, LevelContainer levelContainer, RudeLevelData levelData, string levelName)
		{
			this.scripts = scripts;
			this.bundleContainer = bundleContainer;
			this.levelContainer = levelContainer;
			this.levelData = levelData;
			this.levelName = levelName;

			if (scriptsToDownload != null)
			{
				foreach (string script in scriptsToDownload)
				{
					ScriptUpdateProgressField field = new ScriptUpdateProgressField();
					field.scriptName = script;
					field.caller = this;
					if (ScriptCatalogLoader.TryGetScriptInfo(script, out var scriptInfo))
					{
						if (Plugin.ScriptExists(script))
							field.scriptStatus = ScriptUpdateProgressField.ScriptStatus.Update;
						else
							field.scriptStatus = ScriptUpdateProgressField.ScriptStatus.Download;
					}
					else
						field.scriptStatus = ScriptUpdateProgressField.ScriptStatus.NotFound;
					
					fields.Add(field);
				}
			}
		}

		private Button currentContinueButton;
		public void CheckContinueButtonInteractable()
		{
			if (currentContinueButton == null)
				return;

			bool interactable = true;
			foreach (var field in fields)
			{
				if (field.downloading)
				{
					interactable = false;
					break;
				}
			}

			currentContinueButton.interactable = interactable;
		}

		public override void OnUI(RectTransform panel)
		{
			RectTransform header = UIUtils.MakeText(panel, "Missing Or Outdated Scripts", 30, TextAnchor.UpperCenter);
			header.anchorMin = new Vector2(0, 1);
			header.anchorMax = new Vector2(1, 1);
			header.sizeDelta = new Vector2(0, 70);
			header.pivot = new Vector2(0.5f, 1);
			header.anchoredPosition = new Vector2(0, -50);

			RectTransform scriptsPanel = UIUtils.MakePanel(panel, 5);
			
			foreach (var field in fields)
				field.OnUI(scriptsPanel);

			LayoutRebuilder.ForceRebuildLayoutImmediate(scriptsPanel);

			RectTransform cancelButton = UIUtils.MakeButton(panel, "Cancel");
			cancelButton.anchorMin = new Vector2(0.5f, 0);
			cancelButton.anchorMax = new Vector2(0.5f, 0);
			cancelButton.pivot = new Vector2(1, 0);
			cancelButton.anchoredPosition = new Vector2(-5, 10);
			cancelButton.sizeDelta = new Vector2(295, 60);
			Button cancel = cancelButton.GetComponent<Button>();
			cancel.onClick.AddListener(() =>
			{
				foreach (var field in fields)
					field.StopDownload();

				Close();
			});

			RectTransform updateButton = UIUtils.MakeButton(panel, "Update");
			updateButton.anchorMin = new Vector2(0.5f, 0);
			updateButton.anchorMax = new Vector2(0.5f, 0);
			updateButton.pivot = new Vector2(0, 0);
			updateButton.anchoredPosition = new Vector2(5, 10);
			updateButton.sizeDelta = new Vector2(295, 60);
			Button update = updateButton.GetComponent<Button>();
			update.onClick.AddListener(() =>
			{
				foreach (var field in fields)
					field.StartDownload();

				if (currentContinueButton != null)
					currentContinueButton.interactable = false;
			});

			RectTransform continueButton = UIUtils.MakeButton(panel, "Continue");
			continueButton.anchorMin = new Vector2(0.5f, 0);
			continueButton.anchorMax = new Vector2(0.5f, 0);
			continueButton.pivot = new Vector2(0.5f, 0);
			continueButton.anchoredPosition = new Vector2(0, 80);
			continueButton.sizeDelta = new Vector2(600, 60);
			Button cont = currentContinueButton = continueButton.GetComponent<Button>();
			foreach (var field in fields)
			{
				if (field.downloading)
				{
					cont.interactable = false;
					break;
				}
			}
			cont.onClick.AddListener(() =>
			{
				Close();
				AngrySceneManager.LoadLevelWithScripts(scripts, bundleContainer, levelContainer, levelData, levelName);
			});
		}
	
		public static void Test()
		{
			NotificationPanel.Open(new ScriptUpdateNotification(new List<string>() { "eternalUnion.PhysicsExtensions.dll", "playerStats.dll" }, null, null, null, null, ""));
		}
	}
}
