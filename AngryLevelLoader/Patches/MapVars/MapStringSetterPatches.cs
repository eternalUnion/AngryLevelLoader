using AngryLevelLoader.Managers;
using HarmonyLib;
using Logic;

namespace AngryLevelLoader.Patches.MapVars
{
    [HarmonyPatch(typeof(MapStringSetter))]
    public static class MapStringSetterPatches
    {
        [HarmonyPatch(nameof(MapStringSetter.SetVar))]
        [HarmonyPrefix]
        public static bool OnSetVar(MapStringSetter __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            string value = ResolveSetValue(__instance);
            AngryMapVarManager.Instance.SetString(__instance.variableName, value, __instance.persistence);
            return false;
        }

        private static string ResolveSetValue(MapStringSetter setter)
        {
            switch (setter.inputType)
            {
                case StringInputType.JustText:
                    return setter.textValue;
                case StringInputType.CopyDifferentVariable:
                    switch (setter.sourceVariableType)
                    {
                        case VariableType.Int:
                            return MapVarManager.Instance.GetInt(setter.sourceVariableName).ToString();
                        case VariableType.Float:
                            return MapVarManager.Instance.GetFloat(setter.sourceVariableName).ToString();
                        case VariableType.Bool:
                            return MapVarManager.Instance.GetBool(setter.sourceVariableName).ToString();
                        case VariableType.String:
                            return MapVarManager.Instance.GetString(setter.sourceVariableName);
                        default:
                            return null;
                    }
                default:
                    return null;
            }

        }
    }
}
