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
		public const string HYDRA_LIB_GUID = "Hydraxous.HydraDynamics";

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
			{ UltraTweakerSoftBan.PLUGIN_GUID, "UltraTweaker" }
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
		}

		public static List<SoftBanCheckResult> CheckBans()
		{
			List<SoftBanCheckResult> bans = new List<SoftBanCheckResult>();

			foreach (var checker in checkers)
			{
				try
				{
					SoftBanCheckResult res = checker.Value();

					if (res.banned)
						bans.Add(res);
				}
				catch (Exception e)
				{
					Plugin.logger.LogError($"Exception thrown while checking for soft ban for {guidToName[checker.Key]}\n{e}");
					bans.Add(new SoftBanCheckResult(true, $"Encountered an error while checking for {guidToName[checker.Key]}"));
				}
			}

			return bans;
		}
	}
}
