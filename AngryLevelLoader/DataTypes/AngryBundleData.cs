using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.DataTypes
{
    public class AngryBundleData
    {
        public string bundleName { get; set; }
        public string bundleAuthor { get; set; }
        public string bundleGuid { get; set; }
        public string buildHash { get; set; }
        public string bundleDataPath { get; set; }
        public List<string> levelDataPaths;
    }
}
