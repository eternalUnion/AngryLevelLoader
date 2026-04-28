using RudeLevelScript;

namespace AngryLevelLoader.DataTypes
{
	/// <summary>
	/// Metadata for an angry level. This object is stored inside data.json, which is
	/// located inside the angry file.
	/// </summary>
	public class AngryLevelData
	{
		// V7
		public string[] requiredDllNames;
		public string uniqueIdentifier { get; set; }
		public string levelName { get; set; }
		public bool isSecretLevel { get; set; }
		public int prefferedLevelOrder { get; set; }
		public bool hideIfNotPlayed { get; set; }
		public string[] requiredCompletedLevelIdsForUnlock { get; set; }
		public bool levelChallengeEnabled { get; set; }
		public string levelChallengeText { get; set; }
		public int secretCount { get; set; }
		public bool doNotHideLevelPreviewWhenNotCompleted { get; set; }

		internal static AngryLevelData FromRudeLevelData(RudeLevelData levelData)
		{
			return new AngryLevelData()
			{
				requiredDllNames = levelData.requiredDllNames,
				uniqueIdentifier = levelData.uniqueIdentifier,
				levelName = levelData.levelName,
				isSecretLevel = levelData.isSecretLevel,
				prefferedLevelOrder = levelData.prefferedLevelOrder,
				hideIfNotPlayed = levelData.hideIfNotPlayed,
				requiredCompletedLevelIdsForUnlock = levelData.requiredCompletedLevelIdsForUnlock,
				levelChallengeEnabled = levelData.levelChallengeEnabled,
				levelChallengeText = levelData.levelChallengeText,
				secretCount = levelData.secretCount,
				doNotHideLevelPreviewWhenNotCompleted = levelData.doNotHideLevelPreviewWhenNotCompleted,
			};
		}
	}
}
