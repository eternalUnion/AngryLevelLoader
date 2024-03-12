using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AngryLevelLoader
{
    public static class AngryPaths
    {
        public const string SERVER_ROOT_GLOBAL = "https://angry.dnzsoft.com";
        public const string SERVER_ROOT_LOCAL = "http://localhost:3000";

        public static string SERVER_ROOT
        {
            get => Plugin.useLocalServer.value ? SERVER_ROOT_LOCAL : SERVER_ROOT_GLOBAL;
        }

        public static void TryCreateAllPaths()
        {
            IOUtils.TryCreateDirectory(ConfigFolderPath);
            IOUtils.TryCreateDirectory(OnlineCacheFolderPath);
            IOUtils.TryCreateDirectory(ThumbnailCacheFolderPath);
        }

        public static string ConfigFolderPath
        {
            get => Path.Combine(Paths.ConfigPath, "AngryLevelLoader");
        }

        public static string OnlineCacheFolderPath
        {
            get => Path.Combine(ConfigFolderPath, "OnlineCache");
        }

        public static string ThumbnailCachePath
        {
            get => Path.Combine(OnlineCacheFolderPath, "thumbnailCacheHashes.txt");
        }

        public static string LevelCatalogCachePath
        {
            get => Path.Combine(OnlineCacheFolderPath, "LevelCatalog.json");
        }

        public static string ScriptCatalogCachePath
        {
            get => Path.Combine(OnlineCacheFolderPath, "ScriptCatalog.json");
        }

        public static string ThumbnailCacheFolderPath
        {
            get => Path.Combine(OnlineCacheFolderPath, "ThumbnailCache");
        }

        public static string LastPlayedMapPath
        {
            get => Path.Combine(ConfigFolderPath, "lastPlayedMap.txt");
        }

		public static string LastUpdateMapPath
		{
			get => Path.Combine(ConfigFolderPath, "lastUpdateMap.txt");
		}
        
	}
}
