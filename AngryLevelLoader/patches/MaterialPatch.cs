using HarmonyLib;
using UnityEngine;

namespace AngryLevelLoader.patches
{
	/*[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Shader))]
	public static class MaterialShaderPatch_Ctor0
	{
		[HarmonyPrefix]
		public static bool Prefix(ref Shader __0)
		{
			if (__0 != null && __0.name != null && Plugin.shaderDictionary.TryGetValue(__0.name, out Shader shader))
			{
				__0 = shader;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(Material))]
	public static class MaterialShaderPatch_Ctor1
	{
		[HarmonyPostfix]
		public static void Postfix(Material __instance)
		{
			if (__instance.shader != null && Plugin.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
			{
				__instance.shader = shader;
			}
		}
	}

	[HarmonyPatch(typeof(Material), MethodType.Constructor, typeof(string))]
	public static class MaterialShaderPatch_Ctor2
	{
		[HarmonyPostfix]
		public static void Postfix(Material __instance)
		{
			if (__instance.shader != null && Plugin.shaderDictionary.TryGetValue(__instance.shader.name, out Shader shader))
			{
				__instance.shader = shader;
			}
		}
	}*/
}