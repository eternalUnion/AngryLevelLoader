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

				// Fix first room door not having yellow texture while opening
				if (finalDoor.allRenderers == null)
					finalDoor.allRenderers = finalDoor.GetComponentsInChildren<MeshRenderer>();

				foreach (MeshRenderer mr in finalDoor.allRenderers)
				{
					if (mr.sharedMaterial == null)
						continue;

					Material instMat = mr.sharedMaterial;
					string realName = instMat.name;
					while (realName.EndsWith(" (Instance)"))
						realName = realName.Substring(0, realName.Length - " (Instance)".Length);
					
					int index = -1;
					for (int i = 0; i < finalDoor.offMaterials.Length; i++)
					{
						if (finalDoor.offMaterials[i].name == realName)
						{
							index = i;
							break;
						}
					}

					Debug.Log($"{mr.sharedMaterial.name} : {realName} : {index}");

					if (index != -1)
					{
						finalDoor.offMaterials[index] = mr.sharedMaterial;
					}
				}

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
