using AngryLevelLoader.DataTypes;
using Newtonsoft.Json;
using PluginConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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

        public static string AppData
		{
			get => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

		public static bool TryCreateDirectory(string path)
		{
			if (Directory.Exists(path))
				return false;
			Directory.CreateDirectory(path);
			return true;
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
        public static AngryBundleData TryGetAngryBundleData(string filePath, out Exception error)
		{
			error = null;

			try
			{
				return GetAngryBundleData(filePath);
            }
			catch (Exception e)
			{
				error = e;
				return null;
			}
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
}
