using RudeLevelScript;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.DataTypes
{
	public class AngryLevelData
	{
		// V7
		public string[] requiredDllNames;
		public string uniqueIdentifier { get; set; }
		public string levelName { get; set; }
		public bool isSecretLevel { get; set; }
		public int prefferedLevelOrder { get; set; }
		public bool hideIfNotPlayed { get; set; }
		public string[] requiredCompletedLevelIdsForUnlock { get; set; }
		public bool levelChallengeEnabled { get; set; }
		public string levelChallengeText { get; set; }
		public int secretCount { get; set; }
		public bool doNotHideLevelPreviewWhenNotCompleted { get; set; }
	}
}
