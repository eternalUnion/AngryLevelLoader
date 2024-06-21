using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	public static class V4LegacyPatches
	{
		private const string TramPrefabPath = "Assets/Prefabs/Levels/Tram.prefab";
		private const string Fire32SpritePath = "Assets/Textures/Sprites/fire1_32.png";
		private const string Fire32AnimationPath = "Assets/Animations/fire1_32_0.controller";
		private const string FireMaterialPath = "Assets/Materials/FireMaterial.mat";

		private static GameObject tramZapObject;

		private static Sprite fireSpite32;
		private static RuntimeAnimatorController fireController32;
		private static Material fireMaterial;

		internal static void Init()
		{
			GameObject tramObject = Addressables.LoadAssetAsync<GameObject>(TramPrefabPath).WaitForCompletion();
			if (tramObject.transform.Find("Screen/ZapEffects") != null)
				tramZapObject = tramObject.transform.Find("Screen/ZapEffects").gameObject;

			fireSpite32 = Addressables.LoadAssetAsync<Sprite>(Fire32SpritePath).WaitForCompletion();
			fireController32 = Addressables.LoadAssetAsync<RuntimeAnimatorController>(Fire32AnimationPath).WaitForCompletion();
			fireMaterial = Addressables.LoadAssetAsync<Material>(FireMaterialPath).WaitForCompletion();
		}

		public static void PatchTramControl(TramControl __instance)
		{
			if (__instance.zapEffects != null || tramZapObject == null)
				return;

			GameObject zapObject = UnityEngine.Object.Instantiate(tramZapObject, __instance.transform);
			zapObject.transform.localPosition = new Vector3(1, 1, -0.5f);

			__instance.zapEffects = zapObject;
			__instance.zapLight = zapObject.GetComponentInChildren<Light>(true);
			__instance.zapSprite = zapObject.GetComponentInChildren<SpriteRenderer>(true);
			__instance.zapSound = zapObject.GetComponentInChildren<AudioSource>(true);
		}

		private class CheckRenderer : MonoBehaviour
		{
			private SpriteRenderer rend;

			private void Start()
			{
				rend = GetComponent<SpriteRenderer>();
			}

			private void Update()
			{
				if (rend == null)
				{
					enabled = false;
					return;
				}

				if (rend.sprite == null)
					return;

				if (rend.sprite != null && rend.sprite.name.StartsWith("fire1_16") && rend.gameObject.transform.localScale == Vector3.one * 24)
				{
					rend.sprite = fireSpite32;
					rend.sharedMaterial = fireMaterial;
					rend.gameObject.transform.localScale = Vector3.one * 8;
					rend.gameObject.transform.SetLocalEulerAngles(new Vector3(0, 180, 0), RotationOrder.OrderXYZ);
					rend.gameObject.transform.localPosition = new Vector3(0, 1.25f, 0);
					if (rend.gameObject.TryGetComponent(out Animator anim))
						anim.runtimeAnimatorController = fireController32;
				}

				Destroy(this);
				enabled = false;
			}
		}

		public static void AlwaysLookAtCameraOverwrite(AlwaysLookAtCamera __instance)
		{
			SpriteRenderer rend = __instance.GetComponentInChildren<SpriteRenderer>(true);
			if (rend == null)
				return;

			rend.gameObject.AddComponent<CheckRenderer>();
		}
	}
}
