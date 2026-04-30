using HarmonyLib;
using System;
using System.Linq;
using ULTRAKILL.Portal;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	[LegacyPatch(LegacyPatchState.V6)]
	internal static class V6LegacyScriptPatches
	{
		[HarmonyPatch(typeof(MovingPlatform), nameof(MovingPlatform.Start))]
		[HarmonyPrefix]
		private static void PatchMovingPlatform(MovingPlatform __instance)
		{
			__instance.moveOnEnable = true;
		}
	}

	[LegacyPatch(LegacyPatchState.V6)]
	internal static class V6LegacyEnemyPatches
	{
		private static LazyAddressableAsset<GameObject> MINDFLAYER_BEAM = new LazyAddressableAsset<GameObject>("Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Mindflayer Beam.prefab");

		private static LazyAddressableAsset<GameObject> MALICIOUS_FACE = new LazyAddressableAsset<GameObject>("Assets/Prefabs/Enemies/Malicious Face.prefab");

		private static LazyAsset<AssetReference> MALICIOUS_FACE_EXPLOSION = new LazyAsset<AssetReference>(() =>
		{
			GameObject maliciousFace = MALICIOUS_FACE.Get();
			MaliciousFace mf = maliciousFace.GetComponentInChildren<MaliciousFace>(true);
			return mf.beamExplosion;
		});

		private static LazyAsset<AssetReference> MALICIOUS_FACE_SHOCKWAVE = new LazyAsset<AssetReference>(() =>
		{
			GameObject maliciousFace = MALICIOUS_FACE.Get();
			MaliciousFace mf = maliciousFace.GetComponentInChildren<MaliciousFace>(true);
			return mf.shockwave;
		});

		private static Enemy MachineToEnemy(Machine machine, EnemyIdentifier eid)
		{
			machine.gameObject.SetActive(false);
			Enemy enemy = machine.gameObject.AddComponent<Enemy>();
			eid.machine = enemy;
			enemy.isMachine = true;

			enemy.health = machine.health;
			enemy.noFallDamage = machine.noFallDamage;
			enemy.dontDie = machine.dontDie;
			enemy.specialDeath = machine.specialDeath;
			enemy.simpleDeath = machine.simpleDeath;
			enemy.dismemberment = machine.dismemberment;
			enemy.knockedBack = machine.knockedBack;
			enemy.brakes = machine.brakes;
			enemy.juggleWeight = machine.juggleWeight;
			enemy.parryable = machine.parryable;
			enemy.partiallyParryable = machine.partiallyParryable;
			enemy.hurtSounds = machine.hurtSounds;
			enemy.deathSound = machine.deathSound;
			enemy.scream = machine.scream;
			enemy.chest = machine.chest;
			enemy.smr = machine.smr;
			enemy.deadMaterial = machine.deadMaterial;
			enemy.destroyOnDeath = machine.destroyOnDeath;
			enemy.bigKill = machine.bigKill;
			enemy.thickLimbs = machine.thickLimbs;
			enemy.onDeath = machine.onDeath;

			machine.gameObject.SwapComponents(machine.GetComponentIndex(), enemy.GetComponentIndex());
			machine.enabled = false;
			return enemy;
		}

		private static Enemy StatueToEnemy(Statue statue, EnemyIdentifier eid)
		{
			statue.gameObject.SetActive(false);
			Enemy enemy = statue.gameObject.AddComponent<Enemy>();
			eid.statue = enemy;
			enemy.isStatue = true;

			enemy.health = statue.health;
			enemy.chest = statue.chest;
			enemy.hurtSounds = statue.hurtSounds;
			enemy.deadMaterial = statue.deadMaterial;
			enemy.woundedMaterial = statue.woundedMaterial;
			enemy.woundedEnrageMaterial = statue.woundedEnrageMaterial;
			enemy.woundedParticle = statue.woundedParticle;
			enemy.woundedModel = statue.woundedModel;
			enemy.smr = statue.smr;
			enemy.deathSound = statue.deathSound;
			enemy.extraDamageZones = statue.extraDamageZones;
			enemy.extraDamageMultiplier = statue.extraDamageMultiplier;
			enemy.brakes = statue.brakes;
			enemy.juggleWeight = statue.juggleWeight;
			enemy.bigBlood = statue.bigBlood;
			enemy.isMassDeath = statue.isMassDeath;
			enemy.specialDeath = statue.specialDeath;

			statue.gameObject.SwapComponents(statue.GetComponentIndex(), enemy.GetComponentIndex());
			statue.enabled = false;
			return enemy;
		}

		private static Enemy ZombieToEnemy(Zombie zombie, EnemyIdentifier eid)
		{
			zombie.gameObject.SetActive(false);
			Enemy enemy = zombie.gameObject.AddComponent<Enemy>();
			eid.zombie = enemy;
			enemy.isZombie = true;

			enemy.health = zombie.health;
			enemy.noFallDamage = zombie.noFallDamage;
			enemy.variableSpeed = true;
			enemy.hurtSounds = zombie.hurtSounds;
			enemy.deathSound = zombie.deathSound;
			enemy.scream = zombie.scream;
			enemy.hurtSoundVol = zombie.hurtSoundVol;
			enemy.deathSoundVol = zombie.deathSoundVol;
			enemy.chest = zombie.chest;
			enemy.brakes = zombie.brakes;
			enemy.juggleWeight = zombie.juggleWeight;
			enemy.smr = zombie.smr;
			enemy.deadMaterial = zombie.deadMaterial;

			zombie.gameObject.SwapComponents(zombie.GetComponentIndex(), enemy.GetComponentIndex());
			zombie.enabled = false;
			return enemy;
		}

		[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Awake))]
		[HarmonyPrefix]
		private static bool PatchPreAwakeEnemyIdentifier(EnemyIdentifier __instance)
		{
			Enemy enemy;
			Drone drone;
			Machine machine;
			Statue statue;
			Zombie zombie;
			SpiderBody spider;

			try
			{
				switch (__instance.enemyType)
				{
					case EnemyType.Drone:
						__instance.gameObject.SetActive(false);

						enemy = __instance.gameObject.AddComponent<Enemy>();
						enemy.isDrone = true;
						enemy.hurtSounds = new AudioClip[0];
						enemy.health = __instance.health;
						drone = __instance.gameObject.GetComponent<Drone>();
						if (__instance.gameObject.GetComponent<DroneFlesh>() == null)
						{
							enemy.brakes = 1;
						}
						if (drone != null)
						{
							enemy.hurtSound = drone.hurtSound;
							enemy.deathSound = drone.deathSound;
						}

						break;

					case EnemyType.Virtue:
						__instance.gameObject.SetActive(false);

						enemy = __instance.gameObject.AddComponent<Enemy>();
						enemy.isDrone = true;
						enemy.hurtSounds = new AudioClip[0];
						enemy.health = __instance.health;

						if (__instance.bigEnemy)
							enemy.brakes = 1;

						drone = __instance.gameObject.GetComponent<Drone>();
						if (drone != null)
						{
							enemy.hurtSound = drone.hurtSound;
							enemy.deathSound = drone.deathSound;
							enemy.enrageEffect = drone.enrageEffect;
						}
						if (__instance.GetComponent<DroneFlesh>() != null)
						{
							enemy.health = 5;
						}

						break;

					case EnemyType.Ferryman:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
						{
							enemy = MachineToEnemy(machine, __instance);
							enemy.overrideFalling = true;
						}

						break;

					case EnemyType.FleshPrison:
						statue = __instance.GetComponent<Statue>();
						if (statue != null)
							StatueToEnemy(statue, __instance);
						break;

					case EnemyType.FleshPanopticon:
						statue = __instance.GetComponent<Statue>();
						if (statue != null)
							StatueToEnemy(statue, __instance);
						break;

					case EnemyType.Gutterman:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Guttertank:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Mannequin:
						statue = __instance.GetComponent<Statue>();
						if (statue != null)
							StatueToEnemy(statue, __instance);
						break;

					case EnemyType.HideousMass:
						statue = __instance.GetComponent<Statue>();
						if (statue != null)
							StatueToEnemy(statue, __instance);

						if (__instance.gameObject.TryGetComponent(out Mass mass))
						{
							foreach (EnemySimplifier simplifier in __instance.gameObject.GetComponentsInChildren<EnemySimplifier>(true))
							{
								simplifier.enragedMaterial = mass.enrageMaterial;
								simplifier.enemyScriptHandlesEnrage = false;
							}
						}
						break;

					case EnemyType.Mindflayer:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.MinosPrime:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
						{
							enemy = MachineToEnemy(machine, __instance);
							enemy.overrideFalling = true;
						}
						break;

					case EnemyType.Minotaur:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Stray:
						zombie = __instance.GetComponent<Zombie>();
						if (zombie != null)
							ZombieToEnemy(zombie, __instance);
						break;

					case EnemyType.Puppet:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Soldier:
						zombie = __instance.GetComponent<Zombie>();
						if (zombie != null)
						{
							enemy = ZombieToEnemy(zombie, __instance);
							enemy.variableSpeed = false;
						}

						if (__instance.gameObject.TryGetComponent(out ZombieProjectiles zombieProjectiles))
						{
							zombieProjectiles.chaser = true;
						}
						break;

					case EnemyType.Sisyphus:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
						{
							enemy = MachineToEnemy(machine, __instance);
							enemy.overrideFalling = true;
						}
						break;

					case EnemyType.SisyphusPrime:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.MaliciousFace:
						__instance.gameObject.SetActive(false);

						__instance.transform.parent.gameObject.AddComponent<PortalAwareRenderer>();
						enemy = __instance.gameObject.AddComponent<Enemy>();
						spider = __instance.gameObject.GetComponent<SpiderBody>();
						enemy.isSpider = true;
						enemy.hurtSounds = new AudioClip[0];
						enemy.brakes = 1;
						enemy.health = spider.health;

						if (spider != null)
						{
							enemy.hurtSound = spider.hurtSound;
							enemy.woundedMaterial = spider.woundedMaterial;
							enemy.woundedEnrageMaterial = spider.woundedEnrageMaterial;
							enemy.woundedParticle = spider.woundedParticle;
							enemy.enrageEffect = spider.enrageEffect;
							spider.enabled = false;
							__instance.gameObject.SwapComponents(enemy.GetComponentIndex(), spider.GetComponentIndex());
						}

						enemy.bodyCenter = __instance.transform;

						// Add the new component
						MaliciousFace mf = __instance.gameObject.AddComponent<MaliciousFace>();
						__instance.spider = mf;
						mf.spider = enemy;
						mf.proj = spider.proj;
						mf.spark = spider.spark;
						mf.spiderBeam = spider.spiderBeam;
						mf.beamExplosion = MALICIOUS_FACE_EXPLOSION.Get();
						mf.shockwave = MALICIOUS_FACE_SHOCKWAVE.Get();

						mf.breakParticle = spider.breakParticle;
						mf.impactParticle = spider.impactParticle;
						mf.impactSprite = spider.impactSprite;
						mf.dripBlood = spider.dripBlood;
						mf.chargeEffect = spider.chargeEffect;
						mf.enrageEffect = spider.enrageEffect;
						mf.hurtSound = spider.hurtSound;

						mf.spiderStationary = spider.stationary;
						mf.spiderTargetHeight = spider.targetHeight;
						mf.mouth = spider.mouth;
						mf.sparkRotationOffset = Vector3.up * 90;
						mf.headModel = spider.headModel;
						if (mf.headModel == null)
							mf.headModel = spider.transform.GetChild(0);
						mf.headCollider = spider.headCollider;
						if (mf.headCollider == null)
							mf.headCollider = spider.headModel.GetChild(0).GetComponent<Collider>();
						mf.mainMesh = spider.mainMesh;
						SpiderLegsController legController = spider.transform.parent.GetComponentInChildren<SpiderLegsController>(true);
						if (legController != null)
							mf.legController = legController.gameObject;
						mf.legs = spider.transform.parent.gameObject.GetComponentsInChildren<SpiderLegLines>(true).Select(l => l.gameObject).Concat(spider.transform.parent.gameObject.GetComponentsInChildren<SpiderLeg>(true).Select(l => l.gameObject)).ToArray();
						mf.woundedMaterial = spider.woundedMaterial;
						mf.woundedEnrageMaterial = spider.woundedEnrageMaterial;
						mf.woundedParticle = spider.woundedParticle;

						break;

					case EnemyType.Stalker:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Streetcleaner:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Schism:
						zombie = __instance.GetComponent<Zombie>();
						if (zombie != null)
							ZombieToEnemy(zombie, __instance);
						break;

					case EnemyType.Swordsmachine:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Turret:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
						{
							enemy = MachineToEnemy(machine, __instance);
							if (__instance.gameObject.TryGetComponent(out Turret turret))
								turret.mach = enemy;
						}
						break;

					case EnemyType.V2:
						machine = __instance.GetComponent<Machine>();
						if (machine != null)
							MachineToEnemy(machine, __instance);
						break;

					case EnemyType.Filth:
						zombie = __instance.GetComponent<Zombie>();
						if (zombie != null)
							ZombieToEnemy(zombie, __instance);
						break;
				}

				// Awake script

				if (__instance.puppet)
				{
					__instance.permaPuppet = true;
				}
				__instance.health = 999f;
				__instance.InitializeReferences();
				__instance.ForceGetHealth();
				__instance.UpdateModifiers();
				if (StockMapInfo.Instance != null && StockMapInfo.Instance.forceUpdateEnemyRenderers)
				{
					SkinnedMeshRenderer[] componentsInChildren = __instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						componentsInChildren[i].updateWhenOffscreen = true;
					}
				}
				__instance.bsm = BloodsplatterManager.Instance;
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}

			if (!__instance.gameObject.activeSelf)
				__instance.gameObject.SetActive(true);
			return false;
		}

		[HarmonyPatch(typeof(Turret), nameof(Turret.Start))]
		[HarmonyPrefix]
		private static void PatchTurretStart(Turret __instance)
		{
			__instance.barrelTip = __instance.transform.Find("TurretBot/Armature/Root/Pelvis/Spine_01/Spine_02/Torso/Neck/Head_Main/Barrel/Barrel_end");
		}

		[HarmonyPatch(typeof(Mindflayer), nameof(Mindflayer.Start))]
		[HarmonyPrefix]
		private static void PatchMindflayerStart(Mindflayer __instance)
		{
			// Small hack
			__instance.lr = __instance.GetComponent<LineRenderer>();
			__instance.sweepLineRenderers = new LineRenderer[1] { __instance.lr };
			__instance.beam = MINDFLAYER_BEAM.Get().GetComponent<ContinuousBeam>();
		}
	}
}
