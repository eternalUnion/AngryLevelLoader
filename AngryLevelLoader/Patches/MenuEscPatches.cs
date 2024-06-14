using HarmonyLib;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace AngryLevelLoader.Patches
{
	[HarmonyPatch(typeof(MenuEsc))]
	public class MenuEscPatches
	{
		[HarmonyPatch(nameof(MenuEsc.Update))]
		[HarmonyPrefix]
		public static bool OnMainPanelExit(MenuEsc __instance)
		{
			if (Plugin.config.rootPanel.currentPanel.gameObject != __instance.gameObject)
				return true;

			bool escape = MonoSingleton<InputManager>.Instance.InputSource.Pause.WasPerformedThisFrame || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.TryGetComponent<Slider>(out _));
			bool selectedObject = EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.TryGetComponent(out BackSelectOverride _);
			if (!escape || selectedObject)
				return true;

			if (!string.IsNullOrEmpty(Plugin.searchBar.value))
			{
				Plugin.searchBar.value = "";
				return false;
			}

			if (Plugin.folderStack.Count > 1)
			{
				Plugin.folderStack.Pop();
				Plugin.OpenFolder(Plugin.folderStack.Peek());
				return false;
			}

			return true;
		}
	}
}
