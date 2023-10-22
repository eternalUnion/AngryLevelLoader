using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AngryLevelLoader.Managers
{
	public abstract class AsyncObject
	{
		public abstract bool completed { get; }
		public abstract void WaitForCompletion();
	}

	public class AsyncAddressableObject<T> : AsyncObject where T : UnityEngine.Object
	{
		private bool _completed = false;
		public override bool completed => _completed;

		private AsyncOperationHandle<T> _handle;

		public T result;

		public AsyncAddressableObject(string path)
		{
			_handle = Addressables.LoadAssetAsync<T>(path);
			_handle.Completed += (h) =>
			{
				_completed = true;
				result = h.Result;
			};
		}

		public override void WaitForCompletion()
		{
			if (_completed)
				return;

			_handle.WaitForCompletion();
			_completed = true;
			result = _handle.Result;
		}
	}

	public static class AssetManager
	{
		private static AsyncOperationHandle<bool> cleanBundleCacheHandle;
		public static AsyncOperationHandle<bool> CleanBundleCache()
		{
			if (cleanBundleCacheHandle.IsDone)
				cleanBundleCacheHandle = Addressables.CleanBundleCache();

			return cleanBundleCacheHandle;
		}

		private static AsyncAddressableObject<Sprite> _arrow;
		public static Sprite arrow
		{
			get
			{
				if (!_arrow.completed)
					_arrow.WaitForCompletion();
				return _arrow.result;
			}
		}

		private static AsyncAddressableObject<Sprite> _arrowFilled;
		public static Sprite arrowFilled
		{
			get
			{
				if (!_arrowFilled.completed)
					_arrowFilled.WaitForCompletion();
				return _arrowFilled.result;
			}
		}

		private static AsyncAddressableObject<Sprite> _notPlayedPreview;
		public static Sprite notPlayedPreview
		{
			get
			{
				if (!_notPlayedPreview.completed)
					_notPlayedPreview.WaitForCompletion();
				return _notPlayedPreview.result;
			}
		}

		private static AsyncAddressableObject<Sprite> _lockedPreview;
		public static Sprite lockedPreview
		{
			get
			{
				if (!_lockedPreview.completed)
					_lockedPreview.WaitForCompletion();
				return _lockedPreview.result;
			}
		}

		private static bool _inited = false;
		public static void Init()
		{
			if (_inited)
				return;
			_inited = true;

			_arrow = new AsyncAddressableObject<Sprite>("AngryLevelLoader/Textures/arrow.png");
			_arrowFilled = new AsyncAddressableObject<Sprite>("AngryLevelLoader/Textures/arrow-filled.png");
			_notPlayedPreview = new AsyncAddressableObject<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked3.png");
			_lockedPreview = new AsyncAddressableObject<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked.png");
		}
	}
}
