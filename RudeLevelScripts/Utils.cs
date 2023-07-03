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
		private static Font _gameFont;
		public static Font gameFont
		{
			get
			{
				if (_gameFont == null)
					_gameFont = LoadObject<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf");
				return _gameFont;
			}
		}

		private static Sprite _levelPanel;
		public static Sprite levelPanel
		{
			get
			{
				if (_levelPanel == null)
					_levelPanel = LoadObject<Sprite>("Assets/Textures/UI/meter.png");
				return _levelPanel;
			}
		}

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
