using System.Collections.Generic;
using UnityEngine;

namespace AngryLevelLoader.Utils
{
	public static class AngryRankUtils
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

		public static char GetRankChar(int score)
		{
            if (score == 1)
                return 'D';
            if (score == 2)
                return 'C';
            if (score == 3)
                return 'B';
            if (score == 4)
                return 'A';
            if (score == 5)
                return 'S';
            if (score == 6)
                return 'P';
            if (score == -1)
                return '-';
            if (score == 0)
                return ' ';

            return '-';
        }

		private static Dictionary<char, Color> rankColors = new Dictionary<char, Color>()
		{
			{ 'D', new Color(0, 0x94 / 255f, 0xFF / 255f) },
			{ 'C', new Color(0x4C / 255f, 0xFF / 255f, 0) },
			{ 'B', new Color(0xFF / 255f, 0xD8 / 255f, 0) },
			{ 'A', new Color(0xFF / 255f, 0x6A / 255f, 0) },
			{ 'S', Color.red },
			{ 'P', Color.white },
			{ '-', Color.gray },
			{ ' ', Color.gray },
		};

		public static Color GetRankColor(char rank, Color fallback)
		{
			if (rankColors.TryGetValue(rank, out var color))
				return color;
			return fallback;
		}

		public static string GetFormattedRankText(char rank)
		{
			Color textColor;
			if (!rankColors.TryGetValue(rank, out textColor))
				textColor = Color.gray;

			return $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{rank}</color>";
		}
	}
}
