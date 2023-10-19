using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using AngryLevelLoader.Managers;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(SceneHelper))]
	public class SceneHelper_LoadScene_Patch
	{
		[HarmonyPatch(nameof(SceneHelper.LoadScene))]
		[HarmonyPrefix]
		public static bool GoToMainMenuIfNoSceneFound(ref string __0)
		{
			if (AngrySceneManager.isInCustomLevel && __0 == "")
			{
				__0 = "Main Menu";
			}

			return true;
		}

		[HarmonyPatch(nameof(SceneHelper.RestartScene))]
		[HarmonyPrefix]
		public static bool ChangeSceneNameBeforeLoad(SceneHelper __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
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
                AngrySceneManager.SceneHelper_CurrentScene.SetValue(null, AngrySceneManager.currentLevelData.uniqueIdentifier);
            }
            Addressables.LoadSceneAsync(AngrySceneManager.currentLevelData.scenePath, LoadSceneMode.Single, true, 100).Completed += (scene) =>
			{
				if (SceneHelper.Instance.loadingBlocker != null)
					SceneHelper.Instance.loadingBlocker.SetActive(false);
			};
			if (SceneHelper.Instance.loadingBlocker != null)
				SceneHelper.Instance.loadingBlocker.SetActive(true);

			return false;
		}
	}
}
