using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AngryLevelLoader.Managers.BannedMods
{
	internal static class BannedModsManager
	{
		// This is the local banned mods list. It should normally be fetched from angry server
		// In case the server is offline, this list will be used as a fallback
		public static readonly List<string> LOCAL_BANNED_MODS_LIST = new List<string>();

		public static List<SoftBan> checkers = new List<SoftBan>();

		public static SoftBan GetChecker(string guid)
		{
			return checkers.Where(chk => chk.ModGuid == guid).FirstOrDefault();
		}

		public static void Init()
		{
			foreach (Type softBanClassType in Assembly.GetCallingAssembly().GetTypes().Where(t => t.GetCustomAttribute(typeof(SoftBanClassAttribute)) != null))
			{
				SoftBan instance = softBanClassType.GetConstructor(new Type[0]).Invoke(new object[0]) as SoftBan;
				if (instance == null)
				{
					Plugin.logger.LogWarning($"Softban class {softBanClassType.Name} does not inherit {nameof(SoftBan)}!");
					continue;
				}

				if (!instance.ModLoaded)
					continue;

				Plugin.logger.LogInfo($"Detected {instance.ModName}, adding soft ban check for leaderboards");
				
				try
				{
					instance.Init();
				}
				catch (Exception e)
				{
					Plugin.logger.LogError(e);
					continue;
				}

				LOCAL_BANNED_MODS_LIST.Add(instance.ModGuid);
				checkers.Add(instance);
			}

			ConfigManager.config.rootPanel.onPannelOpenEvent += (externally) =>
			{
				bool bannedModsFound = false;
				ConfigManager.bannedModsText.text = "";

				foreach (SoftBan checker in checkers)
				{
					try
					{
						var result = checker.Check();

						if (result.banned)
						{
							if (!string.IsNullOrEmpty(ConfigManager.bannedModsText.text))
								ConfigManager.bannedModsText.text += '\n';
							ConfigManager.bannedModsText.text += $"<color=red>{checker.ModName}</color>\n<size=18>{result.message}</size>\n\n";
							bannedModsFound = true;
						}
					}
					catch (Exception e)
					{
						Plugin.logger.LogError($"Exception thrown while checking for soft ban for {checker.ModName}\n{e}");

						if (!string.IsNullOrEmpty(ConfigManager.bannedModsText.text))
							ConfigManager.bannedModsText.text += '\n';
						ConfigManager.bannedModsText.text += $"<color=red>{checker.ModName}</color>\n<size=18>- Encountered an error while checking for the soft ban status, check console</size>\n\n";
						bannedModsFound = true;
					}
				}

				ConfigManager.bannedModsPanel.hidden = !bannedModsFound;
			};
		}
	}
}
