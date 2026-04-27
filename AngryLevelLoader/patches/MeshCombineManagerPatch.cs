using HarmonyLib;
using LucasMeshCombine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AngryLevelLoader.Patches
{
	// To prevent error if the shader is not attached

	[HarmonyPatch(typeof(MeshCombineManager))]
	internal static class MeshCombineManagerPatches
	{
		static Shader atlasedShader;
		static Shader vertexlitShader;

		public static void Initialize()
		{
            if (atlasedShader == null)
            {
                var handler = Addressables.LoadAssetAsync<Shader>("Assets/Shaders/Main/ULTRAKILL-vertexlit-atlas.shader");
                handler.Completed += (s) =>
                {
                    if (handler.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
                        return;

                    atlasedShader = handler.Result;
                };

            }

            if (vertexlitShader == null)
            {
                var handler = Addressables.LoadAssetAsync<Shader>("Assets/Shaders/Main/ULTRAKILL-vertexlit.shader");
                handler.Completed += (s) =>
                {
                    if (handler.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
                        return;

                    vertexlitShader = handler.Result;
                };

            }
        }

        [HarmonyPatch(nameof(MeshCombineManager.Awake))]
        [HarmonyPrefix]
        static bool LinkNecessaryShaders(MeshCombineManager __instance)
		{
			if (__instance.atlasedShader == null)
                __instance.atlasedShader = atlasedShader;

			if (__instance.allowedShadersToBatch == null || __instance.allowedShadersToBatch.Length == 0)
                __instance.allowedShadersToBatch = new Shader[] { vertexlitShader };

			return true;
		}
	}
}
