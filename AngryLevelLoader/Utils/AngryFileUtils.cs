using AngryLevelLoader.DataTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.Utility;
using static UnityEngine.AddressableAssets.ResourceLocators.ContentCatalogData;

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
            try
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
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e);
                return false;
            }

            return false;
        }

        private static Regex internalIdRegex = new Regex(@"^{AngryLevelLoader\.Plugin\.tempFolderPath}(?:\\|\/)+([\da-f]{32}.*)");

		/// <summary>
		/// Converts an addressables catalog to use Angry's custom ZippedAssetBundleProvider to avoid unpacking big asset bundle files
		/// </summary>
		internal static async Task MakeCatalogZipProviderSupported(string catalogPath)
        {
            const string zippedProviderTypeName = "AngryLevelLoader.AssetBundleProviders.ZippedAssetBundleProvider";

			JObject catalog = JObject.Parse(await File.ReadAllTextAsync(catalogPath));

            JArray providers = catalog["m_ProviderIds"] as JArray;
            JArray internalIds = catalog["m_InternalIds"] as JArray;

			if (providers.Contains(zippedProviderTypeName))
            {
                Plugin.logger.LogWarning($"Cannot convert '{catalogPath}' because the file already supports zipped load");
                return;
            }

			providers.Add(zippedProviderTypeName);
            int zippedProviderIndex = providers.Count - 1;

            JObject zippedProviderData = new JObject();
            zippedProviderData["m_Id"] = "AngryLevelLoader.AssetBundleProviders.ZippedAssetBundleProvider";
            zippedProviderData["m_ObjectType"] = new JObject();
            zippedProviderData["m_ObjectType"]["m_AssemblyName"] = "AngryLevelLoader.AssetBundleProviders, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            zippedProviderData["m_ObjectType"]["m_ClassName"] = "AngryLevelLoader.AssetBundleProviders.ZippedAssetBundleProvider";
            zippedProviderData["m_Data"] = "";
            (catalog["m_ResourceProviderData"] as JArray).Add(zippedProviderData);

			byte[] entryData = Convert.FromBase64String(catalog["m_EntryDataString"].Value<string>());
            int entryCnt = BitConverter.ToInt32(entryData, 0);

            for (int i = 0; i < entryCnt; i++)
            {
                int internalIdIdx = BitConverter.ToInt32(entryData, 4 + i * 7);

                if (internalIdIdx < 0 || internalIdIdx >= internalIds.Count)
                    continue;

                string internalId = internalIds[internalIdIdx].Value<string>();
                if (!internalId.StartsWith("{AngryLevelLoader.Plugin.tempFolderPath}"))
                    continue;

                Match internalIdMatch = internalIdRegex.Match(internalId);
                if (!internalIdMatch.Success)
                    continue;

                internalIds[internalIdIdx] = internalIdMatch.Groups[1].Value;
                BitConverter.TryWriteBytes(entryData.AsSpan(4 + i * 7 + 4, 4), zippedProviderIndex);
			}

			catalog["m_EntryDataString"] = Convert.ToBase64String(entryData);

            using (JsonWriter jsonStream = new JsonTextWriter(new StreamWriter(File.Open(catalogPath, FileMode.Truncate, FileAccess.Write))))
                await catalog.WriteToAsync(jsonStream);
		}
    }
}
