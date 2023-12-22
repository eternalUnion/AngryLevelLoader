using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Managers.LegacyPatches
{
	public enum LegacyPatchState
	{
		None,
		Ver2
	}

	public class LegacyPatchManager
	{
		public static LegacyPatchState patchState { get; private set; } = LegacyPatchState.None;
		public static Harmony legacyHarmony = new Harmony($"{Plugin.PLUGIN_GUID}_legacyPatches");

		internal static void Init()
		{
			V2LegacyAudioPatches.Init();
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
