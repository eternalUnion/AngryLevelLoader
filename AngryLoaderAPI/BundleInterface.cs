using AngryLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLoaderAPI
{
	public static class BundleInterface
	{
		public static bool BundleExists(string bundleGuid)
		{
			return RudeBundleInterface.BundleExists(bundleGuid);
		}

		public static string GetBundleBuildHash(string bundleGuid)
		{
			return RudeBundleInterface.GetBundleBuildHash(bundleGuid);
		}
    }
}
