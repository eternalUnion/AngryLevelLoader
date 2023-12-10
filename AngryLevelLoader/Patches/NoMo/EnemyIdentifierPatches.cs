using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMo
{
    [HarmonyPatch(typeof(EnemyIdentifier))]
    public static class EnemyIdentifierPatches
    {
		private class ForceDisable : MonoBehaviour
		{
			private void OnEnable()
			{
				gameObject.SetActive(false);
			}
		}

		[HarmonyPatch(nameof(EnemyIdentifier.Start))]
		[HarmonyPrefix]
        public static bool DisableSpawnInOnNoMo(EnemyIdentifier __instance)
        {
            if (!AngrySceneManager.isInCustomLevel || !Plugin.NoMonsters)
                return true;

            __instance.spawnIn = false;

			foreach (var obj in __instance.activateOnDeath)
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

			if (__instance.onDeath != null)
			{
				try
				{
					__instance.onDeath.Invoke();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

            __instance.dead = true;
            __instance.health = 0;
			__instance.gameObject.SetActive(false);

            switch (__instance.enemyType)
            {
                case EnemyType.MaliciousFace:
                    if (__instance.GetComponent<SpiderBody>() != null)
					{
						__instance.transform.parent.gameObject.SetActive(false);
						__instance.transform.parent.gameObject.AddComponent<ForceDisable>();
					}
                    break;

				case EnemyType.Cerberus:
					if (__instance.GetComponent<StatueBoss>() != null)
					{
						__instance.transform.parent.gameObject.SetActive(false);
						__instance.transform.parent.gameObject.AddComponent<ForceDisable>();
					}
					break;
			}

			__instance.gameObject.AddComponent<ForceDisable>();

			return false;
		}
    }
}
