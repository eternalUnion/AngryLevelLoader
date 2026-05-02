using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AngryLevelLoader.Utils
{
	internal static class AngryIOUtils
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
					if (badAppData.IsMatch(_AppData) && Environment.UserName != "User")
					{
						Plugin.logger.LogWarning($"Bad username for local AppData (got '{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}', expected user '{Environment.UserName}'), falling back to absolute path");

						char driveLetter = _AppData.Length > 0 ? _AppData[0] : 'C';
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
	}
}
