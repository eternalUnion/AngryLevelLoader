﻿using Atlas.Modules.Guns;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class AtlasWeaponsSoftBan
	{
		public const string PLUGIN_GUID = "waffle.ultrakill.atlas";

		public static bool AtlasLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			SoftBanCheckResult result = new SoftBanCheckResult();

			foreach (var weapon in GunRegistry.WeaponList)
			{
				if (weapon.Enabled() != 0)
				{
					result.banned = true;

					if (!string.IsNullOrEmpty(result.message))
						result.message += '\n';
					result.message += $"- Gun {weapon.Pref()} is banned, unequip to be able to post records";
				}
			}

			return result;
		}
	}
}
