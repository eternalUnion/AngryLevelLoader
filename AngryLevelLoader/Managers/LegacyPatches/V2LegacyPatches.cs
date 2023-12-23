using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	public static class V2LegacyPatches
	{
		public static string catalogPath;
	}

	public static class V2LegacyAudioPatches
	{
		private static AudioMixerGroup allAudio;
		private static AudioMixerGroup musicAudio;
		private static AudioMixerGroup goreAudio;
		private static AudioMixerGroup doorAudio;
		private static AudioMixerGroup unfreezableAudio;

		internal static void Init()
		{
			V2LegacyPatches.catalogPath = Path.Combine(Plugin.workingDir, "V2LegacyBundle");

			allAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("AllAudio").WaitForCompletion();
			musicAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("MusicAudio").WaitForCompletion();
			goreAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("GoreAudio").WaitForCompletion();
			doorAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("DoorAudio").WaitForCompletion();
			unfreezableAudio = Addressables.LoadAssetAsync<AudioMixerGroup>("UnfreezeableAudio").WaitForCompletion();

			string addressablePath = Path.Combine(Addressables.RuntimePath, "StandaloneWindows64");

			if (!File.Exists(Path.Combine(addressablePath, "other_assets_allaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_allaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_allaudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_dooraudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_dooraudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_dooraudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_goreaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_goreaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_goreaudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_musicaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_musicaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_musicaudio.bundle"));
			if (!File.Exists(Path.Combine(addressablePath, "other_assets_unfreezeableaudio.bundle")))
				File.WriteAllBytes(Path.Combine(addressablePath, "other_assets_unfreezeableaudio.bundle"), ManifestReader.GetBytes("V2PatchBundles.other_assets_unfreezeableaudio.bundle"));

			Addressables.LoadContentCatalogAsync(Path.Combine(Plugin.workingDir, "V2LegacyBundle", "catalog.json")).WaitForCompletion();
			GameObject mixerContainer = Addressables.LoadAssetAsync<GameObject>("LegacyV2Patches/AudioMixerContainer").WaitForCompletion();
			foreach (AudioSource src in mixerContainer.GetComponents<AudioSource>())
			{
				if (src.outputAudioMixerGroup == null || src.outputAudioMixerGroup.audioMixer == null)
					continue;

				string mixerName = src.outputAudioMixerGroup.audioMixer.name;

				AudioMixerGroup toLink = null;
				if (mixerName == "AllAudio")
					toLink = allAudio;
				else if (mixerName == "GoreAudio")
					toLink = goreAudio;
				else if (mixerName == "UnfreezeableAudio")
					toLink = unfreezableAudio;
				else if (mixerName == "MusicAudio")
					toLink = musicAudio;
				else if (mixerName == "DoorAudio")
					toLink = doorAudio;
				else
					continue;

				src.outputAudioMixerGroup.audioMixer.outputAudioMixerGroup = toLink;
			}
		}
	}

	public static class V2LegacyEnemyPatches
	{
		private static Drone virtue;
		private static Drone drone;
		private static Drone mandalore;
		private static Drone goldenEye;
		private static Drone fleshDrone;

		private static StatueBoss statueBoss;
		private static Streetcleaner streetCleaner;
		private static SpiderBody spiderBody;
		private static SwordsMachine swordsMachine;
		private static Mindflayer mindflayer;
		private static Stalker stalker;

		internal static void Init()
		{
			virtue = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Virtue.prefab").WaitForCompletion().GetComponent<Drone>();
			drone = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Drone.prefab").WaitForCompletion().GetComponent<Drone>();
			mandalore = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Mandalore.prefab").WaitForCompletion().GetComponent<Drone>();
			goldenEye = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/DroneFleshCamera Variant.prefab").WaitForCompletion().GetComponent<Drone>();
			fleshDrone = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/DroneFlesh.prefab").WaitForCompletion().GetComponent<Drone>();

			statueBoss = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/StatueEnemy.prefab").WaitForCompletion().GetComponentInChildren<StatueBoss>(true);

			streetCleaner = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Streetcleaner.prefab").WaitForCompletion().GetComponent<Streetcleaner>();

			spiderBody = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Spider.prefab").WaitForCompletion().GetComponentInChildren<SpiderBody>(true);

			swordsMachine = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/SwordsMachine.prefab").WaitForCompletion().GetComponent<SwordsMachine>();

			mindflayer = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Mindflayer.prefab").WaitForCompletion().GetComponent<Mindflayer>();
		
			stalker = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Stalker.prefab").WaitForCompletion().GetComponent<Stalker>();
		}

		public static bool FixDrone(Drone __instance)
		{
			if (!__instance.gameObject.TryGetComponent(out EnemyIdentifier eid))
				return true;

			if (eid.enemyType == EnemyType.Virtue)
			{
				if (eid.TryGetComponent(out DroneFlesh fleshDrone))
				{
					if (__instance.explosion == null || string.IsNullOrEmpty(__instance.explosion.AssetGUID))
						__instance.explosion = goldenEye.explosion;

					if (__instance.gib == null || string.IsNullOrEmpty(__instance.gib.AssetGUID))
						__instance.gib = goldenEye.gib;
				}
				else
				{
					if (__instance.projectile == null || string.IsNullOrEmpty(__instance.projectile.AssetGUID))
						__instance.projectile = virtue.projectile;

					if (__instance.explosion == null || string.IsNullOrEmpty(__instance.explosion.AssetGUID))
						__instance.explosion = virtue.explosion;

					if (__instance.gib == null || string.IsNullOrEmpty(__instance.gib.AssetGUID))
						__instance.gib = virtue.gib;
				}
			}
			else if (eid.enemyType == EnemyType.Drone)
			{
				if (eid.TryGetComponent(out DroneFlesh _))
				{
					if (__instance.explosion == null || string.IsNullOrEmpty(__instance.explosion.AssetGUID))
						__instance.explosion = fleshDrone.explosion;

					if (__instance.gib == null || string.IsNullOrEmpty(__instance.gib.AssetGUID))
						__instance.gib = fleshDrone.gib;
				}
				else
				{
					if (__instance.projectile == null || string.IsNullOrEmpty(__instance.projectile.AssetGUID))
						__instance.projectile = drone.projectile;

					if (__instance.explosion == null || string.IsNullOrEmpty(__instance.explosion.AssetGUID))
						__instance.explosion = drone.explosion;

					if (__instance.gib == null || string.IsNullOrEmpty(__instance.gib.AssetGUID))
						__instance.gib = drone.gib;
				}
			}
			else if (eid.enemyType == EnemyType.Mandalore)
			{
				if (__instance.explosion == null || string.IsNullOrEmpty(__instance.explosion.AssetGUID))
					__instance.explosion = mandalore.explosion;

				if (__instance.gib == null || string.IsNullOrEmpty(__instance.gib.AssetGUID))
					__instance.gib = mandalore.gib;
			}

			return true;
		}
	
		public static bool FixStatueBoss(StatueBoss __instance)
		{
			if (__instance.stompWave == null || string.IsNullOrEmpty(__instance.stompWave.AssetGUID))
				__instance.stompWave = statueBoss.stompWave;

			if (__instance.orbProjectile == null || string.IsNullOrEmpty(__instance.orbProjectile.AssetGUID))
				__instance.orbProjectile = statueBoss.orbProjectile;

			return true;
		}

		public static bool FixStreetCleaner(Streetcleaner __instance)
		{
			if (__instance.explosion == null || string.IsNullOrEmpty(__instance.explosion.AssetGUID))
				__instance.explosion = streetCleaner.explosion;

			return true;
		}

		public static bool FixSpider(SpiderBody __instance)
		{
			if (__instance.spiderBeam != null && __instance.spiderBeam.TryGetComponent(out RevolverBeam beam))
			{
				if (beam.hitParticle == null || string.IsNullOrEmpty(beam.hitParticle.AssetGUID))
					__instance.spiderBeam = spiderBody.spiderBeam;
			}

			if (__instance.beamExplosion == null || string.IsNullOrEmpty(__instance.beamExplosion.AssetGUID))
				__instance.beamExplosion = spiderBody.beamExplosion;

			if (__instance.shockwave == null || string.IsNullOrEmpty(__instance.shockwave.AssetGUID))
				__instance.shockwave = spiderBody.shockwave;

			return true;
		}

		public static bool FixSwordsMachine(SwordsMachine __instance)
		{
			if (__instance.flash == null || string.IsNullOrEmpty(__instance.flash.AssetGUID))
				__instance.flash = swordsMachine.flash;

			if (__instance.gunFlash == null || string.IsNullOrEmpty(__instance.gunFlash.AssetGUID))
				__instance.gunFlash = swordsMachine.gunFlash;

			return true;
		}

		public static bool FixMindflayer(Mindflayer __instance)
		{
			if (__instance.deathExplosion == null || string.IsNullOrEmpty(__instance.deathExplosion.AssetGUID))
				__instance.deathExplosion = mindflayer.deathExplosion;

			return true;
		}


		public static bool FixStalker(Stalker __instance)
		{
			if (__instance.explosion == null || string.IsNullOrEmpty(__instance.explosion.AssetGUID))
				__instance.explosion = stalker.explosion;

			return true;
		}
	}

	public static class V2LegacyHookPointPatches
	{
		public static bool FixSlingshots(HookPoint __instance)
		{
			if (__instance.reachParticle != null)
				__instance.type = hookPointType.Slingshot;

			return true;
		}
	}

	public static class V2LegacyCheckpointPatches
	{
		private static CheckPoint checkpoint;

		public static bool FixCheckpoint(CheckPoint __instance)
		{
			if (checkpoint == null)
				checkpoint = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Levels/Checkpoint.prefab").WaitForCompletion().GetComponent<CheckPoint>();

			if (__instance.activateEffect == null || string.IsNullOrEmpty(__instance.activateEffect.AssetGUID))
				__instance.activateEffect = checkpoint.activateEffect;

			return true;
		}
	}

	public static class V2LegacyRevolverBeamPatches
	{
		private static AssetReference hitParticle = null;

		public static bool FixBeam(RevolverBeam __instance)
		{
			if (__instance.hitParticle == null || string.IsNullOrEmpty(__instance.hitParticle.AssetGUID))
			{
				if (hitParticle == null)
					hitParticle = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam.prefab").WaitForCompletion().GetComponent<RevolverBeam>().hitParticle;

				__instance.hitParticle = hitParticle;
			}

			return true;
		}
	}
}
