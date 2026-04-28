using BepInEx.Bootstrap;
using System;

namespace AngryLevelLoader.Managers.BannedMods
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	internal class SoftBanClassAttribute : Attribute
	{
	}

	internal struct SoftBanCheckResult
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

	internal abstract class SoftBan
	{
		public abstract string ModGuid { get; }

		public abstract string ModName { get; }

		public virtual bool ModLoaded => Chainloader.PluginInfos.ContainsKey(ModGuid);

		public virtual void Init() { }

		public abstract SoftBanCheckResult Check();
	}
}
