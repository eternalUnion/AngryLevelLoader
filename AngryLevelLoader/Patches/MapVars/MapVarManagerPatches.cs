using AngryLevelLoader.Managers;
using HarmonyLib;
using Logic;
using System.Collections.Generic;

namespace AngryLevelLoader.Patches.MapVars
{
    [HarmonyPatch(typeof(MapVarManager))]
    public static class MapVarManagerPatches
    {
        //Add the AngryMapVarManager to the MapVarManager on Start.
        [HarmonyPatch(nameof(MapVarManager.Start))]
        [HarmonyPostfix]
        public static void InstanceAngryMapVarManager(MapVarManager __instance)
        {
            if(!__instance.TryGetComponent<AngryMapVarManager>(out AngryMapVarManager angryMapVarManager))
                __instance.gameObject.AddComponent<AngryMapVarManager>();
        }


        //Reroute some calls to the AngryMapVarManager
        [HarmonyPatch(nameof(MapVarManager.StashStore))]
        [HarmonyPrefix]
        public static bool OnStashStore(MapVarManager __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            if (!__instance.TryGetComponent<AngryMapVarManager>(out AngryMapVarManager angryMapVarManager))
                __instance.gameObject.AddComponent<AngryMapVarManager>();

            angryMapVarManager.StashStore();
            return false;
        }

        [HarmonyPatch(nameof(MapVarManager.RestoreStashedStore))]
        [HarmonyPrefix]
        public static bool OnRestoreStore(MapVarManager __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            if (!__instance.TryGetComponent<AngryMapVarManager>(out AngryMapVarManager angryMapVarManager))
                __instance.gameObject.AddComponent<AngryMapVarManager>();

            angryMapVarManager.RestoreStashedStore();
            return false;
        }

        [HarmonyPatch(nameof(MapVarManager.ReloadMapVars))]
        [HarmonyPrefix]
        public static bool OnReloadMapVars(MapVarManager __instance)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            if(!__instance.TryGetComponent<AngryMapVarManager>(out AngryMapVarManager angryMapVarManager))
                __instance.gameObject.AddComponent<AngryMapVarManager>();

            angryMapVarManager.ReloadMapVars();
            return false;
        }

        //Replace the getters with the Angry ones
        [HarmonyPatch(nameof(MapVarManager.GetInt))]
        [HarmonyPostfix]
        public static void OnGetInt(string key, ref int? __result)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return;

            __result = AngryMapVarManager.Instance.GetInt(key);
        }

        [HarmonyPatch(nameof(MapVarManager.GetBool))]
        [HarmonyPostfix]
        public static void OnGetBool(string key, ref bool? __result)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return;

            __result = AngryMapVarManager.Instance.GetBool(key);
        }

        [HarmonyPatch(nameof(MapVarManager.GetFloat))]
        [HarmonyPostfix]
        public static void OnGetFloat(string key, ref float? __result)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return;

            __result = AngryMapVarManager.Instance.GetFloat(key);
        }

        [HarmonyPatch(nameof(MapVarManager.GetString))]
        [HarmonyPostfix]
        public static void OnGetString(string key, ref string __result)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return;

            __result = AngryMapVarManager.Instance.GetString(key);
        }

        [HarmonyPatch(nameof(MapVarManager.GetAllVariables))]
        [HarmonyPostfix]
        public static void OnGetAllVariables(ref List<VariableSnapshot> __result)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return;

            __result = AngryMapVarManager.Instance.GetAllVariables();
        }

        //Failsafe for the setters using MapVarManager through scripting
        [HarmonyPatch(nameof(MapVarManager.SetInt))]
        [HarmonyPrefix]
        public static bool OnSetInt(string key, int value, bool persistent)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            AngryMapVarManager.Instance.SetInt(key, value, (persistent) ? VariablePersistence.SavedAsMap : VariablePersistence.Session);
            return false;
        }

        [HarmonyPatch(nameof(MapVarManager.SetFloat))]
        [HarmonyPrefix]
        public static bool OnSetFloat(string key, float value, bool persistent)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            AngryMapVarManager.Instance.SetFloat(key, value, (persistent) ? VariablePersistence.SavedAsMap : VariablePersistence.Session);
            return false;
        }

        [HarmonyPatch(nameof(MapVarManager.SetBool))]
        [HarmonyPrefix]
        public static bool OnSetBool(string key, bool value, bool persistent)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            AngryMapVarManager.Instance.SetBool(key, value, (persistent) ? VariablePersistence.SavedAsMap : VariablePersistence.Session);
            return false;
        }

        [HarmonyPatch(nameof(MapVarManager.SetString))]
        [HarmonyPrefix]
        public static bool OnSetString(string key, string value, bool persistent)
        {
            if (!AngrySceneManager.isInCustomLevel)
                return true;

            AngryMapVarManager.Instance.SetString(key, value, (persistent) ? VariablePersistence.SavedAsMap : VariablePersistence.Session);
            return false;
        }
    }
}
