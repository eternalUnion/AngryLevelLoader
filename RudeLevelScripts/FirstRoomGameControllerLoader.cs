using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RudeLevelScripts
{
	internal static class GameObjectExtensions
	{
		public static bool TryGetComponentInChildren<T>(this GameObject obj,  out T result) where T : Component
		{
			result = obj.GetComponentInChildren<T>();
			return result != null;
		}
	}

	public class FirstRoomGameControllerLoader : MonoBehaviour
	{
		public void RunAndDestroy()
		{
			if (gameObject.TryGetComponentInChildren(out PlayerTracker tracker))
			{
				if (tracker.platformerPlayerPrefab == null)
				{
					tracker.platformerPlayerPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Player/PlatformerController.prefab").WaitForCompletion();
				}
			}

			if (gameObject.TryGetComponentInChildren(out SandboxSaver sbSaver))
			{
				if (sbSaver.objects == null)
				{
					sbSaver.objects = Addressables.LoadAssetAsync<SpawnableObjectsDatabase>("Assets/Data/Sandbox/Spawnable Objects Database.asset").WaitForCompletion();
				}
			}

			if (gameObject.TryGetComponentInChildren(out TimeController timeContr))
			{
				if (timeContr.parryLight == null)
				{
					timeContr.parryLight = Addressables.LoadAssetAsync<GameObject>("Assets/Particles/ParryLight.prefab").WaitForCompletion();
				}
			}
		}
	}
}
