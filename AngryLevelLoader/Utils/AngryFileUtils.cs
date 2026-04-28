using AngryLevelLoader.DataTypes;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;

namespace AngryLevelLoader.Utils
{
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
		
		private static AngryBundleData GetAngryBundleData(string filePath)
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
