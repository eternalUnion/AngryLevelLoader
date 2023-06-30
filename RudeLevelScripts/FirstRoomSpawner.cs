using System;
using UnityEngine;

namespace RudeLevelScript
{
	public class FirstRoomSpawner : MonoBehaviour
	{
		public bool secretRoom = false;

		[Header("Player Fields")]
		public CameraClearFlags cameraFillMode = CameraClearFlags.SolidColor;
		public Color backgroundColor = Color.black;

		[Header("Level Fields")]
		public bool displayLevelTitle = true;
		public bool startMusic = true;

		private void Awake()
		{
			GameObject firstRoomRef = Utils.LoadObject<GameObject>(secretRoom ? "FirstRoom Secret" : "FirstRoom");
			GameObject firstRoomInst = Instantiate(firstRoomRef, transform.parent);

			firstRoomInst.transform.position = transform.position;
			firstRoomInst.transform.rotation = transform.rotation;

			try
			{
				Transform doorRoot = firstRoomInst.transform.Find("Room/FinalDoor");
				FinalDoor finalDoor = doorRoot.GetComponent<FinalDoor>();

				// Assign player preferences
				Camera mainCam = Camera.main;
				mainCam.backgroundColor = backgroundColor;
				mainCam.clearFlags = cameraFillMode;

				FinalDoorOpener opener = firstRoomInst.transform.Find("Room/FinalDoor/FinalDoorOpener").GetComponent<FinalDoorOpener>();
				opener.startMusic = startMusic;
				FinalDoor door = firstRoomInst.transform.Find("Room/FinalDoor").GetComponent<FinalDoor>();
				door.levelNameOnOpen = displayLevelTitle;
			}
			catch (Exception e)
			{
				throw e;
			}
			finally
			{
				Destroy(gameObject);
			}
		}
	}
}
