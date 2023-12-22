using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AngryLevelLoader
{
	public class ManifestReader
	{
		public static byte[] GetBytes(string resourceName)
		{
			using (var str = Assembly.GetExecutingAssembly().GetManifestResourceStream($"AngryLevelLoader.Resources.{resourceName}"))
			{
				byte[] buff = new byte[str.Length];
				str.Read(buff, 0, buff.Length);
				return buff;
			}
		}
	}
}
