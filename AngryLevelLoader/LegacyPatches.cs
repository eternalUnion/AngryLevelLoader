using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader
{
	// LEGACY
	public static class LegacyPatchController
	{
		public static bool patched = false;
		public static bool enablePatches = false;
		public static void Patch()
		{
			if (patched)
				return;

			InitShaderDictionary();
			patched = true;
		}

		public static void ReplaceShaders()
		{
			foreach (Renderer rnd in Resources.FindObjectsOfTypeAll(typeof(Renderer)))
			{
				if (rnd.transform.parent != null && rnd.transform.parent.name == "Virtual Camera")
					continue;

				foreach (Material mat in rnd.materials)
				{
					if (shaderDictionary.TryGetValue(mat.shader.name, out Shader shader))
					{
						mat.shader = shader;
					}
				}
			}
		}

		public static void LinkMixers()
		{
			if (AudioMixerController.instance == null)
				return;

			AudioMixer[] realMixers = new AudioMixer[5]
			{
				AudioMixerController.instance.allSound,
				AudioMixerController.instance.musicSound,
				AudioMixerController.instance.goreSound,
				AudioMixerController.instance.doorSound,
				AudioMixerController.instance.unfreezeableSound
			};

			AudioMixer[] allMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();

			Dictionary<AudioMixerGroup, AudioMixerGroup> groupConversionMap = new Dictionary<AudioMixerGroup, AudioMixerGroup>();
			foreach (AudioMixer mixer in allMixers.Where(_mixer => _mixer.name.EndsWith("_rude")).AsEnumerable())
			{
				AudioMixerGroup rudeGroup = mixer.FindMatchingGroups("")[0];

				string realMixerName = mixer.name.Substring(0, mixer.name.Length - 5);
				AudioMixer realMixer = realMixers.Where(mixer => mixer.name == realMixerName).First();
				AudioMixerGroup realGroup = realMixer.FindMatchingGroups("")[0];

				groupConversionMap[rudeGroup] = realGroup;
				Debug.Log($"{mixer.name} => {realMixer.name}");
			}

			foreach (AudioSource source in Resources.FindObjectsOfTypeAll<AudioSource>())
			{
				if (source.outputAudioMixerGroup != null && groupConversionMap.TryGetValue(source.outputAudioMixerGroup, out AudioMixerGroup realGroup))
				{
					source.outputAudioMixerGroup = realGroup;
				}
			}
		}

		public static ResourceLocationMap resourceMap = null;
		private static void InitResourceMap()
		{
			if (resourceMap == null)
			{
				Addressables.InitializeAsync().WaitForCompletion();
				resourceMap = Addressables.ResourceLocators.First() as ResourceLocationMap;
			}
		}

		public static Dictionary<string, Shader> shaderDictionary = new Dictionary<string, Shader>();
		private static void InitShaderDictionary()
		{
			InitResourceMap();
			foreach (KeyValuePair<object, IList<IResourceLocation>> pair in resourceMap.Locations)
			{
				string path = pair.Key as string;
				if (!path.EndsWith(".shader"))
					continue;

				Shader shader = Addressables.LoadAssetAsync<Shader>(path).WaitForCompletion();
				shaderDictionary[shader.name] = shader;
			}

			shaderDictionary.Remove("ULTRAKILL/PostProcessV2");
		}
	}

	[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Shader))]
	public static class MaterialShaderPatch_Ctor0
	{
		[HarmonyPrefix]
		public static bool Prefix(ref Shader __0)
		{
			if (!LegacyPatchController.enablePatches)
				return true;

			if (__0 != null && __0.name != null && LegacyPatchController.shaderDictionary.TryGetValue(__0.name, out Shader shader))
			{
				__0 = shader;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Material))]
	public static class MaterialShaderPatch_Ctor1
	{
		[HarmonyPostfix]
		public static void Postfix(Material __instance)
		{
			if (!LegacyPatchController.enablePatches)
				return;

			if (__instance.shader != null && LegacyPatchController.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
			{
				__instance.shader = shader;
			}
		}
	}

	[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(string))]
	public static class MaterialShaderPatch_Ctor2
	{
		[HarmonyPostfix]
		public static void Postfix(Material __instance)
		{
			if (!LegacyPatchController.enablePatches)
				return;

			if (__instance.shader != null && LegacyPatchController.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
			{
				__instance.shader = shader;
			}
		}
	}

	[HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.RestartScene))]
	public static class SceneHelperRestart_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (!LegacyPatchController.enablePatches)
				return true;

			if (SceneManager.GetActiveScene().path != AngrySceneManager.CurrentScenePath)
				return true;

			foreach (MonoBehaviour monoBehaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
			{
				if (!(monoBehaviour == null) && !(monoBehaviour.gameObject.scene.name == "DontDestroyOnLoad"))
				{
					monoBehaviour.enabled = false;
				}
			}

			SceneManager.LoadScene(AngrySceneManager.CurrentScenePath);

			return false;
		}
	}
}
