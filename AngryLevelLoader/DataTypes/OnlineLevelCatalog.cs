using System.Collections.Generic;

namespace AngryLevelLoader.DataTypes
{
	public class LevelInfo
	{
		public string LevelName { get; set; }
		public string LevelId { get; set; }

		public bool isSecretLevel { get; set; }
		public List<string> requiredCompletedLevelIdsForUnlock;

		public int secretCount { get; set; }

		public bool levelChallengeEnabled { get; set; }
		public string levelChallengeText { get; set; }

		public List<string> requiredDllNames;
	}

	public class BundleInfo
	{
		public class UpdateInfo
		{
			public long Date { get; set; }
			public string Hash { get; set; }
			public string Message { get; set; }
		}

		public string Name { get; set; }
		public string Author { get; set; }
		public int Size { get; set; }
		public string Guid { get; set; }
		public string Hash { get; set; }
		public string ThumbnailHash { get; set; }

		public bool Locked { get; set; }
		public bool EpilepsyWarning { get; set; }
		public List<string> Parts;
		public long LastUpdate { get; set; }
		public List<UpdateInfo> Updates;

		public List<LevelInfo> Levels;
	}

	public class LevelCatalog
	{
		public List<BundleInfo> Levels;
	}
}
