using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class MasqueradeDivinitySoftBan
	{
		public const string PLUGIN_GUID = "maranara_project_prophet";

		public static bool MasqueradeDivinityLoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

		// Amount of coins is not configurable
		public static SoftBanCheckResult Check()
		{
			if (ProjectProphet.ProjectProphet.gabeOn)
				return new SoftBanCheckResult(true, "Cannot post records on Masquerade Divinity save");

			return new SoftBanCheckResult(false, "");
		}
	}
}
