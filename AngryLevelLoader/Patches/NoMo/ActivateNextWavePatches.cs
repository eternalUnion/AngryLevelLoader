using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(ActivateNextWave))]
	internal class ActivateNextWavePatches
	{
		[HarmonyPatch(nameof(ActivateNextWave.FixedUpdate))]
		[HarmonyPrefix]
		private static bool FixedUpdate(ActivateNextWave __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return true;

			if (__instance.activated || __instance.deadEnemies < __instance.enemyCount)
				return false;

			__instance.activated = true;
			
			if (!__instance.lastWave)
			{
				if (__instance.toActivate != null)
				{
					foreach (GameObject gameObject in __instance.toActivate)
					{
						if (gameObject == null)
							continue;
						
						try
						{
							gameObject.SetActive(true);
						}
						catch (Exception e)
						{
							Plugin.logger.LogError(e);
						}
					}
				}

				if (__instance.doors != null)
				{
					foreach (Door door in __instance.doors)
					{
						if (door == null)
							continue;

						try
						{
							door.Unlock();
						}
						catch (Exception e)
						{
							Plugin.logger.LogError(e);
						}
					}
				}

				foreach (GameObject enemy in __instance.nextEnemies)
				{
					NoMoCommon.TriggerEnemyEvents(enemy);
				}
			}
			else
			{
				if (__instance.toActivate != null)
				{
					foreach (GameObject gameObject in __instance.toActivate)
					{
						if (gameObject == null)
							continue;

						try
						{
							gameObject.SetActive(true);
						}
						catch (Exception e)
						{
							Plugin.logger.LogError(e);
						}
					}
				}

				if (__instance.doors != null)
				{
					foreach (Door door in __instance.doors)
					{
						if (door == null)
							continue;

						try
						{
							door.Unlock();
						}
						catch (Exception e)
						{
							Plugin.logger.LogError(e);
						}
						
						if (door == __instance.doorForward)
						{
							try
							{
								door.Open(false, true);
							}
							catch (Exception e)
							{
								Plugin.logger.LogError(e);
							}
						}
					}
				}

				if (__instance.killChallenge)
					ChallengeManager.Instance.ChallengeDone();

				UnityEngine.Object.Destroy(__instance);
			}

			return false;
		}
	}
}
