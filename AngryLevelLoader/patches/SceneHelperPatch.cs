using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace AngryLevelLoader.patches
{
	[HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.LoadScene))]
	class SceneHelper_LoadScene_Patch
	{
		static bool Prefix(ref string __0)
		{
			if (Plugin.isInCustomScene && __0 == "")
			{
				__0 = "Main Menu";
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(SceneHelper), nameof(SceneHelper.RestartScene))]
	public static class SceneHelperRestart_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (SceneManager.GetActiveScene().path != AngryBundleContainer.lastLoadedScenePath)
				return true;

			foreach (MonoBehaviour monoBehaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
			{
				if (!(monoBehaviour == null) && !(monoBehaviour.gameObject.scene.name == "DontDestroyOnLoad"))
				{
					monoBehaviour.enabled = false;
				}
			}

			SceneManager.LoadScene(AngryBundleContainer.lastLoadedScenePath);

			return false;
		}
	}
}
