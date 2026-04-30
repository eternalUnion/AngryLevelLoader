using RudeLevelScript;
using UnityEngine;

namespace RudeLevelScripts
{
	public class AscendingFinalRoom : MonoBehaviour
	{
		private void OnTriggerEnter(Collider other)
		{
			GameObject player = NewMovement.Instance.gameObject;

			if (other.gameObject == player && NewMovement.Instance && NewMovement.Instance.hp > 0)
			{
				FirstRoomSpawner.PlayerForcedMovement forcedMovement = player.AddComponent<FirstRoomSpawner.PlayerForcedMovement>();
				forcedMovement.force = 100f;
			}
		}
	}
}
