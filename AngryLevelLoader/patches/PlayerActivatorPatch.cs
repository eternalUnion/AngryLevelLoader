using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Patches
{
	// Why I even need this witchcraft?

	/*
	 
	It seems after mouse locking (which happens with GameStateManager.PopState("pit-falling"))
	Player orientation is set to 0,0,0. This is normally not an issue for levels where the
	first room's orientation is already 0,0,0 but for custom levels using a different first room orientation
	player will instantly change direction as soon as the player hits the ground

	This is my best attempt to detect and avoid the initial rotation change

	 */

	/*[HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.EvaluateState))]
	public static class PlayerActivator_OnTriggerEnter_Patch
	{
		private class LateRotationSetter : MonoBehaviour
		{
			public Quaternion targetRot;

			public static int defaultFrameTime = 10;
			public static int defaultRecoveryTime = 1;
			public int framesTilDestruction = defaultFrameTime;
			private void LateUpdate ()
			{
				// Debug.Log("Late update");

				if (framesTilDestruction != 0)
				{
					if (transform.rotation == Quaternion.identity)
						framesTilDestruction = defaultRecoveryTime;
					else
						framesTilDestruction -= 1;
					
					Activate();
					return;
				}

				DestroyComp();
			}

			private void Activate()
			{
				// Debug.Log($"Late final rotation: {transform.eulerAngles}");

				transform.rotation = targetRot;
			}

			private void DestroyComp()
			{
				DestroyImmediate(this);
			}
		}

		[HarmonyPrefix]
		static bool Prefix(PlayerActivator __instance)
		{
			if (NewMovement.instance == null)
				return true;

			if (NewMovement.instance.TryGetComponent(out LateRotationSetter rot))
			{
				rot.framesTilDestruction = 5;
				// Debug.Log("Reset activasion");
			}
			else
			{
				if (NewMovement.instance.transform.rotation == Quaternion.identity)
					return true;

				NewMovement.instance.gameObject.AddComponent<LateRotationSetter>().targetRot = NewMovement.instance.transform.rotation;
				// Debug.Log($"Rotation before: {NewMovement.instance.transform.eulerAngles}");
			}

			return true;
		}
	}*/
}
