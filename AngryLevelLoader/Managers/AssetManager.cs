using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AngryLevelLoader.Managers
{
	internal abstract class AsyncObject
	{
		public abstract bool completed { get; }
		public abstract void WaitForCompletion();
	}

	internal class AsyncAddressableObject<T> : AsyncObject where T : UnityEngine.Object
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

	internal static class AssetManager
	{
		private static AsyncOperationHandle<bool> cleanBundleCacheHandle;
		public static AsyncOperationHandle<bool> CleanBundleCache()
		{
			if (cleanBundleCacheHandle.IsDone)
				cleanBundleCacheHandle = Addressables.CleanBundleCache();

			return cleanBundleCacheHandle;
		}

		private static AsyncAddressableObject<Sprite> _heart;
		public static Sprite heart
		{
			get
			{
				if (!_heart.completed)
					_heart.WaitForCompletion();
				return _heart.result;
			}
		}

		private static AsyncAddressableObject<Sprite> _heartFilled;
		public static Sprite heartFilled
		{
			get
			{
				if (!_heartFilled.completed)
					_heartFilled.WaitForCompletion();
				return _heartFilled.result;
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

		private static AsyncAddressableObject<Sprite> _favouriteUnselected;
		public static Sprite favouriteUnselected
		{
			get
			{
				if (!_favouriteUnselected.completed)
					_favouriteUnselected.WaitForCompletion();
				return _favouriteUnselected.result;
			}
		}

		private static AsyncAddressableObject<Sprite> _favouriteSelected;
		public static Sprite favouriteSelected
		{
			get
			{
				if (!_favouriteSelected.completed)
					_favouriteSelected.WaitForCompletion();
				return _favouriteSelected.result;
			}
		}

		private static AsyncAddressableObject<Texture2D> _unknownProfile;
		public static Texture2D unknownProfile
		{
			get
			{
				if (!_unknownProfile.completed)
					_unknownProfile.WaitForCompletion();
				return _unknownProfile.result;
			}
		}

		private static bool _inited = false;
		public static void Init()
		{
			if (_inited)
				return;
			_inited = true;

			_heart = new AsyncAddressableObject<Sprite>("AngryLevelLoader/Textures/heart-unselected.png");
			_heartFilled = new AsyncAddressableObject<Sprite>("AngryLevelLoader/Textures/heart-selected.png");
			_notPlayedPreview = new AsyncAddressableObject<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked3.png");
			_lockedPreview = new AsyncAddressableObject<Sprite>("Assets/Textures/UI/Level Thumbnails/Locked.png");
			_favouriteUnselected = new AsyncAddressableObject<Sprite>("AngryLevelLoader/Textures/fav-unselected.png");
			_favouriteSelected = new AsyncAddressableObject<Sprite>("AngryLevelLoader/Textures/fav-selected.png");
			_unknownProfile = new AsyncAddressableObject<Texture2D>("AngryLevelLoader/Textures/unknown-profile.png");
		}
	}
}
