using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.Extensions
{
	public static class EnumExtensions
	{
		public static T Next<T>(this T src) where T : Enum
		{
			T[] Arr = (T[])Enum.GetValues(src.GetType());
			int j = Array.IndexOf<T>(Arr, src) + 1;
			return (Arr.Length == j) ? Arr[0] : Arr[j];
		}
	}
}
