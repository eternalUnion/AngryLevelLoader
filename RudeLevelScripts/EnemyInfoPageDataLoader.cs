using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RudeLevelScripts
{
	public class EnemyInfoPageDataLoader : MonoBehaviour
	{
		public EnemyInfoPage target;
        public List<SpawnableObject> additionalEnemies;

		public void Awake()
		{
            LoadDataAndDestroy();
		}

		public void LoadDataAndDestroy()
		{
			StartCoroutine(LoadDataAndDestroyAsync());
		}

		private static SpawnableObjectsDatabase database;
		private bool _taskStarted = false;
        private IEnumerator LoadDataAndDestroyAsync()
		{
			if (_taskStarted)
				yield break;
			_taskStarted = true;

			if (database == null)
			{
				var handle = Addressables.LoadAssetAsync<SpawnableObjectsDatabase>("Assets/Data/Bestiary Database.asset");
				yield return handle;
                database = handle.Result;
			}

			if (target == null)
                target = GetComponent<EnemyInfoPage>();
            target.objects = Instantiate(database);

            if (additionalEnemies != null && additionalEnemies.Count != 0)
            {
                var newEnemies = target.objects.enemies.ToList();
                newEnemies.AddRange(additionalEnemies);
                target.objects.enemies = newEnemies.ToArray();
            }

            Destroy(this);
        }
	}
}
