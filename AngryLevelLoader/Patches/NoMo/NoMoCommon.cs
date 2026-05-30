using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMo
{
	internal static class NoMoCommon
	{
		// We need to assume any unity operation may throw an exception and break up the flow
		internal static void TriggerEnemyEvents(GameObject enemy)
		{
			if (enemy == null)
				return;

			try
			{
				enemy.SetActive(true);
			}
			catch (Exception e)
			{
				Plugin.logger.LogError(e);
			}

			EnemyIdentifier eid = enemy.gameObject.GetComponentInChildren<EnemyIdentifier>(true);
			if (eid != null && !eid.dead)
			{
				if (eid.enemyType == EnemyType.Deathcatcher)
				{
					Transform openerTrans = (enemy.transform.parent == null) ? null : enemy.transform.parent.Find("Opener");
					if (openerTrans != null && openerTrans.gameObject.TryGetComponent(out ObjectActivator obac) && !obac.gameObject.activeSelf)
					{
						obac.events.onActivate.AddListener(() =>
						{
							TriggerEnemyEvents(enemy);
						});

						return;
					}
				}

				eid.health = 0;
				eid.dead = true;

				if (eid.onDeath != null)
				{
					try
					{
						eid.onDeath.Invoke();
					}
					catch (Exception e)
					{
						Plugin.logger.LogError(e);
					}
				}

				if (eid.activateOnDeath != null)
				{
					foreach (GameObject go in eid.activateOnDeath)
					{
						if (go == null)
							continue;

						try
						{
							go.SetActive(true);
						}
						catch (Exception e)
						{
							Plugin.logger.LogError(e);
						}
					}
				}

				if (eid.destroyOnDeath != null)
				{
					foreach (GameObject go in eid.destroyOnDeath)
					{
						if (go == null)
							continue;

						try
						{
							UnityEngine.GameObject.Destroy(go);
						}
						catch (Exception e)
						{
							Plugin.logger.LogError(e);
						}
					}
				}

				if (EnemyTracker.Instance)
				{
					EnemyTracker.Instance.RemoveEnemy(eid);
				}
			}

			foreach (ActivateNextWaveHP waveHp in enemy.GetComponentsInChildren<ActivateNextWaveHP>(true))
			{
				waveHp.Update();
			}

			ActivateNextWave wave = enemy.gameObject.GetComponentInParent<ActivateNextWave>(true);
			if (wave != null && (eid == null || !eid.dontCountAsKills))
			{
				wave.AddDeadEnemy();
				wave.FixedUpdate();
			}
		}
	}
}
