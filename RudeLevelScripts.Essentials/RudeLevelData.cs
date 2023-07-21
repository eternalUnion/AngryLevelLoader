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
		[Tooltip("Scene which belongs to the data")]
		public Object targetScene = null;
		[HideInInspector]
		public string scenePath = "";
		[Header("Level Locator")]
		[Tooltip("This ID is used to reference the level externally, such as final pit targets or activasion scripts. This field must not match any other data's id even accross bundles. Strong naming suggested, such as including author name in the id")]
		public string uniqueIdentifier = "";

		[Space(10)]
		[Header("Level Info")]
		[Tooltip("Name which will be displayed on angry")]
		public string levelName = "";
		[Tooltip("If the level is secret and uses FirstRoomSecret variant, set to true. Enabling this field will disable rank and challenge panel on angry side")]
		public bool isSecretLevel = false;
		[Tooltip("Order of the level in angry. Lower valued levels are at the top")]
		public int prefferedLevelOrder = 0;
		[Tooltip("Sprite containing the level thumbnail. Label the sprite as well")]
		public Sprite levelPreviewImage = null;

		[Space(10)]
		[Tooltip("Enablind this field will hide the level from angry until accessed by a final pit. Implemented for secret levels")]
		public bool hideIfNotPlayed = false;
		[Tooltip("The level IDs required to be completed before the level can be played. If one of the level ID's are not completed, level will be locked")]
		public string[] requiredCompletedLevelIdsForUnlock;

		[Space(10)]
		public bool levelChallengeEnabled = false;
		public string levelChallengeText = "";
		[Tooltip("Set exactly to the number of secret bonuses in the level")]
		public int secretCount = 0;
	}
}
