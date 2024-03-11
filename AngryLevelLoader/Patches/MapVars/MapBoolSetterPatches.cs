using AngryLevelLoader.Managers;
using HarmonyLib;
using Logic;

namespace AngryLevelLoader.Patches.MapVars
{
    [HarmonyPatch(typeof(MapBoolSetter))]
    public static class MapBoolSetterPatches
    {
        [HarmonyPatch(nameof(MapBoolSetter.SetVar))]
        [HarmonyPrefix]
        public static bool OnSetVar(MapBoolSetter __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            bool value = ResolveSetValue(__instance);
            AngryMapVarManager.Instance.SetBool(__instance.variableName, value, __instance.persistence);
            return false;
        }

        private static bool ResolveSetValue(MapBoolSetter setter)
        {
            switch (setter.inputType)
            {
                case BoolInputType.Set:
                    return setter.value;
                case BoolInputType.Toggle:
                    return !MapVarManager.Instance.GetBool(setter.variableName) ?? false;
                default:
                    return false;
            }
        }
    }
}
