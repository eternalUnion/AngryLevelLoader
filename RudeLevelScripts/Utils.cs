using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine;

namespace RudeLevelScript
{
	public static class Utils
	{
		public static ResourceLocationMap resourceMap = null;
		public static T LoadObject<T>(string path)
		{
			if (resourceMap == null)
			{
				Addressables.InitializeAsync().WaitForCompletion();
				resourceMap = Addressables.ResourceLocators.First() as ResourceLocationMap;
			}

			Debug.Log($"Loading {path}");
			KeyValuePair<object, IList<IResourceLocation>> obj;

			try
			{
				obj = resourceMap.Locations.Where(
					(KeyValuePair<object, IList<IResourceLocation>> pair) =>
					{
						return (pair.Key as string) == path;
						//return (pair.Key as string).Equals(path, StringComparison.OrdinalIgnoreCase);
					}).First();
			}
			catch (Exception) { return default(T); }

			return Addressables.LoadAsset<T>(obj.Value.First()).WaitForCompletion();
		}
	}
}
