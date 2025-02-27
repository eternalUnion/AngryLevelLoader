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
					_gameFont = Addressables.LoadAssetAsync<Font>("Assets/Fonts/VCR_OSD_MONO_1.001.ttf").WaitForCompletion();
				return _gameFont;
			}
		}

		private static Sprite _levelPanel;
		public static Sprite levelPanel
		{
			get
			{
				if (_levelPanel == null)
					_levelPanel = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/meter.png").WaitForCompletion();
				return _levelPanel;
			}
		}

		private static Sprite _hellmapArrow;
		public static Sprite hellmapArrow
		{
			get
			{
				if (_hellmapArrow == null)
					_hellmapArrow = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/arrow.png").WaitForCompletion();
				return _hellmapArrow;
			}
		}

		private static Material _metalDec20;
		public static Material metalDec20
		{
			get
			{
				if (_metalDec20 == null)
					_metalDec20 = Addressables.LoadAssetAsync<Material>("Assets/Materials/Environment/Metal/Metal Decoration 20.mat").WaitForCompletion();
				return _metalDec20;
			}
		}

		//Jank... but it works.
		public static void SetPlayerWorldRotation(Quaternion newRotation)
		{
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
