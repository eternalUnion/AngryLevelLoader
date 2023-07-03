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
		[HideInInspector]
		internal class PlayerForcedMovement : MonoBehaviour
		{
			public NewMovement player;
			private Rigidbody rb;

			public void Awake()
			{
				if (player == null)
					player = NewMovement.Instance;
			
				rb = player.GetComponent<Rigidbody>();
				rb.useGravity = false;
			}

			public static float defaultMoveForce = 78.5f;
			public float force = defaultMoveForce;
			public void LateUpdate()
			{
				rb.velocity = new Vector3(0, force, 0);
			}

			public void DestroyComp()
			{
				rb.useGravity = true;
				Destroy(this);
			}
		}

		[HideInInspector]
		private class LocalMoveTowards : MonoBehaviour
		{
			public Vector3 targetLocalPosition;

			public bool active = false;
			public float speed = 10;
			public void Update()
			{
				if (!active)
					return;

				transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetLocalPosition, Time.deltaTime * speed);
				if (transform.localPosition == targetLocalPosition)
					Destroy(this);
			}

			public void Activate()
			{
				active = true;
			}
		}

		[HideInInspector]
		private class CustomHellmapCursor : MonoBehaviour
		{
			public Vector2 targetPosition;
			public Image targetImage;
			public AudioSource aud;

			bool white = true;
			RectTransform rect;

			private void Start()
			{
				rect = GetComponent<RectTransform>();
				Invoke("FlashImage", 0.075f);
			}

			private void Update()
			{
				rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, targetPosition, Time.deltaTime * 4f * Vector3.Distance(rect.anchoredPosition, targetPosition));
			}

			private void FlashImage()
			{
				if (white)
				{
					white = false;
					targetImage.color = new Color(0f, 0f, 0f, 0f);
					if (!base.gameObject.activeSelf)
					{
						return;
					}
					aud.Play();
				}
				else
				{
					white = true;
					targetImage.color = Color.white;
				}
				if (gameObject.activeInHierarchy)
				{
					Invoke("FlashImage", 0.075f);
				}
			}
		}

		public bool secretRoom = false;
		public bool convertToUpwardRoom = false;
		public AudioClip upwardRoomDoorCloseClip;
		public List<GameObject> upwardRoomOutOfBoundsToDisable;

		[Header("Player Fields")]
		public CameraClearFlags cameraFillMode = CameraClearFlags.SolidColor;
		public Color backgroundColor = Color.black;

		[Header("Level Fields")]
		public bool displayLevelTitle = true;
		public bool startMusic = true;

		[Header("Hellmap")]
		public bool enableHellMap = false;
		public AudioClip hellmapBeepClip;
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
		public int layerIndexToEndAt;
		public int levelIndexToEndAt;

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

		public static float upDisablePos = 80;
		public static float doorClosePos = 10;
		public static float doorCloseSpeed = 10;
		public static float actDelay = 0.5f;
		public static void ConvertToAscendingFirstRoom(GameObject firstRoom, AudioClip doorCloseAud, List<GameObject> toEnable, List<GameObject> toDisable)
		{
			Transform room = firstRoom.transform.Find("Room");

			Transform pit = room.Find("Pit (3)");
			pit.transform.localPosition = new Vector3(0, 2, 41.72f);
			pit.transform.localRotation = Quaternion.Euler(0, 0, 180);

			Destroy(room.transform.Find("Room/Ceiling").gameObject);

			Transform floor = room.transform.Find("Room/Floor");
			GameObject refTile = floor.GetChild(0).gameObject;

			GameObject t1 = GameObject.Instantiate(refTile, floor);
			t1.transform.localPosition = new Vector3(-15, 9.7f, 20.28f);
			t1.transform.localRotation = Quaternion.identity;

			GameObject t2 = GameObject.Instantiate(refTile, floor);
			t2.transform.localPosition = new Vector3(5, 9.7f, 20.28f);
			t2.transform.localRotation = Quaternion.identity;

			GameObject t3 = GameObject.Instantiate(refTile, floor);
			t3.transform.localPosition = new Vector3(-5, 9.7f, 0.2f);
			t3.transform.localRotation = Quaternion.Euler(0, -90, 0);

			GameObject t4 = GameObject.Instantiate(refTile, floor);
			t4.transform.localPosition = new Vector3(5, 9.7f, 10.28f);
			t4.transform.localRotation = Quaternion.identity;
			t4.GetComponent<MeshRenderer>().materials = new Material[2] { Utils.metalDec20, Utils.metalDec20 };

			GameObject t5 = GameObject.Instantiate(refTile, floor);
			t5.transform.localPosition = new Vector3(-15, 9.7f, 10.28f);
			t5.transform.localRotation = Quaternion.identity;
			t5.GetComponent<MeshRenderer>().materials = new Material[2] { Utils.metalDec20, Utils.metalDec20 };

			GameObject t6 = GameObject.Instantiate(refTile, floor);
			t6.transform.localPosition = new Vector3(-5, -0.3f, 20.28f);
			t6.transform.localRotation = Quaternion.Euler(0, -90, -180);

			Transform decorations = room.Find("Decorations");
			Transform floorTile = decorations.GetChild(12);
			floorTile.localPosition = new Vector3(-5, 2, 52);
			LocalMoveTowards floorMover = floorTile.gameObject.AddComponent<LocalMoveTowards>();
			floorMover.targetLocalPosition = new Vector3(-5, 2, 42);
			floorMover.speed = doorCloseSpeed;
			AudioSource floorTileAud = floorTile.gameObject.AddComponent<AudioSource>();
			floorTileAud.playOnAwake = false;
			floorTileAud.loop = false;
			floorTileAud.clip = doorCloseAud;

			PlayerActivator act = firstRoom.GetComponentsInChildren<PlayerActivator>().First();
			act.gameObject.SetActive(false);

			NewMovement player = NewMovement.instance;
			player.transform.localPosition = new Vector3(player.transform.localPosition.x, -107, player.transform.localPosition.z);
			PlayerForcedMovement focedMov = player.gameObject.AddComponent<PlayerForcedMovement>();
			
			// Upward disabler
			GameObject upDisabler = new GameObject();
			upDisabler.transform.SetParent(act.transform.parent);
			upDisabler.transform.localPosition = new Vector3(0, upDisablePos, 0);
			upDisabler.transform.localRotation = Quaternion.identity;
			upDisabler.transform.localScale = new Vector3(80, 0.2f, 80);
			upDisabler.layer = act.gameObject.layer;
			BoxCollider upDisablerCol = upDisabler.AddComponent<BoxCollider>();
			upDisablerCol.isTrigger = true;
			ObjectActivator upDisablerA1 = upDisabler.AddComponent<ObjectActivator>();
			upDisablerA1.dontActivateOnEnable = true;
			upDisablerA1.oneTime = true;
			upDisablerA1.events = new UltrakillEvent();
			upDisablerA1.events.onActivate = new UnityEngine.Events.UnityEvent();
			upDisablerA1.events.onActivate.AddListener(() => focedMov.DestroyComp());

			// Door closer
			GameObject closer = new GameObject();
			closer.transform.SetParent(act.transform.parent);
			closer.transform.localPosition = new Vector3(0, doorClosePos, 0);
			closer.transform.localRotation = Quaternion.identity;
			closer.transform.localScale = new Vector3(80, 0.2f, 80);
			closer.layer = act.gameObject.layer;
			BoxCollider closerCol = closer.AddComponent<BoxCollider>();
			closerCol.isTrigger = true;
			ObjectActivator closerA1 = closer.AddComponent<ObjectActivator>();
			closerA1.dontActivateOnEnable = true;
			closerA1.oneTime = true;
			closerA1.events = new UltrakillEvent();
			closerA1.events.onActivate = new UnityEngine.Events.UnityEvent();
			closerA1.events.onActivate.AddListener(() => floorMover.Activate());
			closerA1.events.onActivate.AddListener(() => floorTileAud.Play());
			closerA1.events.onActivate.AddListener(() =>
			{
				foreach (GameObject o in toEnable)
					o.SetActive(true);

				foreach (GameObject o in toDisable)
					o.SetActive(false);
			});
			ObjectActivator closerA2 = closer.AddComponent<ObjectActivator>();
			closerA2.dontActivateOnEnable = true;
			closerA2.oneTime = true;
			closerA2.events = new UltrakillEvent();
			closerA2.events.onActivate = new UnityEngine.Events.UnityEvent();
			closerA2.events.onActivate.AddListener(() => act.gameObject.SetActive(true));
			closerA2.delay = actDelay;
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

				GameObject hellmapObj = null;
				if (enableHellMap)
				{
					Deserialize();

					Transform canvas = NewMovement.instance.transform.Find("Canvas") ?? SceneManager.GetActiveScene().GetRootGameObjects().Where(o => o.name == "Canvas").First().transform;

					RectTransform hellmap = MakeRect(canvas);
					hellmap.name = "Hellmap";
					hellmapObj = hellmap.gameObject;
					hellmap.anchorMin = hellmap.anchorMax = new Vector2(0.5f, 0.5f);
					hellmap.pivot = new Vector2(0.5f, 0.5f);
					hellmap.sizeDelta = new Vector2(250, 650);
					hellmap.anchoredPosition = Vector2.zero;
					hellmap.localScale = Vector3.one;
					hellmap.SetAsFirstSibling();
					RectTransform hellmapContainer = MakeRect(hellmap.transform);
					hellmapContainer.name = "Hellmap Container";
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

					LayoutRebuilder.ForceRebuildLayoutImmediate(hellmapContainer);

					int GetChildIndexFromLayerAndLevel(int layer, int level)
					{
						int index = 0;
						for (int i = 0; i < layer; i++)
							index += 1 + levelSizes[i];
						return index + 1 + level;
					}

					Vector2 startLevelPosition = hellmapContainer.GetChild(GetChildIndexFromLayerAndLevel(layerIndexToStartFrom, levelIndexToStartFrom)).GetComponent<RectTransform>().anchoredPosition;
					startLevelPosition = new Vector2(35, startLevelPosition.y - 22.5f);
					Vector2 endLevelPosition = hellmapContainer.GetChild(GetChildIndexFromLayerAndLevel(layerIndexToEndAt, levelIndexToEndAt)).GetComponent<RectTransform>().anchoredPosition;
					endLevelPosition = new Vector2(35, endLevelPosition.y - 22.5f);

					RectTransform cursor = MakeRect(hellmap);
					cursor.anchorMin = cursor.anchorMax = new Vector2(0, 1);
					cursor.pivot = new Vector2(0.5f, 0.5f);
					cursor.sizeDelta = new Vector2(35, 35);
					cursor.rotation = Quaternion.Euler(0, 0, -90);
					cursor.localScale = Vector3.one;
					cursor.anchoredPosition = startLevelPosition;
					AudioSource aud = cursor.gameObject.AddComponent<AudioSource>();
					aud.playOnAwake = false;
					aud.loop = false;
					aud.clip = hellmapBeepClip;
					Image cursorImg = cursor.gameObject.AddComponent<Image>();
					cursorImg.sprite = Utils.hellmapArrow;
					CustomHellmapCursor cursorComp = cursor.gameObject.AddComponent<CustomHellmapCursor>();
					cursorComp.targetPosition = endLevelPosition;
					cursorComp.aud = aud;
					cursorComp.targetImage = cursorImg;

					// Add trigger to destroy the map
					ObjectActivator act = UnityUtils.GetComponentInChildrenRecursive<PlayerActivator>(firstRoomInst.transform).gameObject.AddComponent<ObjectActivator>();
					act.dontActivateOnEnable = true;
					act.oneTime = true;
					act.events = new UltrakillEvent();
					act.events.toDisActivateObjects = new GameObject[1] { hellmapContainer.gameObject };
				}

				if (convertToUpwardRoom)
				{
					foreach (GameObject outOfBounds in upwardRoomOutOfBoundsToDisable)
						outOfBounds.SetActive(false);

					List<GameObject> toDisable = new List<GameObject>();
					if (hellmapObj != null)
						toDisable.Add(hellmapObj);

					List<GameObject> toEnable = new List<GameObject>();
					toEnable.AddRange(upwardRoomOutOfBoundsToDisable);

					ConvertToAscendingFirstRoom(firstRoomInst, upwardRoomDoorCloseClip, toEnable, toDisable);
				}
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
