using AngryLoaderAPI;
using UnityEngine;

namespace RudeLevelScript
{
	public class RudeLevelChallengeChecker : MonoBehaviour
	{
		public string targetLevelId = null;

		public UltrakillEvent onSuccess = null;
		public UltrakillEvent onFailure = null;

		public bool activateOnEnable = true;
		public void OnEnable()
		{
			if (activateOnEnable)
				Activate();
		}

		public void Activate()
		{
			if (LevelInterface.GetLevelChallenge(targetLevelId))
			{
				if (onSuccess != null)
					onSuccess.Invoke();
			}
			else
			{
				if (onFailure != null)
					onFailure.Invoke();
			}
		}
	}
}
