using AngryLevelLoader.Containers;
using AngryLevelLoader.Fields;
using AngryLevelLoader.Managers;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace AngryLevelLoader.UserInterface
{
	internal static class AngryBundleList
	{
		internal static void SortBundles()
		{
			int i = 0;
			if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.Alphabetically)
			{
				foreach (var bundle in Plugin.GetAllBundleContainers().OrderBy(b => b.BundleName))
					bundle.SiblingIndex = i++;
			}
			else if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.Author)
			{
				foreach (var bundle in Plugin.GetAllBundleContainers().OrderBy(b => b.BundleAuthor))
					bundle.SiblingIndex = i++;
			}
			else if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.LastPlayed)
			{
				foreach (var bundle in Plugin.GetAllBundleContainers().OrderByDescending((b) => {
					if (LastPlayedMapManager.lastPlayed.TryGetValue(b.bundleGuid, out long time))
						return time;
					return 0;
				}))
				{
					bundle.SiblingIndex = i++;
				}
			}
			else if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.LastUpdate)
			{
				foreach (var bundle in Plugin.GetAllBundleContainers().OrderByDescending((b) => {
					if (LastPlayedMapManager.lastUpdate.TryGetValue(b.bundleGuid, out long time))
						return time;
					return 0;
				}))
				{
					bundle.SiblingIndex = i++;
				}
			}
		}

		#region Folder subsystem
		private static Dictionary<string, FolderButtonField> pathToFolderMap = new Dictionary<string, FolderButtonField>();
		private static Stack<FolderButtonField> folderStack = new Stack<FolderButtonField>();

		private static FolderButtonField GetFolder(string folder)
		{
			if (!pathToFolderMap.TryGetValue(folder, out FolderButtonField folderField))
			{
				folderField = new FolderButtonField(ConfigManager.folderDivision);
				folderField.folderName = Path.GetFileName(folder);
				folderField.onPressed.AddListener(() =>
				{
					DisplayFolder(folderField);
					folderStack.Push(folderField);
				});

				pathToFolderMap[folder] = folderField;

				string currentPath = folder;
				FolderButtonField currentFolder = folderField;
				while (currentPath != "/")
				{
					string parentPath = Path.GetDirectoryName(currentPath).Replace('\\', '/');
					bool parentFolderExisted = true;

					if (!pathToFolderMap.TryGetValue(parentPath, out FolderButtonField parentFolder))
					{
						parentFolderExisted = false;

						parentFolder = new FolderButtonField(ConfigManager.folderDivision);
						parentFolder.folderName = Path.GetFileName(parentPath);
						parentFolder.onPressed.AddListener(() =>
						{
							DisplayFolder(parentFolder);
							folderStack.Push(parentFolder);
						});

						pathToFolderMap[parentPath] = parentFolder;
					}

					parentFolder.folders.Add(currentFolder);
					if (parentFolderExisted)
						break;

					currentFolder = parentFolder;
					currentPath = parentPath;
				}
			}

			return folderField;
		}

		private static void DisplayFolder(FolderButtonField folderField)
		{
			if (folderField == null)
				folderField = pathToFolderMap["/"];

			foreach (var bundle in Plugin.GetAllBundleContainers())
				bundle.Hidden = true;
			foreach (var folder in pathToFolderMap.Values)
				folder.hidden = true;

			foreach (var bundle in folderField.bundles)
				if (bundle.LazyLoaded)
					bundle.Hidden = false;

			foreach (var folder in folderField.folders)
			{
				if (folder.Any())
					folder.hidden = false;
			}

			if (folderField == pathToFolderMap["/"])
				ConfigManager.levelBundlesHeader.text = "Level Bundles";
			else
				ConfigManager.levelBundlesHeader.text = $"Level Bundles <color=grey>{pathToFolderMap.Where(e => e.Value == folderField).FirstOrDefault().Key ?? "<unknown folder>"}</color>";
		}
		
		internal static void OpenFolder(string folderPath)
		{
			List<FolderButtonField> folders = new List<FolderButtonField>();
			FolderButtonField rootFolder = pathToFolderMap["/"];

			string currentPath = folderPath;
			while (currentPath != "/")
			{
				folders.Add(GetFolder(folderPath));
				currentPath = Path.GetDirectoryName(currentPath).Replace('\\', '/');
			}
			folders.Add(rootFolder);
			folders.Reverse();

			folderStack.Clear();
			foreach (FolderButtonField folder in folders)
				folderStack.Push(folder);

			DisplayFolder(folderStack.Peek());
		}
		
		internal static bool PopFolder()
		{
			if (folderStack.Count <= 1)
				return false;

			folderStack.Pop();
			DisplayFolder(folderStack.Peek());
			return true;
		}

		internal static void ResetFolders()
		{
			foreach (FolderButtonField folder in pathToFolderMap.Values)
			{
				folder.bundles.Clear();
			}
		}

		internal static void AddBundle(BundleContainer bundle, string folderPath)
		{
			GetFolder(folderPath).bundles.Add(bundle);
		}

		private static string[] validImageExts = new string[]
		{
			".jpg", ".jpeg", ".png"
		};

		internal static void UpdateFolderIcons()
		{
			foreach (KeyValuePair<string, FolderButtonField> folder in pathToFolderMap)
			{
				if (folder.Value == pathToFolderMap["/"])
					continue;

				string realPath = Path.Combine(Plugin.levelsPath, folder.Key.Substring(1));
				if (Directory.Exists(realPath))
				{
					string pathToIcon = Directory.GetFiles(realPath).Where(path => validImageExts.Contains(Path.GetExtension(path))).FirstOrDefault();
					if (!string.IsNullOrEmpty(pathToIcon))
					{
						folder.Value.CreateIcon(pathToIcon);
						continue;
					}
				}

				folder.Value.CreateIcon(folder.Value);
			}
		}
		#endregion



		#region Search subsystem
		private static string[] currentSearchKeywords = new string[0];
		private static char[] whitespaceSeparator = new char[] { ' ' };

		private static void UpdateBundleSearch(string newVal)
		{
			string[] newKeywords = newVal.Split(whitespaceSeparator, StringSplitOptions.RemoveEmptyEntries).Select(keyword => keyword.ToLower()).ToArray();
			if (newKeywords.Length == currentSearchKeywords.Length && newKeywords.SequenceEqual(currentSearchKeywords))
				return;

			currentSearchKeywords = newKeywords;

			// If not searching anything, open the current folder
			if (currentSearchKeywords.Length == 0)
			{
				ConfigManager.folderDivision.hidden = false;
				ConfigManager.searchInfo.hidden = true;

				foreach (BundleContainer bundle in Plugin.GetAllBundleContainers())
					bundle.SearchKeywords = new string[0];

				DisplayFolder(folderStack.Peek());
				return;
			}

			ConfigManager.folderDivision.hidden = true;
			ConfigManager.searchInfo.hidden = false;
			int filterCount = 0, totalCount = 0;
			foreach (BundleContainer bundle in Plugin.GetAllBundleContainers())
			{
				bundle.SearchKeywords = currentSearchKeywords;

				if (!bundle.HasValidAngryFile)
					continue;

				totalCount += 1;
				if (bundle.SearchMatch)
					filterCount += 1;
			}

			ConfigManager.levelBundlesHeader.text = "Level Bundles";
			ConfigManager.searchInfo.text = $"Showing {filterCount} of {totalCount} bundles";
		}
		#endregion

		internal static void Init()
		{
			if (!pathToFolderMap.TryGetValue("/", out FolderButtonField rootFolder))
			{
				rootFolder = new FolderButtonField(ConfigManager.config.rootPanel);
				rootFolder.hidden = true;
				pathToFolderMap["/"] = rootFolder;
			}

			ConfigManager.InitializeConfig();

			ConfigManager.config.rootPanel.onPannelOpenEvent += (externally) =>
			{
				Button.ButtonClickedEvent backButtonEvent = PluginConfiguratorController.backButton.onClick;

				PluginConfiguratorController.backButton.onClick = new Button.ButtonClickedEvent();
				PluginConfiguratorController.backButton.onClick.AddListener(() =>
				{
					if (folderStack.Count <= 1 || !string.IsNullOrEmpty(ConfigManager.searchBar.value))
					{
						backButtonEvent.Invoke();
					}
					else
					{
						folderStack.Pop();

						if (folderStack.Count != 0)
							DisplayFolder(folderStack.Peek());
						else
							DisplayFolder(null);
					}
				});
			};

			ConfigManager.searchBar.onValueChange += UpdateBundleSearch;
			ConfigManager.searchBar.onReset += () => ConfigManager.searchBar.value = "";
			ConfigManager.searchBar.onEndEdit += (bool wasCanceled) =>
			{
				if (!wasCanceled)
					return;

				if (!string.IsNullOrWhiteSpace(ConfigManager.searchBar.value))
				{
					ConfigManager.searchBar.value = "";
					return;
				}

				if (folderStack.Count > 1)
					return;

				ConfigManager.config.rootPanel.ClosePanel();
			};
		}
	}
}
