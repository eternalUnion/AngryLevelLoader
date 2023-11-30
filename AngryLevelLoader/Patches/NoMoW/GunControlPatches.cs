using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches.NoMoW
{
	[HarmonyPatch(typeof(GunControl))]
	public static class GunControlPatches
	{
		[HarmonyPatch(nameof(GunControl.ForceWeapon))]
		[HarmonyPrefix]
		public static bool PreventForceGun()
		{
			if (!AngrySceneManager.isInCustomLevel || !Plugin.NoMoW)
				return true;

			return false;
		}

		[HarmonyPatch(nameof(GunControl.UpdateWeaponList))]
		[HarmonyPrefix]
		public static bool PreventWeapons(GunControl __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !Plugin.NoMoW)
				return true;

			__instance.noWeapons = true;
			for (int i = 0; i < __instance.slots.Count; i++)
			{
				foreach (GameObject gameObject in __instance.slots[i])
				{
					if (gameObject != null)
					{
						UnityEngine.Object.Destroy(gameObject);
					}
				}

				__instance.slots[i].Clear();
			}

			return true;
		}
	}
}
