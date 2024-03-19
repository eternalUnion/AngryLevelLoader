using AngryLevelLoader.Managers;
using HarmonyLib;
using Logic;

namespace AngryLevelLoader.Patches.MapVars
{
    [HarmonyPatch(typeof(MapIntSetter))]
    public static class MapIntSetterPatches
    {
        [HarmonyPatch(nameof(MapIntSetter.SetVar))]
        [HarmonyPrefix]
        public static bool OnSetVar(MapIntSetter __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;
            
            int value = ResolveSetValue(__instance);
            AngryMapVarManager.Instance.SetInt(__instance.variableName, value, __instance.persistence);
            return false;
        }

        private static int ResolveSetValue(MapIntSetter intSetter)
        {
            switch (intSetter.inputType)
            {
                case IntInputType.SetToNumber:
                    return intSetter.number;
                case IntInputType.AddNumber:
                    return intSetter.number + (MapVarManager.Instance.GetInt(intSetter.variableName) ?? 0);
                case IntInputType.RandomRange:
                    return UnityEngine.Random.Range(intSetter.min, intSetter.max);
                case IntInputType.RandomFromList:
                    return intSetter.list[UnityEngine.Random.Range(0, intSetter.list.Length)];
                case IntInputType.CopyDifferentVariable:
                    return MapVarManager.Instance.GetInt(intSetter.sourceVariableName) ?? -1;
                default:
                    return 0;
            }
        }
    }
}
