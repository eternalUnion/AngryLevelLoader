using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace AngryLevelLoader.DataTypes
{
	/// <summary>
	/// Metadata for an angry bundle. This object is stored inside data.json, which is
	/// located inside the angry file.
	/// </summary>
	public class AngryBundleData
    {
        // V2-V6
        public string bundleName { get; set; }
        public string bundleAuthor { get; set; }
        public string bundleGuid { get; set; }
        public string buildHash { get; set; }
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int bundleVersion { get; set; }
        public string bundleDataPath { get; set; }
        public List<string> levelDataPaths;

        // V7
        [DefaultValue(false)]
        public bool epilepsyWarning { get; set; }
        [DefaultValue(null)]
        public List<AngryLevelData> levels;
        [DefaultValue(false)]
        public bool zippedProviderSupported { get; set; }
    }
}
