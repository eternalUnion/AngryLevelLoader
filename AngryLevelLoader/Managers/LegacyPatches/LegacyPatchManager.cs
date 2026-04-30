using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	internal class LegacyPatchAttribute : Attribute
	{
		public readonly LegacyPatchState targetVersion;

		public LegacyPatchAttribute(LegacyPatchState targetVersion)
		{
			this.targetVersion = targetVersion;
		}
	}

	/// <summary>
	/// Older angry files may need some additional patches to work.
	/// These patches are only applied if the angry file version matches the legacy patch version.
	/// </summary>
	internal static class LegacyPatchManager
	{
		internal static Dictionary<LegacyPatchState, List<Type>> patches = new Dictionary<LegacyPatchState, List<Type>>();

		static LegacyPatchManager()
		{
			foreach (LegacyPatchState patchState in Enum.GetValues(typeof(LegacyPatchState)))
				patches[patchState] = new List<Type>();

			foreach (Type patchType in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute(typeof(LegacyPatchAttribute)) != null))
			{
				foreach (LegacyPatchState patchVersion in patchType.GetCustomAttributes<LegacyPatchAttribute>().Select(attr => attr.targetVersion).Distinct())
				{
					patches[patchVersion].Add(patchType);
				}
			}
		}

		private static LegacyPatchState patchState = LegacyPatchState.None;
		private static Harmony legacyHarmony = new Harmony($"{Plugin.PLUGIN_GUID}_legacyPatches");

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

		internal static void SetLegacyPatchState(LegacyPatchState state)
		{
			if (patchState == state)
				return;

			patchState = state;
			legacyHarmony.UnpatchSelf();

			if (patches.TryGetValue(state, out List<Type> patchClasses))
			{
				foreach (Type patchClass in patchClasses)
				{
					legacyHarmony.PatchAll(patchClass);
				}
			}
		}
	}
}
