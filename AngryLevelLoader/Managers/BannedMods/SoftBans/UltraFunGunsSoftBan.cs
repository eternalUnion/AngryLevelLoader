using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UltraFunGuns;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	//[SoftBanClass]
	//public class UltraFunGunsSoftBan : SoftBan
	//{
	//	private static bool currentlyBanned = false;

	//	public override string ModGuid => "Hydraxous.ULTRAKILL.UltraFunGuns";

	//	public override string ModName => "Ultra Fun Guns";

	//	private static void OnWeaponRedeploy()
	//	{
	//		var loadout = UltraFunGuns.Data.Loadout.Data;

	//		foreach (var slot in loadout.slots)
	//		{
	//			foreach (var node in slot.slotNodes)
	//			{
	//				if (node.weaponUnlocked && node.weaponEnabled)
	//				{
	//					currentlyBanned = true;
	//					return;
	//				}
	//			}
	//		}
	//	}

	//	public override void Init()
	//	{
	//		SceneManager.sceneLoaded += (scene, mode) =>
	//		{
	//			if (mode == LoadSceneMode.Additive)
	//				return;

	//			currentlyBanned = false;
	//		};

	//		Plugin.harmony.Patch(typeof(GunSetter).GetMethod(nameof(GunSetter.ResetWeapons), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
	//			postfix: new HarmonyLib.HarmonyMethod(typeof(UltraFunGunsSoftBan).GetMethod(nameof(OnWeaponRedeploy), BindingFlags.Static | BindingFlags.NonPublic)));

	//		Plugin.harmony.Patch(typeof(WeaponManager).GetMethod(nameof(WeaponManager.DeployWeapons), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
	//			postfix: new HarmonyLib.HarmonyMethod(typeof(UltraFunGunsSoftBan).GetMethod(nameof(OnWeaponRedeploy), BindingFlags.Static | BindingFlags.NonPublic)));
	//	}

	//	public override SoftBanCheckResult Check()
	//	{
	//		SoftBanCheckResult result = new SoftBanCheckResult();

	//		var loadout = UltraFunGuns.Data.Loadout.Data;
			
	//		foreach (var slot in loadout.slots)
	//		{
	//			foreach (var node in slot.slotNodes)
	//			{
	//				if (node.weaponUnlocked && node.weaponEnabled)
	//				{
	//					result.banned = true;

	//					if (!string.IsNullOrEmpty(result.message))
	//						result.message += '\n';
	//					result.message += $"- Gun {node.weaponKey} is banned, unequip to be able to post records";
	//				}
	//			}
	//		}

	//		if (currentlyBanned && string.IsNullOrEmpty(result.message))
	//		{
	//			result.banned = true;
	//			result.message += $"At least one weapon was enabled during the level. Restart the level without ever picking up a custom weapon.";
	//		}

	//		currentlyBanned = result.banned;
	//		return result;
	//	}
	//}
}
