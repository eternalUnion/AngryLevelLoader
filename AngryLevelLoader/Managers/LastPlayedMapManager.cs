using AngryLevelLoader.Containers;
using AngryLevelLoader.UserInterface;
using AngryLevelLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace AngryLevelLoader.Managers
{
	// System which tracks when a bundle was played last in unix time
	internal static class LastPlayedMapManager
	{
		public static readonly Dictionary<string, long> lastPlayed = new Dictionary<string, long>();
		public static readonly Dictionary<string, long> lastUpdate = new Dictionary<string, long>();

		public static void LoadLastPlayedMap()
		{
			lastPlayed.Clear();

			string path = AngryPaths.LastPlayedMapPath;
			if (!File.Exists(path))
				return;

			using (StreamReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read)))
			{
				while (!reader.EndOfStream)
				{
					string key = reader.ReadLine();
					if (reader.EndOfStream)
					{
						Plugin.logger.LogWarning("Invalid end of last played map file");
						break;
					}

					string value = reader.ReadLine();
					if (long.TryParse(value, out long seconds))
					{
						lastPlayed[key] = seconds;
					}
					else
					{
						Plugin.logger.LogInfo($"Invalid last played time '{value}'");
					}
				}
			}
		}

		public static void LoadLastUpdateMap()
		{
			lastUpdate.Clear();

			string path = AngryPaths.LastUpdateMapPath;
			if (!File.Exists(path))
				return;

			using (StreamReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read)))
			{
				while (!reader.EndOfStream)
				{
					string key = reader.ReadLine();
					if (reader.EndOfStream)
					{
						Plugin.logger.LogWarning("Invalid end of last played map file");
						break;
					}

					string value = reader.ReadLine();
					if (long.TryParse(value, out long seconds))
					{
						lastUpdate[key] = seconds;
					}
					else
					{
						Plugin.logger.LogInfo($"Invalid last played time '{value}'");
					}
				}
			}
		}

		public static void UpdateLastPlayed(BundleContainer bundle)
		{
			string guid = bundle.bundleGuid;
			if (guid.Length != 32)
				return;

			long secondsNow = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			lastPlayed[guid] = secondsNow;

			string path = AngryPaths.LastPlayedMapPath;
			AngryIOUtils.TryCreateDirectoryForFile(path);
			using (StreamWriter writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)))
			{
				writer.BaseStream.Seek(0, SeekOrigin.Begin);
				writer.BaseStream.SetLength(0);
				foreach (var pair in lastPlayed)
				{
					writer.WriteLine(pair.Key);
					writer.WriteLine(pair.Value.ToString());
				}
			}

			if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.LastPlayed)
				AngryBundleList.SortBundles();
		}

		public static void UpdateLastUpdate(BundleContainer bundle)
		{
			string guid = bundle.bundleGuid;
			if (guid.Length != 32)
				return;

			long secondsNow = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			lastUpdate[guid] = secondsNow;

			string path = AngryPaths.LastUpdateMapPath;
			AngryIOUtils.TryCreateDirectoryForFile(path);
			using (StreamWriter writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)))
			{
				writer.BaseStream.Seek(0, SeekOrigin.Begin);
				writer.BaseStream.SetLength(0);
				foreach (var pair in lastUpdate)
				{
					writer.WriteLine(pair.Key);
					writer.WriteLine(pair.Value.ToString());
				}
			}

			if (ConfigManager.bundleSortingMode.value == ConfigManager.BundleSorting.LastUpdate)
				AngryBundleList.SortBundles();
		}
	}
}