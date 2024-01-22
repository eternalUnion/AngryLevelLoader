using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public struct SoftBanCheckResult
	{
		public bool banned;
		public string message;

		public SoftBanCheckResult()
		{
			banned = false;
			message = "";
		}

		public SoftBanCheckResult(bool banned, string message)
		{
			this.banned = banned;
			this.message = message;
		}
	}

	public static class BannedModsManager
	{

		// This is the local banned mods list. It should normally be fetched from angry server
		// In case the server is offline, this list will be used as a fallback
		public static readonly string[] LOCAL_BANNED_MODS_LIST = new string[]
		{
			AtlasWeaponsSoftBan.PLUGIN_GUID,
			DualWieldPunchesSoftBan.PLUGIN_GUID,
			FasterPunchSoftBan.PLUGIN_GUID,
			MovementPlusSoftBan.PLUGIN_GUID,
			UltraCoinsSoftBan.PLUGIN_GUID,
			UltraFunGunsSoftBan.PLUGIN_GUID,
			UltrapainSoftBan.PLUGIN_GUID,
			UltraTweakerSoftBan.PLUGIN_GUID,
			HeavenOrHellSoftBan.PLUGIN_GUID,
			WipFixHardBan.PLUGIN_GUID,
			MasqueradeDivinitySoftBan.PLUGIN_GUID,
		};

		public static Dictionary<string, Func<SoftBanCheckResult>> checkers = new Dictionary<string, Func<SoftBanCheckResult>>();
		public static Dictionary<string, string> guidToName = new Dictionary<string, string>()
		{
			{ AtlasWeaponsSoftBan.PLUGIN_GUID, "Atlas" },
			{ DualWieldPunchesSoftBan.PLUGIN_GUID, "DualWieldPunches" },
			{ FasterPunchSoftBan.PLUGIN_GUID, "FasterPunches" },
			{ MovementPlusSoftBan.PLUGIN_GUID, "Movement+" },
			{ UltraCoinsSoftBan.PLUGIN_GUID, "UltraCoins" },
			{ UltraFunGunsSoftBan.PLUGIN_GUID, "UltraFunGuns" },
			{ UltrapainSoftBan.PLUGIN_GUID, "UltraPain" },
			{ UltraTweakerSoftBan.PLUGIN_GUID, "UltraTweaker" },
			{ HeavenOrHellSoftBan.PLUGIN_GUID, "HeavenOrHell" },
			{ WipFixHardBan.PLUGIN_GUID, "Whiplash Buff" },
			{ MasqueradeDivinitySoftBan.PLUGIN_GUID, "Masquerade Divinity" },
		};

		public static void Init()
		{
			if (UltrapainSoftBan.UltrapainLoaded)
			{
				Plugin.logger.LogInfo("Detected UltraPain, adding soft ban check for leaderboards");
				checkers.Add(UltrapainSoftBan.PLUGIN_GUID, UltrapainSoftBan.Check);
			}

			if (DualWieldPunchesSoftBan.DualWieldPunchesLoaded)
			{
				Plugin.logger.LogInfo("Detected DualPunches, adding soft ban check for leaderboards");
				checkers.Add(DualWieldPunchesSoftBan.PLUGIN_GUID, DualWieldPunchesSoftBan.Check);
			}

			if (UltraTweakerSoftBan.UltraTweakerLoaded)
			{
				Plugin.logger.LogInfo("Detected UltraTweaker, adding soft ban check for leaderboards");
				checkers.Add(UltraTweakerSoftBan.PLUGIN_GUID, UltraTweakerSoftBan.Check);
			}

			if (MovementPlusSoftBan.MovementPlusLoaded)
			{
				Plugin.logger.LogInfo("Detected Movement+, adding soft ban check for leaderboards");
				checkers.Add(MovementPlusSoftBan.PLUGIN_GUID, MovementPlusSoftBan.Check);
			}

			if (UltraCoinsSoftBan.UltraCoinsLoaded)
			{
				Plugin.logger.LogInfo("Detected UltraCoins, adding soft ban check for leaderboards");
				checkers.Add(UltraCoinsSoftBan.PLUGIN_GUID, UltraCoinsSoftBan.Check);
			}

			if (UltraFunGunsSoftBan.UltraFunGunsLoaded)
			{
				Plugin.logger.LogInfo("Detected UltraFunGuns, adding soft ban check for leaderboards");
				checkers.Add(UltraFunGunsSoftBan.PLUGIN_GUID, UltraFunGunsSoftBan.Check);
			}

			if (FasterPunchSoftBan.FasterPunchLoaded)
			{
				Plugin.logger.LogInfo("Detected FasterPunch, adding soft ban check for leaderboards");
					checkers.Add(FasterPunchSoftBan.PLUGIN_GUID, FasterPunchSoftBan.Check);
			}

			if (AtlasWeaponsSoftBan.AtlasLoaded)
			{
				Plugin.logger.LogInfo("Detected AtlasLib, adding soft ban check for leaderboards");
				checkers.Add(AtlasWeaponsSoftBan.PLUGIN_GUID, AtlasWeaponsSoftBan.Check);
			}

			if (HeavenOrHellSoftBan.HeavenOrHellLoaded)
			{
				Plugin.logger.LogInfo("Detected HeavenOrHell, adding soft ban check for leaderboards");
				checkers.Add(HeavenOrHellSoftBan.PLUGIN_GUID, HeavenOrHellSoftBan.Check);
			}

			if (WipFixHardBan.WipFixLoaded)
			{
				Plugin.logger.LogInfo("Detected WipFix, adding soft ban check for leaderboards");
				checkers.Add(WipFixHardBan.PLUGIN_GUID, WipFixHardBan.Check);
			}

			if (MasqueradeDivinitySoftBan.MasqueradeDivinityLoaded)
			{
				Plugin.logger.LogInfo("Detected MasqueradeDivinity, adding soft ban check for leaderboards");
				checkers.Add(MasqueradeDivinitySoftBan.PLUGIN_GUID, MasqueradeDivinitySoftBan.Check);
			}
		}
	}
}
