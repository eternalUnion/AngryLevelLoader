using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Train;
using UnityEngine;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	public enum LegacyPatchState
	{
		None,
	}

	public class LegacyPatchManager
	{
		public const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		public const BindingFlags STATIC = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		public static LegacyPatchState patchState { get; private set; } = LegacyPatchState.None;
		public static Harmony legacyHarmony = new Harmony($"{Plugin.PLUGIN_GUID}_legacyPatches");

		internal static void Init()
		{
		}

		public static void SetLegacyPatchState(LegacyPatchState state)
		{
			if (patchState == state)
				return;

			patchState = state;
			legacyHarmony.UnpatchSelf();
		}
	}
}
