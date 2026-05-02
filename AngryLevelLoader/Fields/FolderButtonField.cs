using AngryLevelLoader.Containers;
using AngryLevelLoader.Managers;
using AngryLevelLoader.UserInterface;
using AngryUiComponents;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

namespace AngryLevelLoader.Fields
{
	internal class FolderButtonField : CustomConfigField, IEnumerable<BundleContainer>
	{
		private IEnumerator<BundleContainer> FolderEnumerator(FolderButtonField folder)
		{
			foreach (BundleContainer bundle in folder.bundles)
				yield return bundle;

			foreach (FolderButtonField subfolder in folder.folders)
			{
				IEnumerator<BundleContainer> subfolderEnumerator = FolderEnumerator(subfolder);
				while (subfolderEnumerator.MoveNext())
					yield return subfolderEnumerator.Current;
			}
		}

		internal readonly string folderName;
		internal readonly FolderButtonField parent;
		private readonly List<FolderButtonField> folders = new List<FolderButtonField>();
		
		internal readonly List<BundleContainer> bundles = new List<BundleContainer>();

		internal IEnumerable<FolderButtonField> GetAllSubfolders()
		{
			return folders;
		}

		internal FolderButtonField GetOrCreateFolder(string subfolderName)
		{
			FolderButtonField subFolder = folders.FirstOrDefault(f => f.folderName == subfolderName);
			if (subFolder == null)
			{
				subFolder = new FolderButtonField(subfolderName, this);
				folders.Add(subFolder);
			}

			return subFolder;
		}

		public string GetRelativeFolderPath()
		{
			if (parent == null)
				return "/";

			StringBuilder sb = new StringBuilder();
			FolderButtonField current = this;

			while (current != null && current.parent != null)
			{
				sb.Insert(0, '/');
				sb.Insert(1, current.folderName);
				current = current.parent;
			}

			return sb.ToString();
		}

		public string GetAbsoluteFolderPath(string prefixPath)
		{
			List<FolderButtonField> folders = new List<FolderButtonField>();
			FolderButtonField currentFolder = this;

			while (currentFolder != null)
			{
				if (currentFolder == AngryBundleList.rootFolder)
					break;

				folders.Add(currentFolder);
				currentFolder = currentFolder.parent;
			}

			folders.Reverse();

			string absolutePath = prefixPath;
			foreach (FolderButtonField folder in folders)
			{
				absolutePath = Path.Combine(absolutePath, folder.folderName);
			}

			return absolutePath;
		}

		private const string ASSET_PATH = "AngryLevelLoader/Fields/FolderButtonField.prefab";
		private const int ICON_SIZE = 128;
		private const int ICON_GAP = 8;

		private static RenderTexture rt = new RenderTexture(ICON_SIZE, ICON_SIZE, 24);

		public RectTransform currentContainer = null;
		private AngryFolderButtonComponent currentUi;

		private static Texture2D blankIcon = null;
		private Texture2D icon = null;
		public void CreateIcon(IEnumerable<BundleContainer> bundles)
		{
			if (blankIcon == null)
			{
				blankIcon = new Texture2D(ICON_SIZE * 2 + ICON_GAP, ICON_SIZE * 2 + ICON_GAP);
				blankIcon.filterMode = FilterMode.Point;
				for (int x = 0; x < blankIcon.width; x++)
					for (int y = 0; y < blankIcon.height; y++)
						blankIcon.SetPixel(x, y, Color.black);
				blankIcon.Apply();
			}

			if (icon != null)
				UnityEngine.Object.Destroy(icon);
			icon = UnityEngine.Object.Instantiate(blankIcon);

			List<Texture2D> icons = new List<Texture2D>();
			foreach (var bundle in bundles)
			{
				string iconPath = Path.Combine(Plugin.tempFolderPath, bundle.bundleGuid, "icon.png");
				if (!File.Exists(iconPath))
					continue;

				Texture2D smallIcon = new Texture2D(1, 1);
				smallIcon.LoadImage(File.ReadAllBytes(iconPath));
				icons.Add(smallIcon);

				if (icons.Count == 4)
					break;
			}

			Stack<(int x, int y)> iconPositions = new Stack<(int x, int y)>();
			iconPositions.Push((ICON_SIZE + ICON_GAP, 0));
			iconPositions.Push((0, 0));
			iconPositions.Push((ICON_SIZE + ICON_GAP, ICON_SIZE + ICON_GAP));
			iconPositions.Push((0, ICON_SIZE + ICON_GAP));

			RenderTexture previousRt = RenderTexture.active;
			RenderTexture.active = rt;

			foreach (var smallIcon in icons)
			{
				(int x, int y) = iconPositions.Pop();

				Graphics.Blit(smallIcon, rt);
				icon.ReadPixels(new Rect(0, 0, ICON_SIZE, ICON_SIZE), x, y);

				UnityEngine.Object.Destroy(smallIcon);
			}

			RenderTexture.active = previousRt;
			icon.Apply();

			if (currentUi != null)
				currentUi.folderIcon.texture = icon;
		}

		public void CreateIcon(string imagePath)
		{
			if (icon != null)
				UnityEngine.Object.Destroy(icon);

			icon = new Texture2D(2, 2);
			icon.LoadImage(File.ReadAllBytes(imagePath));

			if (currentUi != null)
				currentUi.folderIcon.texture = icon;
		}

		public UnityEvent onPressed = new UnityEvent();

		private readonly bool _inited = false;
		public FolderButtonField(string folderName, FolderButtonField parent) : base(ConfigManager.folderDivision)
		{
			_inited = true;
			this.folderName = folderName;
			this.parent = parent;

			onPressed.AddListener(() =>
			{
				AngryBundleList.DisplayFolder(this);
			});

			if (currentContainer != null)
				OnCreateUI(currentContainer);

			CreateIcon(Enumerable.Empty<BundleContainer>());
		}

		public override void OnCreateUI(RectTransform fieldUI)
		{
			currentContainer = fieldUI;
			if (!_inited)
				return;

			GameObject ui = Addressables.InstantiateAsync(ASSET_PATH, fieldUI).WaitForCompletion();
			currentUi = ui.GetComponent<AngryFolderButtonComponent>();

			RectTransform currentUiRect = ui.GetComponent<RectTransform>();
			fieldUI.sizeDelta = currentUiRect.sizeDelta;
			currentUiRect.pivot = new Vector2(0, 1);
			currentUiRect.anchorMin = new Vector2(0, 1);
			currentUiRect.anchorMax = new Vector2(0, 1);
			currentUiRect.anchoredPosition = new Vector2(0, 0);

			currentUi.folderName.text = folderName;
			currentUi.folderButton.onClick.AddListener(() => onPressed.Invoke());
			currentUi.folderIcon.texture = icon;

			fieldUI.gameObject.SetActive(!hidden && !hierarchyHidden);
			currentUi.folderButton.interactable = interactable && hierarchyInteractable;
		}

		public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
		{
			if (currentUi != null)
				currentContainer.gameObject.SetActive(!selfHidden && !hierarchyHidden);
		}

		public override void OnInteractableChange(bool selfInteractable, bool hierarchyInteractable)
		{
			if (currentUi != null)
				currentUi.folderButton.interactable = selfInteractable && hierarchyInteractable;
		}

		public IEnumerator<BundleContainer> GetEnumerator()
		{
			return FolderEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
