using RudeLevelScript;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RudeLevelScripts
{
	public class AscendingFinalRoom : MonoBehaviour
	{
		private void OnTriggerEnter(Collider other)
		{
			GameObject player = NewMovement.instance.gameObject;

			if (other.gameObject == player && MonoSingleton<NewMovement>.Instance && MonoSingleton<NewMovement>.Instance.hp > 0)
			{
				FirstRoomSpawner.PlayerForcedMovement forcedMovement = player.AddComponent<FirstRoomSpawner.PlayerForcedMovement>();
				forcedMovement.force = 100f;
			}
		}
	}
}
