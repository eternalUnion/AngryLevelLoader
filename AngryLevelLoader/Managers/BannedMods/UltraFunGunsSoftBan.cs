using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class UltraFunGunsSoftBan
	{
		public const string PLUGIN_GUID = "Hydraxous.ULTRAKILL.UltraFunGuns";

		public static bool UltraFunGunsLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID) && Chainloader.PluginInfos.ContainsKey(BannedModsManager.HYDRA_LIB_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			SoftBanCheckResult result = new SoftBanCheckResult();
			result.pluginName = "UltraFunGuns";

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
						result.message += $"UltraFunGun {node.weaponKey} is banned";	
					}
				}
			}

			return result;
		}
	}
}
