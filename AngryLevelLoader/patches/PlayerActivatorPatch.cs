using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.patches
{
	/*
	 Fix the orientation problem later
	 */

	//[HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.EvaluateState))]
	public static class PlayerActivator_OnTriggerEnter_Patch
	{
		private class LateRotationSetter : MonoBehaviour
		{
			public Quaternion targetRot;

			private void Start ()
			{
				Invoke("Activate", 0.01f);
			}

			/*private void Update ()
			{
				if (transform.rotation == Quaternion.identity)
					Activate();
			}*/

			private void Activate()
			{
				transform.rotation = targetRot;
				DestroyComp();
			}

			private void DestroyComp()
			{
				Destroy(this);
			}
		}

		[HarmonyPrefix]
		static bool Prefix(PlayerActivator __instance, out Quaternion __state)
		{
			if (NewMovement.instance == null)
			{
				__state = Quaternion.identity;
				return true;
			}

			NewMovement.instance.gameObject.AddComponent<LateRotationSetter>().targetRot = NewMovement.instance.transform.rotation;
			__state = NewMovement.instance.transform.rotation;
			return true;

			/*if (!Plugin.isInCustomScene)
				return true;

			if (!__0.gameObject.CompareTag("Player"))
				return false;

			if (__instance.activated)
				return false;

			//NewMovement.instance.gameObject.AddComponent<LateRotationSetter>().targetRot = NewMovement.instance.transform.rotation;
			return true;*/
		}

		/*[HarmonyPostfix]
		static void Postfix(Quaternion __state)
		{
			if (NewMovement.instance != null)
				NewMovement.instance.transform.rotation = __state;
		}*/
	}
}
