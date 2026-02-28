using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader.DataTypes
{
	public class ScriptInfo
	{
		public string FileName { get; set; }
		public string Hash { get; set; }
		public int Size { get; set; }
		public List<string> Updates;
	}

	public class ScriptCatalog
	{
		public List<ScriptInfo> Scripts;
	}
}
