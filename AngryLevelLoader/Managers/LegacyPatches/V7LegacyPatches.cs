using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static AngryLevelLoader.Managers.LegacyPatches.LegacyPatchManager;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	public static class V7LegacyPlayerPatches
	{
		private static LazyAddressableAsset<GameObject> PLAYER_PREFAB = new LazyAddressableAsset<GameObject>("Assets/Prefabs/Player/Player.prefab");

		public static void Patch(Harmony harmony)
		{
			harmony.Patch(typeof(NewMovement).GetMethod(nameof(NewMovement.Start), INSTANCE), prefix: new HarmonyMethod(typeof(V7LegacyPlayerPatches).GetMethod(nameof(PatchPlayer), STATIC)));
		}

		public static void PatchPlayer(NewMovement __instance)
		{
			if (__instance.windStateParticle == null)
			{
				// Old custom player, need to instantiate the particle
				GameObject player = PLAYER_PREFAB.Get();
				if (player != null && player.TryGetComponent(out NewMovement playerMovement) && playerMovement.windStateParticle != null)
				{
					GameObject windStateGo = UnityEngine.Object.Instantiate(playerMovement.windStateParticle.gameObject);
					windStateGo.transform.SetParent(__instance.transform);
					windStateGo.transform.localPosition = playerMovement.windStateParticle.transform.localPosition;
					windStateGo.transform.localRotation = playerMovement.windStateParticle.transform.localRotation;
					windStateGo.transform.localScale = playerMovement.windStateParticle.transform.localScale;

					__instance.windStateParticle = windStateGo.GetComponent<ParticleSystem>();
				}
			}
		}
	}
}
