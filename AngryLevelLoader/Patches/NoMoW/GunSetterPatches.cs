using AngryLevelLoader.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Patches.NoMoW
{
	[HarmonyPatch(typeof(GunSetter))]
	public static class GunSetterPatches
	{
		[HarmonyPatch(nameof(GunSetter.ResetWeapons))]
		[HarmonyPrefix]
		public static bool RemoveGuns(GunSetter __instance)
		{
			if (!AngrySceneManager.isInCustomLevel || !Plugin.NoMoW)
				return true;

			VariantSetting disabledVariant = new VariantSetting()
			{
				blueVariant = VariantOption.ForceOff,
				greenVariant = VariantOption.ForceOff,
				redVariant = VariantOption.ForceOff,
			};

			ArmVariantSetting allowedArmVariant = new ArmVariantSetting()
			{
				blueVariant = VariantOption.IfEquipped,
				greenVariant = VariantOption.IfEquipped,
				redVariant = VariantOption.IfEquipped,
			};

			__instance.forcedLoadout = new ForcedLoadout()
			{
				revolver = disabledVariant,
				shotgun = disabledVariant,
				nailgun = disabledVariant,
				railcannon = disabledVariant,
				rocketLauncher = disabledVariant,

				altRevolver = disabledVariant,
				altNailgun = disabledVariant,

				arm = allowedArmVariant
			};

			return true;
		}

		[HarmonyPatch(nameof(GunSetter.ForceWeapon))]
		[HarmonyPrefix]
		public static bool NoForcedWeapons()
		{
			if (!AngrySceneManager.isInCustomLevel || !Plugin.NoMoW)
				return true;

			return false;
		}
	}
}
