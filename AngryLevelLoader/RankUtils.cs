namespace AngryLevelLoader
{
	public static class RankUtils
	{
		public static int GetRankScore(char rank)
		{
			if (rank == 'D')
				return 1;
			if (rank == 'C')
				return 2;
			if (rank == 'B')
				return 3;
			if (rank == 'A')
				return 4;
			if (rank == 'S')
				return 5;
			if (rank == 'P')
				return 6;
			if (rank == '-')
				return -1;
			if (rank == ' ')
				return 0;

			return -1;
		}

		/*public static bool CheckState(LevelRankState state, char rank)
		{
			int rankScore = GetRankScore(rank);

			switch (state)
			{
				case LevelRankState.NotCompleted:
					return rank == '-';
				case LevelRankState.Completed:
					return rank != '-';
				case LevelRankState.CompletedWithCheats:
					return rank == ' ';
				case LevelRankState.CompletedWithoutCheats:
					return rank != '-' && rank != ' ';
				case LevelRankState.D:
					return rank == 'D';
				case LevelRankState.AtLeastD:
					return rankScore >= GetRankScore('D');
				case LevelRankState.AtMostD:
					return rankScore <= GetRankScore('D');
				case LevelRankState.C:
					return rank == 'C';
				case LevelRankState.AtLeastC:
					return rankScore >= GetRankScore('C');
				case LevelRankState.AtMostC:
					return rankScore <= GetRankScore('C');
				case LevelRankState.B:
					return rank == 'B';
				case LevelRankState.AtLeastB:
					return rankScore >= GetRankScore('B');
				case LevelRankState.AtMostB:
					return rankScore <= GetRankScore('B');
				case LevelRankState.A:
					return rank == 'A';
				case LevelRankState.AtLeastA:
					return rankScore >= GetRankScore('A');
				case LevelRankState.AtMostA:
					return rankScore <= GetRankScore('A');
				case LevelRankState.S:
					return rank == 'S';
				case LevelRankState.AtLeastS:
					return rankScore >= GetRankScore('S');
				case LevelRankState.AtMostS:
					return rankScore <= GetRankScore('S');
				case LevelRankState.P:
					return rank == 'P';
			}

			return false;
		}*/
	}
}
