using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace AngryLevelLoader.Patches.NoMo
{
	[HarmonyPatch(typeof(ActivateNextWaveHP))]
	public static class ActivateNextWaveHPPatches
	{
		[HarmonyPatch(nameof(ActivateNextWaveHP.Update))]
		[HarmonyPrefix]
		public static bool PreUpdate(ActivateNextWaveHP __instance)
		{
			static void Activate(ActivateNextWaveHP wave)
			{
				foreach (var obj in wave.toActivate)
					if (obj != null)
					{
						try
						{
							obj.SetActive(true);
						}
						catch (Exception e)
						{
							Debug.LogException(e);
						}
					}

				if (wave.lastWave)
				{
					foreach (var door in wave.doors)
					{
						if (door == null)
							continue;

						try
						{
							door.Unlock();
						}
						catch (Exception e)
						{
							Debug.LogException(e);
						}

						if (door == wave.doorForward)
						{
							try
							{
								door.Open(false, true);
							}
							catch (Exception e)
							{
								Debug.LogException(e);
							}
						}
					}
				}
				else
				{
					foreach (var enemy in wave.nextEnemies)
						if (enemy != null)
						{
							try
							{
								enemy.SetActive(true);
							}
							catch (Exception e)
							{
								Debug.LogException(e);
							}
						}
				}

				UnityEngine.Object.Destroy(wave);
			}

			if (!AngrySceneManager.isInCustomLevel || !(Plugin.NoMo || Plugin.NoMoW))
				return true;

			if (__instance.target == null)
			{
				Activate(__instance);
			}
			else if (__instance.target.health <= __instance.health)
			{
				Activate(__instance);
			}

			return false;
		}
	}
}
