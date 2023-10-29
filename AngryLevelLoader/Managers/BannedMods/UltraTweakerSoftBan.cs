using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class UltraTweakerSoftBan
	{
		public const string PLUGIN_GUID = "waffle.ultrakill.ultratweaker";

		public static bool UltraTweakerLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		public static SoftBanCheckResult Check()
		{
			Type[] bannedTweaks = new Type[]
			{
				typeof(UltraTweaker.Tweaks.Impl.CloseQuarters),
				typeof(UltraTweaker.Tweaks.Impl.FallDamage),
				typeof(UltraTweaker.Tweaks.Impl.FloorIsLava),
				typeof(UltraTweaker.Tweaks.Impl.Fragility),
				typeof(UltraTweaker.Tweaks.Impl.Fresh),
				typeof(UltraTweaker.Tweaks.Impl.FuelLeak),
				typeof(UltraTweaker.Tweaks.Impl.Ice),
				typeof(UltraTweaker.Tweaks.Impl.Mitosis),
				typeof(UltraTweaker.Tweaks.Impl.Sandify),
				typeof(UltraTweaker.Tweaks.Impl.Speed),
				typeof(UltraTweaker.Tweaks.Impl.Submerged),
				typeof(UltraTweaker.Tweaks.Impl.Tankify),
				typeof(UltraTweaker.Tweaks.Impl.Ultrahot)
			};

			SoftBanCheckResult result = new SoftBanCheckResult();

			foreach (var tweak in UltraTweaker.UltraTweaker.AllTweaks)
			{
				if (Array.IndexOf(bannedTweaks, tweak.Key) != -1)
				{
					if (tweak.Value.IsEnabled)
					{
						if (!string.IsNullOrEmpty(result.message))
							result.message += '\n';
						result.message += $"UltraTweaker tweak {tweak.Key.Name} is banned";

						result.banned = true;
					}
				}
			}

			return result;
		}
	}
}
