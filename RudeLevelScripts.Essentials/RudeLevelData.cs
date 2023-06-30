using UnityEngine;

namespace RudeLevelScript
{
	/*public enum LevelRankState
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

	[Serializable]
	public class LevelRankRequirement
	{
		[SerializeField]
		public string levelId = "";
		[SerializeField]
		public LevelRankState requiredRank = LevelRankState.Completed;
	}*/

	[CreateAssetMenu]
	public class RudeLevelData : ScriptableObject
	{
		[SerializeField]
		public UnityEngine.Object targetScene = null;
		[HideInInspector]
		public string scenePath = "";
		[Header("Level Locator")]
		public string uniqueIdentifier = "";

		[Space(10)]
		[Header("Level Info")]
		public string levelName = "";
		public bool isSecretLevel = false;
		public int prefferedLevelOrder = 0;
		public Sprite levelPreviewImage = null;

		[Space(10)]
		public bool hideIfNotPlayed = false;
		public string[] requiredCompletedLevelIdsForUnlock;

		[Space(10)]
		public bool levelChallengeEnabled = false;
		public string levelChallengeText = "";
		public int secretCount = 0;
	}
}
