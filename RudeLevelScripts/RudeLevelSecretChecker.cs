using AngryLoaderAPI;
using UnityEngine;

namespace RudeLevelScript
{
	public class RudeLevelSecretChecker : MonoBehaviour
	{
		public string targetLevelId = "";
		public int targetSecretIndex = 0;

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
			if (LevelInterface.GetLevelSecret(targetLevelId, targetSecretIndex))
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
