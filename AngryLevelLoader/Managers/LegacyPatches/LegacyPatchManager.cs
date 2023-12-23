using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
		public const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		public const BindingFlags STATIC = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		public static LegacyPatchState patchState { get; private set; } = LegacyPatchState.None;
		public static Harmony legacyHarmony = new Harmony($"{Plugin.PLUGIN_GUID}_legacyPatches");

		internal static void Init()
		{
			V2LegacyAudioPatches.Init();
			V2LegacyEnemyPatches.Init();
		}

		public static void SetLegacyPatchState(LegacyPatchState state)
		{
			if (patchState == state)
				return;

			patchState = state;
			legacyHarmony.UnpatchSelf();

			if (state == LegacyPatchState.Ver2)
			{
				legacyHarmony.Patch(typeof(Drone).GetMethod(nameof(Drone.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyEnemyPatches).GetMethod(nameof(V2LegacyEnemyPatches.FixDrone), STATIC)));

				legacyHarmony.Patch(typeof(StatueBoss).GetMethod(nameof(StatueBoss.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyEnemyPatches).GetMethod(nameof(V2LegacyEnemyPatches.FixStatueBoss), STATIC)));

				legacyHarmony.Patch(typeof(Streetcleaner).GetMethod(nameof(Streetcleaner.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyEnemyPatches).GetMethod(nameof(V2LegacyEnemyPatches.FixStreetCleaner), STATIC)));

				legacyHarmony.Patch(typeof(SpiderBody).GetMethod(nameof(SpiderBody.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyEnemyPatches).GetMethod(nameof(V2LegacyEnemyPatches.FixSpider), STATIC)));

				legacyHarmony.Patch(typeof(SwordsMachine).GetMethod(nameof(SwordsMachine.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyEnemyPatches).GetMethod(nameof(V2LegacyEnemyPatches.FixSwordsMachine), STATIC)));

				legacyHarmony.Patch(typeof(Mindflayer).GetMethod(nameof(Mindflayer.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyEnemyPatches).GetMethod(nameof(V2LegacyEnemyPatches.FixMindflayer), STATIC)));

				legacyHarmony.Patch(typeof(Stalker).GetMethod(nameof(Stalker.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyEnemyPatches).GetMethod(nameof(V2LegacyEnemyPatches.FixStalker), STATIC)));

				legacyHarmony.Patch(typeof(HookPoint).GetMethod(nameof(HookPoint.Start), INSTANCE),
					prefix: new HarmonyMethod(typeof(V2LegacyHookPointPatches).GetMethod(nameof(V2LegacyHookPointPatches.FixSlingshots), STATIC)));

				legacyHarmony.Patch(typeof(CheckPoint).GetMethod(nameof(CheckPoint.Start), INSTANCE),
						prefix: new HarmonyMethod(typeof(V2LegacyCheckpointPatches).GetMethod(nameof(V2LegacyCheckpointPatches.FixCheckpoint), STATIC)));

				legacyHarmony.Patch(typeof(RevolverBeam).GetMethod(nameof(RevolverBeam.Start), INSTANCE),
						prefix: new HarmonyMethod(typeof(V2LegacyRevolverBeamPatches).GetMethod(nameof(V2LegacyRevolverBeamPatches.FixBeam), STATIC)));
			}
		}
	}
}
