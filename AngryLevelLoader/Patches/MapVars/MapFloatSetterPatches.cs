using AngryLevelLoader.Managers;
using HarmonyLib;
using Logic;

namespace AngryLevelLoader.Patches.MapVars
{
    [HarmonyPatch(typeof(MapFloatSetter))]
    public static class MapFloatSetterPatches
    {
        [HarmonyPatch(nameof(MapFloatSetter.SetVar))]
        [HarmonyPrefix]
        public static bool OnSetVar(MapFloatSetter __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            float value = ResolveSetValue(__instance);
            AngryMapVarManager.Instance.SetFloat(__instance.variableName, value, __instance.persistence);
            return false;
        }

        private static float ResolveSetValue(MapFloatSetter setter)
        {
            switch (setter.inputType)
            {
                case FloatInputType.SetToNumber:
                    return setter.number;
                case FloatInputType.AddNumber:
                    return setter.number + (MapVarManager.Instance.GetFloat(setter.variableName) ?? 0);
                case FloatInputType.RandomRange:
                    return UnityEngine.Random.Range(setter.min, setter.max);
                case FloatInputType.RandomFromList:
                    return setter.list[UnityEngine.Random.Range(0, setter.list.Length)];
                case FloatInputType.CopyDifferentVariable:
                    return MapVarManager.Instance.GetFloat(setter.sourceVariableName) ?? -1;
                case FloatInputType.MultiplyByNumber:
                    return setter.number * (MapVarManager.Instance.GetFloat(setter.variableName) ?? 1f);
                case FloatInputType.MultiplyByVariable:
                    return (MapVarManager.Instance.GetFloat(setter.variableName) ?? 1f) * MapVarManager.Instance.GetFloat(setter.sourceVariableName) ?? 1f;
                default:
                    return 0;
            }
        }
    }
}
