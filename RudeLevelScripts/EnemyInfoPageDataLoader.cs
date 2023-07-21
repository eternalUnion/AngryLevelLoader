using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RudeLevelScripts
{
	[RequireComponent(typeof(EnemyInfoPage))]
	public class EnemyInfoPageDataLoader : MonoBehaviour
	{
		public void LoadDataAndDestroy()
		{
			EnemyInfoPage comp = GetComponent<EnemyInfoPage>();
			comp.objects = Addressables.LoadAssetAsync<SpawnableObjectsDatabase>("Assets/Data/Bestiary Database.asset").WaitForCompletion();
			Destroy(this);
		}
	}
}
