using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMo
{
    [HarmonyPatch(typeof(EnemyIdentifier))]
	internal static class EnemyIdentifierPatches
    {
		private class ForceDisable : MonoBehaviour
		{
			private bool _inited = false;

			private void OnEnable()
			{
				if (_inited)
					gameObject.SetActive(false);
			}

			private void Start()
			{
				_inited = true;
				gameObject.SetActive(false);
			}
		}

		[HarmonyPatch(nameof(EnemyIdentifier.Start))]
		[HarmonyPrefix]
		public static void NoSpawnIn(EnemyIdentifier __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
				return;

			__instance.spawnIn = false;
		}

		[HarmonyPatch(nameof(EnemyIdentifier.Start))]
		[HarmonyPostfix]
        public static void DisableSpawnInOnNoMo(EnemyIdentifier __instance)
        {
            if (!AngrySceneManager.isInCustomLevel || !AngryGamemodeManager.NoMonsters)
                return;

			if (__instance.enemyType == EnemyType.Deathcatcher)
				return;

			NoMoCommon.TriggerEnemyEvents(__instance.gameObject);
			__instance.gameObject.AddComponent<ForceDisable>();
		}
    }
}
