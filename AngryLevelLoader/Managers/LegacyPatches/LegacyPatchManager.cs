using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Train;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	internal class LazyAddressableAsset<T> where T : UnityEngine.Object
	{
		private T _asset = null;
		private readonly string path;

		public LazyAddressableAsset(string path)
		{
			this.path = path;
		}

		public T Get()
		{
			if (_asset != null)
				return _asset;

			_asset = Addressables.LoadAssetAsync<T>(path).WaitForCompletion();
			return _asset;
		}
	}

	internal class LazyAsset<T>
	{
		private T _asset = default(T);
		private Func<T> _assetGetter = null;

		public LazyAsset(Func<T> assetGetter)
		{
			this._assetGetter = assetGetter;
		}

		public T Get()
		{
			if (_asset != null)
				return _asset;

			_asset = _assetGetter.Invoke();
			return _asset;
		}
	}

	internal enum LegacyPatchState
	{
		None,
		V6,
		V7,
	}

	internal class LegacyPatchManager
	{
		public const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		public const BindingFlags STATIC = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		public static LegacyPatchState patchState { get; private set; } = LegacyPatchState.None;
		public static Harmony legacyHarmony = new Harmony($"{Plugin.PLUGIN_GUID}_legacyPatches");

		internal static void Init()
		{
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (mode == LoadSceneMode.Additive)
					return;

				if (AngrySceneManager.isInCustomLevel)
				{
					int levelVersion = AngrySceneManager.currentBundleContainer.BundleVersion;

					if (levelVersion == 6)
					{
						SetLegacyPatchState(LegacyPatchState.V6);
					}
					else if (levelVersion == 7)
					{
						SetLegacyPatchState(LegacyPatchState.V7);
					}
					else
					{
						SetLegacyPatchState(LegacyPatchState.None);
					}
				}
				else
				{
					SetLegacyPatchState(LegacyPatchState.None);
				}
			};
		}

		public static void SetLegacyPatchState(LegacyPatchState state)
		{
			if (patchState == state)
				return;

			patchState = state;
			legacyHarmony.UnpatchSelf();

			if (state == LegacyPatchState.V6)
			{
				// Apply all Revamp patches
				V6LegacyScriptPatches.Patch(legacyHarmony);
				V6LegacyEnemyPatches.Patch(legacyHarmony);
			}
			else if (state == LegacyPatchState.V7)
			{
				// Apply some patches that fixes compability issues from 17b2 to 17d2
				V7LegacyPlayerPatches.Patch(legacyHarmony);
			}
		}
	}
}
