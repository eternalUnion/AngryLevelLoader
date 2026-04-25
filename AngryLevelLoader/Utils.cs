using AngryLevelLoader.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public static class RankUtils
	{
		public static int GetRankScore(char rank)
		{
			if (rank == 'D')
				return 1;
			if (rank == 'C')
				return 2;
			if (rank == 'B')
				return 3;
			if (rank == 'A')
				return 4;
			if (rank == 'S')
				return 5;
			if (rank == 'P')
				return 6;
			if (rank == '-')
				return -1;
			if (rank == ' ')
				return 0;

			return -1;
		}

		public static char GetRankChar(int score)
		{
            if (score == 1)
                return 'D';
            if (score == 2)
                return 'C';
            if (score == 3)
                return 'B';
            if (score == 4)
                return 'A';
            if (score == 5)
                return 'S';
            if (score == 6)
                return 'P';
            if (score == -1)
                return '-';
            if (score == 0)
                return ' ';

            return '-';
        }

		private static Dictionary<char, Color> rankColors = new Dictionary<char, Color>()
		{
			{ 'D', new Color(0, 0x94 / 255f, 0xFF / 255f) },
			{ 'C', new Color(0x4C / 255f, 0xFF / 255f, 0) },
			{ 'B', new Color(0xFF / 255f, 0xD8 / 255f, 0) },
			{ 'A', new Color(0xFF / 255f, 0x6A / 255f, 0) },
			{ 'S', Color.red },
			{ 'P', Color.white },
			{ '-', Color.gray },
			{ ' ', Color.gray },
		};

		public static Color GetRankColor(char rank, Color fallback)
		{
			if (rankColors.TryGetValue(rank, out var color))
				return color;
			return fallback;
		}

		public static string GetFormattedRankText(char rank)
		{
			Color textColor;
			if (!rankColors.TryGetValue(rank, out textColor))
				textColor = Color.gray;

			return $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{rank}</color>";
		}
	}

	public static class IOUtils
	{
		/// <summary>
		/// Returns a unique file name in the given folder by appending `_num` to file name if the file already exists
		/// </summary>
		/// <param name="folder">Folder path</param>
		/// <param name="name">File name with the extension</param>
		/// <returns>File name which does not exist in the folder</returns>
		public static string GetUniqueFileName(string folder, string name)
		{
			string nameExtensionless = Path.GetFileNameWithoutExtension(name);
			string newName = nameExtensionless;
			string ext = Path.GetExtension(name);

			int i = 0;
			while (File.Exists(Path.Combine(folder, $"{newName}{ext}")))
				newName = $"{nameExtensionless}_{i++}";

			return $"{newName}{ext}";
		}

        /// <summary>
        /// Returns a full path to a unique file in the given folder by appending `_num` to file name if the file already exists
        /// </summary>
        /// <param name="path">Full path to the file</param>
        /// <returns>Full path to a file which does not exist in the directory</returns>
        public static string GetUniqueFileName(string path)
		{
			return Path.Combine(Path.GetDirectoryName(path), GetUniqueFileName(Path.GetDirectoryName(path), Path.GetFileName(path)));
		}

		public static string GetPathSafeName(string name)
		{
			StringBuilder newName = new StringBuilder();
			for (int i = 0; i < name.Length; i++)
			{
                char c = name[i];

                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
				{
					newName.Append(c);
				}
				else if (c == ' ')
				{
					if (i > 0 && name[i - 1] != ' ')
						newName.Append('_');
				}
			}

			string result = newName.ToString();
			if (string.IsNullOrEmpty(result))
				return "file";
			return result;
		}

		private static string _AppData = null;
        public static string AppData
		{
			get
			{
				if (_AppData != null)
					return _AppData;

				_AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

				Regex badAppData = new Regex(@"^[^:]+:\\Users\\User\\AppData\\Roaming");
				if (badAppData.IsMatch(_AppData))
				{
					Plugin.logger.LogWarning($"Bad username for AppData (got '{_AppData}', expected user '{Environment.UserName}'), falling back to local AppData");

					_AppData = Path.Combine(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).FullName, "Roaming");
					if (badAppData.IsMatch(_AppData))
					{
						Plugin.logger.LogWarning($"Bad username for local AppData (got '{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}', expected user '{Environment.UserName}'), falling back to absolute path");

						char driveLetter = (_AppData.Length > 0) ? _AppData[0] : 'C';
						_AppData = @$"{driveLetter}:\Users\{Environment.UserName}\AppData\Roaming";
					}
				}

				return _AppData;
			}
        }

		public static bool TryCreateDirectory(string path)
		{
			if (Directory.Exists(path))
				return false;
			Directory.CreateDirectory(path);
			return true;
		}

		public static bool TryCreateDirectoryForFile(string path)
		{
			return TryCreateDirectory(Path.GetDirectoryName(path));
		}

		// Taken from msdn
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool deleteSource)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }


            // Get the file contents of the directory to copy.
            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = Path.Combine(destDirName, file.Name);

                // Copy the file.
				if (deleteSource)
					file.MoveTo(temppath);
                else
					file.CopyTo(temppath, false);
            }

            // If copySubDirs is true, copy the subdirectories.
            if (copySubDirs)
            {

                foreach (DirectoryInfo subdir in dirs)
                {
                    // Create the subdirectory.
                    string temppath = Path.Combine(destDirName, subdir.Name);

                    // Copy the subdirectories.
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs, deleteSource);
                }
            }
        }

		public static bool PathEquals(string path1, string path2)
		{
			if (string.IsNullOrEmpty(path1))
			{
				return string.IsNullOrEmpty(path2);
			}
			else if (string.IsNullOrEmpty(path2))
			{
				return false;
			}

			return Path.GetFullPath(path1) == Path.GetFullPath(path2);
		}

		public struct SubFileInfo
		{
			public string filePath;
			public string subFolder;

			public SubFileInfo(string fileName, string subFolder)
			{
				this.filePath = fileName;
				this.subFolder = subFolder;
			}
		}

		public static IEnumerable<SubFileInfo> GetAllFilesRecursive(string folderPath, string currentSubFolder = "/")
		{
			foreach (string filePath in Directory.GetFiles(folderPath))
				yield return new SubFileInfo(filePath, currentSubFolder);

			foreach (string subFolderPath in Directory.GetDirectories(folderPath))
			{
				string subFolderName = Path.GetFileName(subFolderPath);
				foreach (SubFileInfo subFile in GetAllFilesRecursive(subFolderPath, currentSubFolder == "/" ? "/" + subFolderName : $"{currentSubFolder}/{subFolderName}"))
					yield return subFile;
			}
		}
	}

	public static class UIUtils
	{
        public static void AddMouseEvents(GameObject field, Button btn, Action<BaseEventData> mouseOnEvent, Action<BaseEventData> mouseOffEvent)
        {
            EventTrigger trigger = field.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = field.AddComponent<EventTrigger>();
                PluginConfig.API.Utils.AddScrollEvents(trigger, PluginConfig.API.Utils.GetComponentInParent<ScrollRect>(btn.transform));
            }

            EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
            mouseOn.callback.AddListener(e => mouseOnEvent(e));
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener(e => mouseOffEvent(e));
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);
        }
    }

	public static class CryptographyUtils
	{
		public static string AdminPrivateKey => Environment.GetEnvironmentVariable("ANGRY_ADMIN_KEY");

		public static byte[] Encrypt(string data, string keyXml)
		{
			RSA rsa = RSA.Create();
			rsa.FromXmlString(keyXml);
			return rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.Pkcs1);
		}

		public static byte[] Encrypt(byte[] data, string keyXml)
		{
			RSA rsa = RSA.Create();
			rsa.FromXmlString(keyXml);
			return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
		}

		public static bool VerifyFileCertificate(string filePath, string certificatePath)
		{
			const string angryPublicKey = "<RSAKeyValue><Modulus>+/ueeOpso05dA+5GjKbjQ0VpM+JAHmRRgYRw36G4dXqmpCGfVDNVdjjBBkVWO+6lJoSNaaG4Yprn4uQVslUQ7OYWAw6Y+9E0Ezvr1quWE7i0KGxG6weplRTsu9aO0/9gJgP/gWQxC0Cf83NwyvMPsThtCruAQFT+cW0LGghtFgrBr++aknI06SJI5ydrbZgEtU5i4FfjrV1ms4CRRojhydJglfGQfG8W3pTDge4jVdND+RGB6F01QGi0+Bnq5DfKdjvb3/Zh1ko7WocWgavDaIgLYj88AgbGdC0lidLMIgzdnGxkLyxbTzsgi/mvUpB2foy4uHoV22EaWMj+6H+oXQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
			RSA publicKey = RSA.Create();
			publicKey.FromXmlString(angryPublicKey);
			byte[] cert = File.ReadAllBytes(certificatePath);
			return publicKey.VerifyData(File.ReadAllBytes(filePath), cert, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}

		// Why don't you exist Convert.ToHexString(byte[])? Why nothing exists in this framework?
		private static string ByteArrayToString(byte[] ba)
		{
			return BitConverter.ToString(ba).Replace("-", "");
		}

		public static string GetSHA256Hash(string filePath)
		{
			SHA256 hash = SHA256.Create();
			hash.Initialize();
			return ByteArrayToString(hash.ComputeHash(File.ReadAllBytes(filePath))).ToLower();
		}

		public static string GetMD5String(string text)
		{
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
			return ByteArrayToString(hash).ToLower();
		}

		public static string GetMD5String(byte[] data)
		{
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(data);
			return ByteArrayToString(hash).ToLower();
		}
	}

	public static class AngryFileUtils
	{
        public static bool TryGetAngryBundleData(string filePath, out AngryBundleData data, out Exception error)
		{
			error = null;
			data = null;

			try
			{
				data = GetAngryBundleData(filePath);
				return true;
            }
			catch (Exception e)
			{
				error = e;
			}

			return false;
		}
		
		public static AngryBundleData GetAngryBundleData(string filePath)
		{
			using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
			{
				using (ZipArchive zip = new ZipArchive(fs))
				{
					var entry = zip.GetEntry("data.json");
					if (entry == null)
						return null;

					using (StreamReader dataReader = new StreamReader(entry.Open()))
						return JsonConvert.DeserializeObject<AngryBundleData>(dataReader.ReadToEnd());
				}
			}
        }

        public static bool IsV1LegacyFile(string pathToAngryBundle)
        {
            using (FileStream fs = File.Open(pathToAngryBundle, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    BinaryReader reader = new BinaryReader(fs);
                    fs.Seek(0, SeekOrigin.Begin);
                    int bundleCount = reader.ReadInt32();
                    if (bundleCount * 4 + 4 >= fs.Length)
                        return false;
                    int totalSize = 4 + bundleCount * 4;
                    for (int i = 0; i < bundleCount && totalSize < fs.Length; i++)
                        totalSize += reader.ReadInt32();

                    if (totalSize == fs.Length)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }
    }

	// Helper classes for creation of singleton tasks. If a task is running, the currently
	// running task is returned instead of creating a new task. This is useful for tasks
	// concerned with IO operations.

	public class CachedTask
	{
		private Func<Task> task;

		public CachedTask(Func<Task> task)
		{
			this.task = task;
		}

		private Task currentTask = null;
		public bool Running => currentTask != null && !currentTask.IsCompleted;

		public async Task GetTask(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!Running)
			{
				currentTask = task();
			}

			await Task.Run(async () =>
			{
				await currentTask;
			}, cancellationToken);
		}
	}

	public class CachedTask<T>
	{
		private Func<Task<T>> task;

		public CachedTask(Func<Task<T>> task)
		{
			this.task = task;
		}

		private Task<T> currentTask = null;
		public bool Running => currentTask != null && !currentTask.IsCompleted;

		public async Task<T> GetTask(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!Running)
			{
				currentTask = task();
			}

			return await Task.Run(async () =>
			{
				return await currentTask;
			}, cancellationToken);
		}
	}

	public static class AsyncExtensions
	{
		public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
		{
			/*if (asyncOp.isDone)
			{
				var instantReturn = new TaskCompletionSource<object>();
				instantReturn.SetResult(null);
				return ((Task)instantReturn.Task).GetAwaiter();
			}*/

			var tcs = new TaskCompletionSource<object>();
			asyncOp.completed += obj => { tcs.SetResult(null); };
			return ((Task)tcs.Task).GetAwaiter();
		}

		public static TaskAwaiter GetAwaiter<T>(this AsyncOperationHandle<T> asyncOp)
		{
			/*if (asyncOp.IsDone)
			{
				var instantReturn = new TaskCompletionSource<object>();
				instantReturn.SetResult(null);
				return ((Task)instantReturn.Task).GetAwaiter();
			}*/

			var tcs = new TaskCompletionSource<object>();
			asyncOp.Completed += obj => { tcs.SetResult(null); };
			return ((Task)tcs.Task).GetAwaiter();
		}

		public static TaskAwaiter GetAwaiter(this AsyncOperationHandle asyncOp)
		{
			/*if (asyncOp.IsDone)
			{
				var instantReturn = new TaskCompletionSource<object>();
				instantReturn.SetResult(null);
				return ((Task)instantReturn.Task).GetAwaiter();
			}*/

			var tcs = new TaskCompletionSource<object>();
			asyncOp.Completed += obj => { tcs.SetResult(null); };
			return ((Task)tcs.Task).GetAwaiter();
		}

		public static async Task TEST_InstantReturn()
		{
			var task = new UnityWebRequest(@"file://C:\Users\ROG\Downloads\CarcassEnemy.dll.cert");
			task.downloadHandler = new DownloadHandlerBuffer();
			var handle = task.SendWebRequest();

			Plugin.logger.LogInfo("First await start");
			await handle;
			Plugin.logger.LogInfo("First await end");

			Plugin.logger.LogInfo("Second await start");
			await handle;
			Plugin.logger.LogInfo("Second await end");
		}
	}

	public class WordHighlighter
	{
		public string rawText { get; private set; }

		public WordHighlighter(string text)
		{
			rawText = text;
		}

		public List<(int, int)> highlights = new List<(int, int)>();

		public void Highlight(int index, int endIndex)
		{
			int insertionIndex = 0;
			while (insertionIndex < highlights.Count && index > highlights[insertionIndex].Item1)
				insertionIndex += 1;

			bool removeLeft = false;
			if (insertionIndex > 0 && highlights[insertionIndex - 1].Item2 >= index - 1)
			{
				// Characters already highlighted
				if (highlights[insertionIndex - 1].Item2 >= endIndex)
					return;

				index = highlights[insertionIndex - 1].Item1;
				removeLeft = true;
			}

			int removeRightCnt = 0;
			while (true)
			{
				if (insertionIndex < (highlights.Count - removeRightCnt) && endIndex >= highlights[insertionIndex + removeRightCnt].Item1 - 1)
				{
					endIndex = Mathf.Max(endIndex, highlights[insertionIndex + removeRightCnt].Item2);
					removeRightCnt += 1;
					continue;
				}

				break;
			}

			highlights.Insert(insertionIndex, (index, endIndex));
			for (int i = 0; i < removeRightCnt; i++)
				highlights.RemoveAt(insertionIndex + 1);
			if (removeLeft)
				highlights.RemoveAt(insertionIndex - 1);
		}

		public string GenerateFormattedText(string prefix, string postfix)
		{
			if (rawText.Length == 0)
				return string.Empty;

			StringBuilder sb = new StringBuilder();
			string text = rawText;

			int currentIndex = 0;
			foreach ((int index, int endIndex) in highlights)
			{
				if (currentIndex < index)
					sb.Append(text.Substring(currentIndex, index - currentIndex));

				sb.Append(prefix);
				sb.Append(text.Substring(index, endIndex - index + 1));
				sb.Append(postfix);

				currentIndex = endIndex + 1;
			}

			if (currentIndex < text.Length)
				sb.Append(text.Substring(currentIndex));

			return sb.ToString();
		}
	}
}
