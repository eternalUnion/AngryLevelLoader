using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.ObjectExplorer;
using UnityExplorer.UI;

namespace AngryLevelLoader.Managers.BannedMods
{
	public static class UnityExplorerSoftBan
	{
		public const string PLUGIN_GUID = "com.sinai.unityexplorer";
		private static bool currentlyBanned = false;

		public static bool UELoaded
		{
			get => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
		}

#if !DEBUG
		internal static void AngryClassFilterPatch(List<object> __result)
		{
			Assembly thisAssembly = Assembly.GetExecutingAssembly();

			__result.RemoveAll((o) =>
			{
				if (o is not Type type)
					return false;

				if (type.Assembly == thisAssembly)
					return true;

				return false;
			});
		}
#endif

		internal static void OnShowUI(bool value)
		{
			if (value && AngrySceneManager.isInCustomLevel)
				currentlyBanned = true;
		}

		internal static void Init()
		{
			try
			{
				SceneManager.sceneLoaded += (scene, mode) =>
				{
					if (mode == LoadSceneMode.Additive)
						return;

					currentlyBanned = false;
				};

#if !DEBUG
				Plugin.harmony.Patch(
					typeof(SearchProvider).GetMethod(nameof(SearchProvider.ClassSearch), BindingFlags.Static | BindingFlags.NonPublic),
					postfix: new HarmonyLib.HarmonyMethod(typeof(UnityExplorerSoftBan).GetMethod(nameof(AngryClassFilterPatch), BindingFlags.Static | BindingFlags.NonPublic)));

				Plugin.harmony.Patch(
					typeof(UIManager).GetProperty(nameof(UIManager.ShowMenu), BindingFlags.Public | BindingFlags.Static).SetMethod,
					postfix: new HarmonyLib.HarmonyMethod(typeof(UnityExplorerSoftBan).GetMethod(nameof(OnShowUI), BindingFlags.Static | BindingFlags.NonPublic)));
#endif
			}
			catch (Exception e)
			{
				Plugin.logger.LogError(e);
			}
		}

		public static SoftBanCheckResult Check()
		{
			if (currentlyBanned)
				return new SoftBanCheckResult(true, "Cannot post to leaderboards once Unity Explorer window is opened. Close the window and complete the level without opening UE again to post a record.");

			return new SoftBanCheckResult();
		}
	}
}
