using AngryLevelLoader.Extensions;
using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMo
{
    [HarmonyPatch(typeof(ActivateArena))]
    public static class ActivateArenaPatches
    {
        [HarmonyPatch(nameof(ActivateArena.Activate))]
        [HarmonyPrefix]
        public static bool InstantNomoActivasion(ActivateArena __instance)
        {
            if (!(Plugin.NoMo || Plugin.NoMoW) || !AngrySceneManager.isInCustomLevel)
                return true;

			foreach (var door in __instance.doors)
                if (door != null)
                {
                    try
                    {
                        door.Lock();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

            HashSet<ActivateNextWave> processedWaves = new HashSet<ActivateNextWave>();
            HashSet<ActivateNextWave> currentWaves = new HashSet<ActivateNextWave>();

            foreach (var enemy in __instance.enemies)
            {
                if (enemy == null)
                    continue;

                ActivateNextWave nextWave = enemy.GetComponentInParent<ActivateNextWave>(true);
                if (nextWave != null)
                {
                    foreach (var childWave in nextWave.gameObject.GetComponents<ActivateNextWave>())
                        currentWaves.Add(childWave);
                }
                
                try
                {
                    enemy.SetActive(true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            while (currentWaves.Count != 0)
            {
                foreach (var wave in currentWaves)
                    processedWaves.Add(wave);

                HashSet<ActivateNextWave> nextWaves = new HashSet<ActivateNextWave>();

                foreach (var wave in currentWaves)
                {
					foreach (var toActivate in wave.toActivate)
                    {
                        if (toActivate == null)
                            continue;

                        try
                        {
                            toActivate.SetActive(true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }

                        bool checkForParentWave = true;

						if (toActivate.GetComponentInChildren<EnemyIdentifier>() != null)
                        {
                            // Enemy specific code if needed
						}
                        else if (toActivate.GetComponentInChildren<StatueActivator>() == null && toActivate.GetComponentInChildren<DeathMarker>() == null)
                        {
							checkForParentWave = false;
                        }

						if (checkForParentWave)
                        {
                            ActivateNextWave activatedObjectWave = toActivate.GetComponentInParent<ActivateNextWave>(true);
                            if (activatedObjectWave != null)
                            {
                                foreach (var childWave in activatedObjectWave.gameObject.GetComponents<ActivateNextWave>().Where(wave => !processedWaves.Contains(wave)))
                                    nextWaves.Add(childWave);
                            }
                        }
                    }

                    foreach (var door in wave.doors)
                    {
                        if (door != null)
                        {
                            try
                            {
                                door.Unlock();
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                    }

                    foreach (var enemy in wave.nextEnemies)
                    {
                        if (enemy == null)
                            continue;

                        try
                        {
                            enemy.SetActive(true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }

                        ActivateNextWave nextWave = enemy.GetComponentInParent<ActivateNextWave>(true);
                        if (nextWave != null)
                        {
                            foreach (var childWave in nextWave.gameObject.GetComponents<ActivateNextWave>().Where(wave => !processedWaves.Contains(wave)))
                                nextWaves.Add(childWave);
                        }
					}

                    UnityEngine.Object.Destroy(wave);
                }

                currentWaves = nextWaves;
            }

            UnityEngine.Object.Destroy(__instance);
            return false;
        }
    }
}
