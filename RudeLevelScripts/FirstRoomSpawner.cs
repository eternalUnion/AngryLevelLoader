using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RudeLevelScript
{
	[Serializable]
	public class LayerInfo
	{
		public string layerName = "";
		public string[] layerLevels = new string[0];
	}

	public class FirstRoomSpawner : MonoBehaviour, ISerializationCallbackReceiver
	{
		public bool secretRoom = false;

		[Header("Player Fields")]
		public CameraClearFlags cameraFillMode = CameraClearFlags.SolidColor;
		public Color backgroundColor = Color.black;

		[Header("Level Fields")]
		public bool displayLevelTitle = true;
		public bool startMusic = true;

		[Header("Hellmap")]
		public bool enableHellMap = false;
		public List<LayerInfo> layersAndLevels = new List<LayerInfo>();
		// thank you serialization hell
		[HideInInspector]
		public List<int> levelSizes = new List<int>();
		[HideInInspector]
		public List<string> layerNames = new List<string>();
		[HideInInspector]
		public List<string> levelNames = new List<string>();

		public int layerIndexToStartFrom;
		public int levelIndexToStartFrom;

		private bool spawned = false;

		public void OnBeforeSerialize()
		{
			//Debug.Log($"Pre serialize {layersAndLevels == null}");

			levelSizes.Clear();
			layerNames.Clear();
			levelNames.Clear();
			for (int i = 0; i < layersAndLevels.Count; i++)
			{
				layerNames.Add(layersAndLevels[i].layerName);
				levelSizes.Add(layersAndLevels[i].layerLevels.Length);
				levelNames.AddRange(layersAndLevels[i].layerLevels);
			}
		}

		public void Deserialize()
		{
			layersAndLevels.Clear();
			int levelIndex = 0;

			for (int i = 0; i < levelSizes.Count; i++)
			{
				LayerInfo layer = new LayerInfo();
				layer.layerName = layerNames[i];

				int size = levelSizes[i];
				layer.layerLevels = new string[size];
				for (int k = 0; k < size; k++)
				{
					layer.layerLevels[k] = levelNames[levelIndex++];
				}

				layersAndLevels.Add(layer);
			}
		}

		public void OnAfterDeserialize()
		{
			//Debug.Log("Deserialize");

			/*layersAndLevels.Clear();
			int levelIndex = 0;

			for (int i = 0; i < levelSizes.Count; i++)
			{
				LayerInfo layer = new LayerInfo();
				layer.layerName = layerNames[i];

				int size = levelSizes[i];
				layer.layerLevels = new string[size];
				for (int k = 0; k < size; k++)
				{
					layer.layerLevels[k] = levelNames[levelIndex++];
				}

				layersAndLevels.Add(layer);
			}*/
		}

		private static Text MakeText(Transform parent)
		{
			GameObject obj = new GameObject();
			RectTransform rect = obj.AddComponent<RectTransform>();
			rect.SetParent(parent);

			obj.transform.localScale = Vector3.one;

			return obj.AddComponent<Text>();
		}

		private static RectTransform MakeRect(Transform parent)
		{
			GameObject obj = new GameObject();
			RectTransform rect = obj.AddComponent<RectTransform>();
			rect.SetParent(parent);

			return rect;
		}

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

				// Create hellmap
				if (!enableHellMap)
					return;

				Deserialize();

				Transform canvas = NewMovement.instance.transform.Find("Canvas") ?? SceneManager.GetActiveScene().GetRootGameObjects().Where(o => o.name == "Canvas").First().transform;
				RectTransform hellmapContainer = MakeRect(canvas);
				hellmapContainer.anchorMin = hellmapContainer.anchorMax = new Vector2(0.5f, 0.5f);
				hellmapContainer.pivot = new Vector2(0.5f, 0.5f);
				hellmapContainer.sizeDelta = new Vector2(250, 650);
				hellmapContainer.anchoredPosition = Vector2.zero;
				hellmapContainer.localScale = Vector3.one;
				VerticalLayoutGroup vLayout = hellmapContainer.gameObject.AddComponent<VerticalLayoutGroup>();
				vLayout.childAlignment = TextAnchor.UpperCenter;
				vLayout.spacing = 5;
				vLayout.childForceExpandHeight = false;
				vLayout.childControlHeight = false;
				vLayout.childControlWidth = false;

				foreach (LayerInfo layer in layersAndLevels)
				{
					// Add the layer text
					Text header = MakeText(hellmapContainer);
					header.text = layer.layerName;
					header.fontSize = 36;
					header.font = Utils.gameFont;
					header.alignment = TextAnchor.MiddleLeft;
					header.color = Color.white;
					RectTransform textRect = header.GetComponent<RectTransform>();
					textRect.anchorMin = textRect.anchorMax = new Vector2(0, 1);
					textRect.sizeDelta = new Vector2(250, 50);
					textRect.pivot = new Vector2(0, 1);
					textRect.localScale = Vector3.one;

					// Add all levels
					foreach (string level in layer.layerLevels)
					{
						RectTransform levelContainer = MakeRect(hellmapContainer);
						levelContainer.anchorMin = levelContainer.anchorMax = new Vector2(0, 1);
						levelContainer.pivot = new Vector2(0.5f, 1);
						levelContainer.localScale = Vector3.one;

						RectTransform levelPanel = MakeRect(levelContainer.transform);
						levelPanel.anchorMin = levelPanel.anchorMax = new Vector2(0.5f, 0.5f);
						levelPanel.sizeDelta = new Vector2(25, 9);
						levelPanel.anchoredPosition = Vector2.zero;
						levelPanel.localScale = new Vector3(5, 5, 1);
						Image levelPanelImg = levelPanel.gameObject.AddComponent<Image>();
						levelPanelImg.type = Image.Type.Sliced;
						levelPanelImg.sprite = Utils.levelPanel;
						levelPanelImg.pixelsPerUnitMultiplier = 1;

						Text levelTxt = MakeText(levelContainer.transform);
						levelTxt.text = level;
						levelTxt.font = Utils.gameFont;
						levelTxt.fontSize = 32;
						levelTxt.alignment = TextAnchor.MiddleCenter;
						levelTxt.color = Color.black;
						RectTransform levelTxtRect = levelTxt.gameObject.GetComponent<RectTransform>();
						levelTxtRect.anchorMin = Vector2.zero;
						levelTxtRect.anchorMax = Vector2.one;
						levelTxtRect.pivot = new Vector2(0.5f, 0.5f);
						levelTxtRect.sizeDelta = Vector2.zero;
						levelTxtRect.anchoredPosition = new Vector2(0, 0);
						levelTxtRect.localScale = Vector3.one;

						levelContainer.sizeDelta = new Vector2(125, 45);
					}
				}

				// Add trigger to destroy the map
				ObjectActivator act = firstRoomInst.GetComponentsInChildren<PlayerActivator>().First().gameObject.AddComponent<ObjectActivator>();
				act.dontActivateOnEnable = true;
				act.oneTime = true;
				act.events = new UltrakillEvent();
				act.events.toDisActivateObjects = new GameObject[1] { hellmapContainer.gameObject };
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
