using AngryLevelLoader.Managers;
using HarmonyLib;
using Logic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Patches
{
    [HarmonyPatch(typeof(SceneHelper))]
	internal static class SceneHelperPatches
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


		[HarmonyPatch(nameof(SceneHelper.RestartSceneAsync))]
		[HarmonyPrefix]
		public static bool ChangeSceneNameBeforeLoad(SceneHelper __instance)
		{
			if (!AngrySceneManager.isInCustomLevel)
				return true;

			Time.timeScale = 0f;
			SceneHelper.PendingScene = AngrySceneManager.currentLevelData.uniqueIdentifier;

			foreach (MonoBehaviour monoBehaviour in Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (!(monoBehaviour == null) && !(monoBehaviour.gameObject.scene.name == "DontDestroyOnLoad"))
                {
                    monoBehaviour.enabled = false;
					monoBehaviour.CancelInvoke();
				}
            }

            if (string.IsNullOrEmpty(SceneHelper.CurrentScene))
            {
                SceneHelper.CurrentScene = AngrySceneManager.currentLevelData.uniqueIdentifier;
			}

			//call will be rerouted to AngryMapVarManager.
			MapVarManager.Instance?.ReloadMapVars();

			if (SceneHelper.Instance.loadingBlocker != null)
				SceneHelper.Instance.loadingBlocker.SetActive(true);

			Addressables.LoadSceneAsync(AngrySceneManager.currentLevelData.scenePath, LoadSceneMode.Single, true, 100).Completed += (scene) =>
			{
				if (SceneHelper.Instance.preloadingBadge != null)
					SceneHelper.Instance.preloadingBadge.SetActive(false);
				if (SceneHelper.Instance.loadingBlocker != null)
					SceneHelper.Instance.loadingBlocker.SetActive(false);
				if (SceneHelper.Instance.loadingBar != null)
					SceneHelper.Instance.loadingBar.gameObject.SetActive(false);

				Time.timeScale = 1f;
				SceneHelper.PendingScene = null;
			};

            return false;
		}

		internal static bool forceDisableIsInCustomLevel = false;
		[HarmonyPatch(nameof(SceneHelper.IsPlayingCustom), MethodType.Getter)]
		[HarmonyPrefix]
		public static bool OverwriteIsInCustomLevel(ref bool __result)
		{
			if (AngrySceneManager.isInCustomLevel)
			{
				__result = !forceDisableIsInCustomLevel;
				return false;
			}

			return true;
		}

		[HarmonyPatch(nameof(SceneHelper.CurrentLevelNumber), MethodType.Getter)]
		[HarmonyPrefix]
		public static bool OverwriteGetCurrentLevelNumber(ref int __result)
		{
			if (AngrySceneManager.isInCustomLevel)
			{
				__result = -1;
				return false;
			}

			return true;
		}

		[HarmonyPatch(nameof(SceneHelper.OnSceneLoaded))]
		[HarmonyPrefix]
		public static bool AvoidSceneSetup()
		{
			forceDisableIsInCustomLevel = true;
			return true;
		}

		[HarmonyPatch(nameof(SceneHelper.OnSceneLoaded))]
		[HarmonyPostfix]
		public static void PostAvoidSceneSetup()
		{
			forceDisableIsInCustomLevel = false;
		}

        [HarmonyPatch(nameof(SceneHelper.IsSceneRankless), MethodType.Getter)]
        [HarmonyPostfix]
        public static void IsSceneRanklessFix(ref bool __result)
        {
            if (AngrySceneManager.isInCustomLevel)
            {
				__result = AngrySceneManager.currentLevelContainer.FinalRank != '-';
            }
        }
    }
}
