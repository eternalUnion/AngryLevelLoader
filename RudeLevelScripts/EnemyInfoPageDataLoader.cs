using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RudeLevelScripts
{
	[RequireComponent(typeof(EnemyInfoPage))]
	public class EnemyInfoPageDataLoader : MonoBehaviour
	{
		public List<SpawnableObject> additionalEnemies;

		public void LoadDataAndDestroy()
		{
			EnemyInfoPage comp = GetComponent<EnemyInfoPage>();
			comp.objects = Instantiate(Addressables.LoadAssetAsync<SpawnableObjectsDatabase>("Assets/Data/Bestiary Database.asset").WaitForCompletion());
			
			if (additionalEnemies != null && additionalEnemies.Count != 0)
			{
				var newEnemies = comp.objects.enemies.ToList();
				newEnemies.AddRange(additionalEnemies);
				comp.objects.enemies = newEnemies.ToArray();
			}

			Destroy(this);
		}
	}
}
