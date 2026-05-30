using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMo
{
    [HarmonyPatch(typeof(ActivateArena))]
	internal static class ActivateArenaPatches
    {
        [HarmonyPatch(nameof(ActivateArena.Activate))]
        [HarmonyPrefix]
        public static bool InstantNomoActivasion(ActivateArena __instance)
        {
            if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters || __instance.activated)
                return true;

            __instance.activated = true;

			foreach (var door in __instance.doors)
            {
                if (door == null)
                    continue;
                    
                if (!door.gameObject.activeSelf)
                {
                    try
                    {
                        door.gameObject.SetActive(true);
                    }
                    catch (Exception e)
                    {
                        Plugin.logger.LogError(e);
                    }
                }

                try
                {
                    door.Lock();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            foreach (GameObject enemy in __instance.enemies)
            {
                NoMoCommon.TriggerEnemyEvents(enemy);
            }

            UnityEngine.Object.Destroy(__instance);
            return false;
        }
    }
}
