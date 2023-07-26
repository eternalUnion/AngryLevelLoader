using UnityEngine;

namespace RudeLevelScript
{
	public class FinalRoomTarget : MonoBehaviour
	{
		[Tooltip("If a valid Unique ID of a Rude Level Data is written, the level will be loaded. This ID can be in other bundles. If the id is invalid or the level is not available, returns to the main menu")]
		public string targetLevelUniqueId = "";
	}
}
