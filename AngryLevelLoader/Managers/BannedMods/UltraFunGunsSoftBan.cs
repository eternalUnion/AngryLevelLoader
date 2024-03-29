﻿using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class UltraFunGunsSoftBan
	{
		public const string PLUGIN_GUID = "Hydraxous.ULTRAKILL.UltraFunGuns";
        public const string CONFIGGY_LIB_GUID = "Hydraxous.ULTRAKILL.Configgy";


        public static bool UltraFunGunsLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID) && Chainloader.PluginInfos.ContainsKey(CONFIGGY_LIB_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			SoftBanCheckResult result = new SoftBanCheckResult();

			var loadout = UltraFunGuns.Data.Loadout.Data;
			
			foreach (var slot in loadout.slots)
			{
				foreach (var node in slot.slotNodes)
				{
					if (node.weaponUnlocked && node.weaponEnabled)
					{
						result.banned = true;

						if (!string.IsNullOrEmpty(result.message))
							result.message += '\n';
						result.message += $"- Gun {node.weaponKey} is banned, unequip to be able to post records";
					}
				}
			}

			return result;
		}
	}
}
