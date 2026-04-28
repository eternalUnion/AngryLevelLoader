using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Utils
{
	internal class WordHighlighter
	{
		public string rawText { get; private set; }

		public WordHighlighter(string text)
		{
			rawText = text;
		}

		public List<(int, int)> highlights = new List<(int, int)>();

		public void Highlight(int index, int endIndex)
		{
			int insertionIndex = 0;
			while (insertionIndex < highlights.Count && index > highlights[insertionIndex].Item1)
				insertionIndex += 1;

			bool removeLeft = false;
			if (insertionIndex > 0 && highlights[insertionIndex - 1].Item2 >= index - 1)
			{
				// Characters already highlighted
				if (highlights[insertionIndex - 1].Item2 >= endIndex)
					return;

				index = highlights[insertionIndex - 1].Item1;
				removeLeft = true;
			}

			int removeRightCnt = 0;
			while (true)
			{
				if (insertionIndex < highlights.Count - removeRightCnt && endIndex >= highlights[insertionIndex + removeRightCnt].Item1 - 1)
				{
					endIndex = Mathf.Max(endIndex, highlights[insertionIndex + removeRightCnt].Item2);
					removeRightCnt += 1;
					continue;
				}

				break;
			}

			highlights.Insert(insertionIndex, (index, endIndex));
			for (int i = 0; i < removeRightCnt; i++)
				highlights.RemoveAt(insertionIndex + 1);
			if (removeLeft)
				highlights.RemoveAt(insertionIndex - 1);
		}

		public string GenerateFormattedText(string prefix, string postfix)
		{
			if (rawText.Length == 0)
				return string.Empty;

			StringBuilder sb = new StringBuilder();
			string text = rawText;

			int currentIndex = 0;
			foreach ((int index, int endIndex) in highlights)
			{
				if (currentIndex < index)
					sb.Append(text.Substring(currentIndex, index - currentIndex));

				sb.Append(prefix);
				sb.Append(text.Substring(index, endIndex - index + 1));
				sb.Append(postfix);

				currentIndex = endIndex + 1;
			}

			if (currentIndex < text.Length)
				sb.Append(text.Substring(currentIndex));

			return sb.ToString();
		}
	}
}
