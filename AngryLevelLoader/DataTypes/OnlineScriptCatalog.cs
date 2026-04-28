using System.Collections.Generic;

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
