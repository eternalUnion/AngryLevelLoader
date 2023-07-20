using System;
using System.Collections.Generic;
using System.Text;

namespace AngryLevelLoader
{
	public static class AngrySceneManager
	{
		public static string CurrentLoadPath = "";

		public static void LoadLevel(string levelPath, string tempFolder)
		{
			CurrentLoadPath = tempFolder;

			SceneHelper.LoadScene(levelPath);
		}
	}
}
