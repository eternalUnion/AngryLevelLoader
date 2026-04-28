using HarmonyLib;
using UnityEngine;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	[LegacyPatch(LegacyPatchState.V7)]
	internal static class V7LegacyPlayerPatches
	{
		private static LazyAddressableAsset<GameObject> PLAYER_PREFAB = new LazyAddressableAsset<GameObject>("Assets/Prefabs/Player/Player.prefab");

		[HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Start))]
		[HarmonyPrefix]
		private static void PatchPlayer(NewMovement __instance)
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
