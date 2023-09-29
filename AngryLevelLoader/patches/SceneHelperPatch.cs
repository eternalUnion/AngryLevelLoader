using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(SceneHelper))]
	public class SceneHelper_LoadScene_Patch
	{
		[HarmonyPatch(nameof(SceneHelper.LoadScene))]
		[HarmonyPrefix]
		public static bool GoToMainMenuIfNoSceneFound(ref string __0)
		{
			if (Plugin.isInCustomScene && __0 == "")
			{
				__0 = "Main Menu";
			}

			return true;
		}

		[HarmonyPatch(nameof(SceneHelper.RestartScene))]
		[HarmonyPrefix]
		public static bool ChangeSceneNameBeforeLoad(SceneHelper __instance)
		{
			if (!Plugin.isInCustomScene)
				return true;

            foreach (MonoBehaviour monoBehaviour in Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (!(monoBehaviour == null) && !(monoBehaviour.gameObject.scene.name == "DontDestroyOnLoad"))
                {
                    monoBehaviour.enabled = false;
                }
            }
            if (string.IsNullOrEmpty(SceneHelper.CurrentScene))
            {
				Plugin.SceneHelper_CurrentScene.SetValue(null, Plugin.currentLevelData.uniqueIdentifier);
            }
            Addressables.LoadSceneAsync(Plugin.currentLevelData.scenePath, LoadSceneMode.Single, true, 100).WaitForCompletion();

            return false;
		}
	}
}
