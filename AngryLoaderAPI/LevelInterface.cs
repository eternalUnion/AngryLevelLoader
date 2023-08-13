using AngryLevelLoader;

namespace AngryLoaderAPI
{
	public static class LevelInterface
	{
		public static char INCOMPLETE_LEVEL_CHAR = RudeLevelInterface.INCOMPLETE_LEVEL_CHAR;
		public static char GetLevelRank(string levelId)
		{
			return RudeLevelInterface.GetLevelRank(levelId);
		}

		public static bool GetLevelChallenge(string levelId)
		{
			return RudeLevelInterface.GetLevelChallenge(levelId);
		}

		public static bool GetLevelSecret(string levelId, int secretIndex)
		{
			return RudeLevelInterface.GetLevelSecret(levelId, secretIndex);
		}

        public static string GetCurrentLevelId()
        {
            return RudeLevelInterface.GetCurrentLevelId();
        }
    }
}
