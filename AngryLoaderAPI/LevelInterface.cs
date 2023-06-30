using AngryLevelLoader;

namespace AngryLoaderAPI
{
	public static class LevelInterface
	{
		public static char INCOMPLETE_LEVEL_CHAR = RudeInterface.INCOMPLETE_LEVEL_CHAR;
		public static char GetLevelRank(string levelId)
		{
			return RudeInterface.GetLevelRank(levelId);
		}

		public static bool GetLevelChallenge(string levelId)
		{
			return RudeInterface.GetLevelChallenge(levelId);
		}

		public static bool GetLevelSecret(string levelId, int secretIndex)
		{
			return RudeInterface.GetLevelSecret(levelId, secretIndex);
		}
	}
}
