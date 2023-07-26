using HarmonyLib;
using LucasMeshCombine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.patches
{
	// To prevent error if the shader is not attached

	[HarmonyPatch(typeof(MeshCombineManager), nameof(MeshCombineManager.Awake))]
	public static class MeshCombineManager_Awake_Patch
	{
		static bool Prefix(MeshCombineManager __instance)
		{
			if (__instance.atlasedShader == null)
			{
				__instance.atlasedShader = Addressables.LoadAssetAsync<Shader>("Assets/Shaders/Main/ULTRAKILL-vertexlit-atlas.shader").WaitForCompletion();
			}

			if (__instance.allowedShadersToBatch == null || __instance.allowedShadersToBatch.Length == 0)
			{
				__instance.allowedShadersToBatch = new Shader[] { Addressables.LoadAssetAsync<Shader>("Assets/Shaders/Main/ULTRAKILL-vertexlit.shader").WaitForCompletion() };
			}

			return true;
		}
	}
}
