using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Managers.BannedMods.SoftBans
{
	[SoftBanClass]
	internal class MasqueradeDivinitySoftBan : SoftBan
	{
		public override string ModGuid => "maranara_project_prophet";

		public override string ModName => "Masquerade Divinity";

		public override SoftBanCheckResult Check()
		{
			if (ProjectProphet.ProjectProphet.gabeOn)
				return new SoftBanCheckResult(true, "Cannot post records on Masquerade Divinity save");

			return new SoftBanCheckResult(false, "");
		}
	}
}
