using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using AngryLevelLoader.Managers;
using AngryLevelLoader.UserInterface;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(MenuEsc))]
	internal class MenuEscPatches
	{
		[HarmonyPatch(nameof(MenuEsc.Update))]
		[HarmonyPrefix]
		public static bool OnMainPanelExit(MenuEsc __instance)
		{
			if (ConfigManager.config.rootPanel.currentPanel.gameObject != __instance.gameObject)
				return true;

			bool escape = MonoSingleton<InputManager>.Instance.InputSource.Pause.WasPerformedThisFrame || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.TryGetComponent<Slider>(out _));
			bool selectedObject = EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.TryGetComponent(out BackSelectOverride _);
			if (!escape || selectedObject)
				return true;

			if (!string.IsNullOrEmpty(ConfigManager.searchBar.value))
			{
				ConfigManager.searchBar.value = "";
				return false;
			}

			if (AngryBundleList.PopFolder())
				return false;

			return true;
		}
	}
}
