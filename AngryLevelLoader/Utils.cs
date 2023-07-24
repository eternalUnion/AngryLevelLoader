using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

		private static Dictionary<char, Color> rankColors = new Dictionary<char, Color>()
		{
			{ 'D', new Color(0, 0x94 / 255f, 0xFF / 255f) },
			{ 'C', new Color(0x4C / 255f, 0xFF / 255f, 0) },
			{ 'B', new Color(0xFF / 255f, 0xD8 / 255f, 0) },
			{ 'A', new Color(0xFF / 255f, 0x6A / 255f, 0) },
			{ 'S', Color.red },
			{ 'P', Color.white },
			{ '-', Color.gray }
		};
		public static string GetFormattedRankText(char rank)
		{
			Color textColor;
			if (!rankColors.TryGetValue(rank, out textColor))
				textColor = Color.gray;

			return $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{rank}</color>";
		}
	}

	public static class IOUtils
	{
		public static string GetUniqueFileName(string folder, string name)
		{
			string nameExtensionless = Path.GetFileNameWithoutExtension(name);
			string newName = nameExtensionless;
			string ext = Path.GetExtension(name);

			int i = 0;
			while (File.Exists(Path.Combine(folder, newName)))
				newName = $"{nameExtensionless}_{i++}";

			return $"{newName}{ext}";
		}
	}
}
