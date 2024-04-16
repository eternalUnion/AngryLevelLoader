using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	public static class V3LegacyPatches
	{
	}

	public static class V3LegacyEnemyPatches
	{
		private static ZombieMelee filth;
		private static Transform biteTrailObj;
		private static Transform diveTrailObj;
		private static Transform diveSwingCheckObj;

		private static SpiderBody spiderBody;

		private static SwordsMachine swordsMachine;
		private static Transform slapCheck;

		internal static void Init()
		{
			filth = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Zombie.prefab").WaitForCompletion().GetComponentInChildren<ZombieMelee>(true);
			biteTrailObj = filth.transform.Find("ZombieFilth/Armature.001/Bone001/Spine_01/Spine_02/Neck/Head/BiteTrail");
			diveTrailObj = filth.transform.Find("ZombieFilth/Armature.001/Bone001/Spine_01/Spine_02/DiveTrail");
			diveSwingCheckObj = filth.transform.Find("ZombieFilth/Armature.001/Bone001/Spine_01/Spine_02/Neck/Head/DiveSwingCheck");
			
			spiderBody = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Spider.prefab").WaitForCompletion().GetComponentInChildren<SpiderBody>(true);

			swordsMachine = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/SwordsMachineNonboss.prefab").WaitForCompletion().GetComponent<SwordsMachine>();
			slapCheck = swordsMachine.transform.Find("SlapCheck");
		}

		public static bool FixStreetCleaner(Streetcleaner __instance)
		{
			if (__instance.aimBone == null)
				__instance.aimBone = __instance.transform.Find("flameboi2rig2/Armature/spine1/spine2");
			return true;
		}

		public static bool FixSpider(SpiderBody __instance)
		{
			if (__instance.spiderBeam != null && __instance.spiderBeam.TryGetComponent(out RevolverBeam beam))
			{
				if (beam.hitParticle == null)
					__instance.spiderBeam = spiderBody.spiderBeam;
			}

			return true;
		}

		public static bool FixFilth(ZombieMelee __instance)
		{
			if (__instance.biteTrail == null && biteTrailObj != null)
			{
				GameObject biteTrailInstance = GameObject.Instantiate(biteTrailObj.gameObject, __instance.transform.Find("ZombieFilth/Armature.001/Bone001/Spine_01/Spine_02/Neck/Head"));
				biteTrailInstance.transform.localPosition = biteTrailObj.localPosition;
				biteTrailInstance.transform.localRotation = biteTrailObj.localRotation;
				__instance.biteTrail = biteTrailInstance.GetComponent<TrailRenderer>();
			}

			if (__instance.diveTrail == null && diveTrailObj != null)
			{
				GameObject diveTrailInstance = GameObject.Instantiate(diveTrailObj.gameObject, __instance.transform.Find("ZombieFilth/Armature.001/Bone001/Spine_01/Spine_02"));
				diveTrailInstance.transform.localPosition = diveTrailObj.localPosition;
				diveTrailInstance.transform.localRotation = diveTrailObj.localRotation;
				__instance.diveTrail = diveTrailInstance.GetComponent<TrailRenderer>();
			}

			if (__instance.swingCheck == null)
				__instance.swingCheck = __instance.transform.Find("SwingCheck").GetComponent<SwingCheck2>();

			if (__instance.diveSwingCheck == null && diveSwingCheckObj != null)
			{
				GameObject diveSwingCheckInstance = GameObject.Instantiate(diveSwingCheckObj.gameObject, __instance.transform.Find("ZombieFilth/Armature.001/Bone001/Spine_01/Spine_02/Neck/Head"));
				diveSwingCheckInstance.transform.localPosition = diveSwingCheckObj.localPosition;
				diveSwingCheckInstance.transform.localRotation = diveSwingCheckObj.localRotation;
				__instance.diveSwingCheck = diveSwingCheckInstance.GetComponent<SwingCheck2>();
			}

			if (__instance.modelTransform == null)
				__instance.modelTransform = __instance.transform.Find("ZombieFilth");

			if (__instance.hitGroundParticle == null)
				__instance.hitGroundParticle = filth.hitGroundParticle;

			if (__instance.pullOutParticle == null)
				__instance.pullOutParticle = filth.pullOutParticle;

			return true;
		}

		public static void FixSwordsmachine(SwordsMachine __instance)
		{
			if (__instance.swordSwingCheck == null)
				__instance.swordSwingCheck = __instance.GetComponentsInChildren<SwingCheck2>(true).ToArray();

			if (__instance.aimBones == null)
				__instance.aimBones = new Transform[2]
				{
					__instance.transform.Find("Swordsmachine_New/Armature/Control/Waist/Chest"),
					__instance.transform.Find("Swordsmachine_New/Armature/Control/Waist/Chest/UArm_R")
				};

			if (__instance.slapSwingCheck == null && slapCheck != null)
			{
				GameObject slapCheckInstance = GameObject.Instantiate(slapCheck.gameObject, __instance.transform);
				slapCheckInstance.transform.localPosition = slapCheck.localPosition;
				slapCheckInstance.transform.localRotation = slapCheck.localRotation;
				__instance.slapSwingCheck = slapCheckInstance.GetComponent<SwingCheck2>();
			}

			if (__instance.slapTrail == null)
			{
				Transform trailParent = __instance.transform.Find("Swordsmachine_New/Armature/Control/Waist/Chest/UArm_L/LArm_L/Hand_L");
				TrailRenderer slapTrail = trailParent.gameObject.AddComponent<TrailRenderer>();

				static T CopyTo<T>(T from, Component to) where T : Component
				{
					Type type = to.GetType();
					if (type != from.GetType()) return null; // type mis-match
					BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
					PropertyInfo[] pinfos = type.GetProperties(flags);
					foreach (var pinfo in pinfos)
					{
						if (pinfo.CanWrite)
						{
							try
							{
								pinfo.SetValue(to, pinfo.GetValue(from, null), null);
							}
							catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
						}
					}
					FieldInfo[] finfos = type.GetFields(flags);
					foreach (var finfo in finfos)
					{
						finfo.SetValue(to, finfo.GetValue(from));
					}
					return to as T;
				}
				CopyTo(swordsMachine.slapTrail, slapTrail);

				__instance.slapTrail = slapTrail;
			}
		}
	}

	public static class V3LegacyRevolverBeamPatches
	{
		private static GameObject hitParticle;

		public static bool FixBeam(RevolverBeam __instance)
		{
			if (__instance.hitParticle == null)
			{
				if (hitParticle == null)
					hitParticle = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam.prefab").WaitForCompletion().GetComponent<RevolverBeam>().hitParticle;

				__instance.hitParticle = hitParticle;
			}

			return true;
		}
	}
}
