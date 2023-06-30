using AngryLoaderAPI;
using UnityEngine;

namespace RudeLevelScript
{
	public enum LevelRanks
	{
		NotCompleted,
		Completed,
		CompletedWithCheats,
		CompletedWithoutCheats,
		D,
		AtLeastD,
		AtMostD,
		C,
		AtLeastC,
		AtMostC,
		B,
		AtLeastB,
		AtMostB,
		A,
		AtLeastA,
		AtMostA,
		S,
		AtLeastS,
		AtMostS,
		P
	}

	public class RudeLevelRankChecker : MonoBehaviour
	{
		public string targetLevelUniqueId = "";
		public LevelRanks requiredFinalRank = LevelRanks.Completed;

		public UltrakillEvent OnSuccess = null;
		public UltrakillEvent OnFail = null;

		public bool activateOnEnable = true;
		public void OnEnable()
		{
			if (activateOnEnable)
				Activate();
		}
		
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
			return -1;
		}

		public void Activate()
		{
			char rank = LevelInterface.GetLevelRank(targetLevelUniqueId);
			int rankScore = GetRankScore(rank);
			bool success = false;

			switch (requiredFinalRank)
			{
				case LevelRanks.NotCompleted:
					success = rank == '-';
					break;
				case LevelRanks.Completed:
					success = rank != '-';
					break;
				case LevelRanks.CompletedWithCheats:
					success = rank == ' ';
					break;
				case LevelRanks.CompletedWithoutCheats:
					success = rank != ' ' && rank != '-';
					break;
				case LevelRanks.D:
					success = rank == 'D';
					break;
				case LevelRanks.C:
					success = rank == 'C';
					break;
				case LevelRanks.B:
					success = rank == 'B';
					break;
				case LevelRanks.A:
					success = rank == 'A';
					break;
				case LevelRanks.S:
					success = rank == 'S';
					break;
				case LevelRanks.P:
					success = rank == 'P';
					break;

				case LevelRanks.AtLeastD:
					success = rankScore >= GetRankScore('D');
					break;
				case LevelRanks.AtMostD:
					success = rankScore <= GetRankScore('D');
					break;
				case LevelRanks.AtLeastC:
					success = rankScore >= GetRankScore('C');
					break;
				case LevelRanks.AtMostC:
					success = rankScore <= GetRankScore('C');
					break;
				case LevelRanks.AtLeastB:
					success = rankScore >= GetRankScore('B');
					break;
				case LevelRanks.AtMostB:
					success = rankScore <= GetRankScore('B');
					break;
				case LevelRanks.AtLeastA:
					success = rankScore >= GetRankScore('A');
					break;
				case LevelRanks.AtMostA:
					success = rankScore <= GetRankScore('A');
					break;
				case LevelRanks.AtLeastS:
					success = rankScore >= GetRankScore('S');
					break;
				case LevelRanks.AtMostS:
					success = rankScore <= GetRankScore('S');
					break;
			}

			if (success)
			{
				if (OnSuccess != null)
					OnSuccess.Invoke();
			}
			else
			{
				if (OnFail != null)
					OnFail.Invoke();
			}
		}
	}
}
