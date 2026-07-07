using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using AsyncOperation = UnityEngine.AsyncOperation;
using Debug = UnityEngine.Debug;

namespace AngryLevelLoader.AssetBundleProviders
{
	class ZippedAssetBundleResource : IAssetBundleResource
	{
		private ProvideHandle handle;
		private string zipPath;
		private string entryName;

		private AssetBundleCreateRequest assetBundleOp;
		private AssetBundle assetBundle = null;

		public AssetBundle GetAssetBundle()
		{
			return assetBundle;
		}

		public ZippedAssetBundleResource(ProvideHandle handle, string zipPath, string entryName)
		{
			this.handle = handle;
			this.zipPath = zipPath;
			this.entryName = entryName;
		}

		public void Start()
		{
			ZipArchive archive = null;
			Stream bundleStream = null;

			void DisposeResources()
			{
				if (bundleStream != null)
				{
					try
					{
						bundleStream.Close();
					}
					catch (Exception ex)
					{
						ZippedAssetBundleProvider.LogError(ex);
					}

					try
					{
						bundleStream.Dispose();
					}
					catch (Exception ex)
					{
						ZippedAssetBundleProvider.LogError(ex);
					}
				}

				if (archive != null)
				{
					try
					{
						archive.Dispose();
					}
					catch (Exception ex)
					{
						ZippedAssetBundleProvider.LogError(ex);
					}
				}
			}

			void CompleteOperation()
			{
				handle.Complete(this, true, null);
				DisposeResources();
			}

			void CompleteWithError(Exception ex)
			{
				ZippedAssetBundleProvider.LogError(ex);
				handle.Complete<ZippedAssetBundleResource>(null, false, ex);
				DisposeResources();
			}

			try
			{
				archive = new ZipArchive(File.Open(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read);
				ZipArchiveEntry bundleEntry = archive.GetEntry(entryName);

				if (bundleEntry == null)
				{
					CompleteWithError(new Exception($"Could not locate entry '{entryName}' at zip file '{zipPath}'"));
					return;
				}

				byte[] bundleBinary = new byte[bundleEntry.Length];
				bundleStream = bundleEntry.Open();
				Task<int> readZipTask = bundleStream.ReadAsync(bundleBinary).AsTask();

				readZipTask.ContinueWith((_) =>
				{
					if (readZipTask.Exception != null)
					{
						CompleteWithError(readZipTask.Exception);
						return;
					}

					assetBundleOp = AssetBundle.LoadFromMemoryAsync(bundleBinary);
					void OnCompletion()
					{
						assetBundle = assetBundleOp.assetBundle;

						if (assetBundle == null)
							CompleteWithError(new Exception($"Failed to load asset bundle '{entryName}'"));
						else
							CompleteOperation();
					}

					if (assetBundleOp.isDone)
					{
						OnCompletion();
					}
					else
					{
						assetBundleOp.completed += (_) =>
						{
							OnCompletion();
						};
					}

				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
			catch (Exception ex)
			{
				CompleteWithError(ex);
			}
		}

		public bool Unload(out AssetBundleUnloadOperation unloadOp)
		{
			unloadOp = null;

			if (assetBundle != null)
			{
				unloadOp = assetBundle.UnloadAsync(true);
				assetBundle = null;
			}

			assetBundleOp = null;
			return unloadOp != null;
		}
	}

	[DisplayName("Zipped AssetBundle Provider")]
	public class ZippedAssetBundleProvider : ResourceProviderBase
	{
		public static Func<string, string> ResolveBundleGuidToZipFilePath;
		public static ManualLogSource logger;

		internal static void Log(object o)
		{
			if (logger != null)
				logger.LogInfo($"{nameof(ZippedAssetBundleProvider)} : {o}");
		}

		internal static void LogError(object o)
		{
			if (logger != null)
				logger.LogError($"{nameof(ZippedAssetBundleProvider)} : {o}");
		}

		public override void Provide(ProvideHandle providerInterface)
		{
			string internalId = providerInterface.Location.InternalId;
			if (internalId.Length < 33)
			{
				LogError($"Zipped bundle has invalid internal id '{internalId}'");
				providerInterface.Complete<ZippedAssetBundleProvider>(null, false, new Exception($"Zipped bundle has invalid internal id '{internalId}'"));
				return;
			}

			// Format of internal id: "<bundle guid>/<entry name>"

			string bundleGuid = internalId.Substring(0, 32);
			string entryName = internalId.Substring(33);

			if (ResolveBundleGuidToZipFilePath == null)
			{
				LogError("Delegate for resolving bundle guid is not provided");
				providerInterface.Complete<ZippedAssetBundleProvider>(null, false, new Exception("Delegate for resolving bundle guid is not provided"));
				return;
			}

			string zipFilePath = ResolveBundleGuidToZipFilePath(bundleGuid);
			if (string.IsNullOrEmpty(zipFilePath) || !File.Exists(zipFilePath))
			{
				LogError($"Could not locate zip file for bundle '{internalId}'");
				providerInterface.Complete<ZippedAssetBundleProvider>(null, false, new IOException($"Could not locate zip file for bundle '{internalId}'"));
				return;
			}

			new ZippedAssetBundleResource(providerInterface, zipFilePath, entryName).Start();
		}

		public override Type GetDefaultType(IResourceLocation location)
		{
			return typeof(IAssetBundleResource);
		}

		public override void Release(IResourceLocation location, object asset)
		{
			if (location == null)
				throw new ArgumentNullException("location");

			if (asset == null)
			{
				return;
			}

			var bundle = asset as ZippedAssetBundleResource;
			if (bundle != null)
			{
				bundle.Unload(out var unloadOp);
			}
		}
	}
}
