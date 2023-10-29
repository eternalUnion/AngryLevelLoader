using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	// Note: Movement namespace collides with the Movmement type, so type instances cannot be used without reflections
	public static class MovementPlusSoftBan
	{
		public const string PLUGIN_GUID = "waffle.ultrakill.movement";

		public static bool MovementPlusLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID) && Chainloader.PluginInfos.ContainsKey(UltraTweakerSoftBan.PLUGIN_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			string[] bannedTweakTypes = new string[]
			{
				"Movement.Tweaks.DoubleJump",
				"Movement.Tweaks.MomentumDash",
				"Movement.Tweaks.RealHook",
				"Movement.Tweaks.Recoil",
			};

			SoftBanCheckResult result = new SoftBanCheckResult();
			result.pluginName = "Movement+";

			foreach (var tweak in UltraTweaker.UltraTweaker.AllTweaks)
			{
				if (Array.IndexOf(bannedTweakTypes, tweak.Key.FullName) != -1)
				{
					if (tweak.Value.IsEnabled)
					{
						if (!string.IsNullOrEmpty(result.message))
							result.message += '\n';
						result.message += $"Movement+ tweak {tweak.Key.Name} is banned";

						result.banned = true;
					}
				}
			}

			return result;
		}
	}
}
