using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine;
using System.Collections;

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

		private static Sprite _hellmapArrow;
		public static Sprite hellmapArrow
		{
			get
			{
				if (_hellmapArrow == null)
					_hellmapArrow = LoadObject<Sprite>("Assets/Textures/UI/arrow.png");
				return _hellmapArrow;
			}
		}

		private static Material _metalDec20;
		public static Material metalDec20
		{
			get
			{
				if (_metalDec20 == null)
					_metalDec20 = LoadObject<Material>("Assets/Materials/Environment/Metal/Metal Decoration 20.mat");
				return _metalDec20;
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

			return Addressables.LoadAssetAsync<T>(obj.Value.First()).WaitForCompletion();
		}

		//Jank... but it works.
		public static void SetPlayerWorldRotation(Quaternion newRotation)
		{
			Quaternion oldRot = CameraController.Instance.transform.rotation;
			CameraController.Instance.transform.rotation = newRotation;
			float sampleX = CameraController.Instance.transform.localEulerAngles.x;
			float newX = sampleX;

			if (sampleX <= 90.0f && sampleX >= 0)
			{
				newX = -sampleX;
			}
			else if (sampleX >= 270.0f && sampleX <= 360.0f)
			{
				newX = Mathf.Lerp(0.0f, 90.0f, Mathf.InverseLerp(360.0f, 270.0f, sampleX));
			}

			float newY = CameraController.Instance.transform.rotation.eulerAngles.y;

			CameraController.Instance.rotationX = newX;
			CameraController.Instance.rotationY = newY;
		}
	}

	public static class UnityUtils
	{
		public static IEnumerable GetComponentsInChildrenRecursive<T>(Transform parent) where T : Component
		{
			foreach (Transform child in parent)
			{
				if (child.TryGetComponent(out T comp))
					yield return comp;

				foreach (T childComp in GetComponentsInChildrenRecursive<T>(child))
					yield return childComp;
			}
		}

		public static T GetComponentInChildrenRecursive<T>(Transform parent) where T : Component
		{
			foreach (Transform child in parent)
			{
				if (child.TryGetComponent(out T comp))
					return comp;

				T childComp = GetComponentInChildrenRecursive<T>(child);
				if (childComp != null)
					return childComp;
			}

			return null;
		}
	}
}
