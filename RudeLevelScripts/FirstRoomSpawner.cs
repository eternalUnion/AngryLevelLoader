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

		private bool spawned = false;

		public void Spawn()
		{
			if (spawned)
				return;

			GameObject firstRoomRef = Utils.LoadObject<GameObject>(secretRoom ? "FirstRoom Secret" : "FirstRoom");
			GameObject firstRoomInst = Instantiate(firstRoomRef, transform.parent);

			// Reverse combined mesh
			foreach (MeshCollider col in firstRoomInst.GetComponentsInChildren<MeshCollider>())
			{
				if (col.gameObject.TryGetComponent(out MeshFilter mf))
				{
					mf.mesh = col.sharedMesh;
				}
			}
			// Update player position and orientation
			Transform player = NewMovement.instance.transform;
			player.transform.parent = firstRoomInst.transform;
			firstRoomInst.transform.position = transform.position;
			firstRoomInst.transform.rotation = transform.rotation;
			player.transform.parent = null;
			player.transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
			StatsManager.instance.spawnPos = player.transform.position;

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
				spawned = true;
				Destroy(gameObject);
			}
		}
		
		// Compability for older levels
		public void Awake()
		{
			Spawn();
		}
	}
}
