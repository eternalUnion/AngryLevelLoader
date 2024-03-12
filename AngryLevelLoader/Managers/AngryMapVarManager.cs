using AngryLevelLoader.DataTypes.MapVarHandlers;
using AngryLevelLoader.Extensions;
using Logic;
using RudeLevelScripts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers
{
    //The original MapVarManager is lacking scoped persistence entirely.
    //This is more or less a re-write with the features needed. (presumably how PITR intended it to work)
    public class AngryMapVarManager : MonoBehaviour
    {
        public static AngryMapVarManager Instance { get; private set; }

        private const string MAPVAR_FILE_EXTENSION = ".vars.json";
        private const string LEVELS_DIRECTORY = "Levels";
        private const string BUNDLES_DIRECTORY = "BundleDefined";
        private const string USER_DEFINED_DIRECTORY = "UserDefined";
        private const string DEFAULT_PRESET_NAME = "default";

        //Pathes

        //Root MapVars directory
        private string angryMapVarsDirectory => Plugin.mapVarsFolderPath;

        //Directory for the current config preset
        private string GetCurrentMapVarsDirectory() => Path.Combine(angryMapVarsDirectory, GetConfigPresetID());
        
        //Bundle directory, the directory root of a .angry bundle file
        private string GetBundleDirectory() => Path.Combine(GetCurrentMapVarsDirectory(), BUNDLES_DIRECTORY, AngrySceneManager.currentBundleContainer.bundleData.bundleGuid);
        
        //Levels directory within a bundle
        private string GetLevelDirectory() => Path.Combine(GetBundleDirectory(), LEVELS_DIRECTORY);
        
        //for storing user defined persistent mapvar files
        private string GetCurrentUserMapVarsDirectory() => Path.Combine(GetCurrentMapVarsDirectory(), USER_DEFINED_DIRECTORY);

        //for storing bundle persistent mapvars
        private string GetBundleFilePath() => Path.Combine(GetBundleDirectory(), AngrySceneManager.currentBundleContainer.bundleData.bundleGuid + MAPVAR_FILE_EXTENSION);

        //for storing level persistent mapvars
        private string GetLevelFilePath() => Path.Combine(GetLevelDirectory(), AngrySceneManager.currentLevelData.uniqueIdentifier + MAPVAR_FILE_EXTENSION);

        //Current config preset. default if there is no preset.
        private string GetConfigPresetID() => string.IsNullOrEmpty(Plugin.config.currentPresetId) ? DEFAULT_PRESET_NAME : Plugin.config.currentPresetId ;

        public MapVarHandler UltrakillSessionVars { get; private set; }
        public PersistentMapVarHandler UltrakillLevelVars { get; private set; }
        public PersistentMapVarHandler UltrakillBundleVars { get; private set; }

        private List<MapVarHandler> allHandlers;

        private Dictionary<string, PersistentMapVarHandler> userDefined;
        private Dictionary<string, string> userDefinedMapVarKeyToFileMap;

        private void Awake()
        {
            Instance = this;
            allHandlers = new List<MapVarHandler>();

            //Handle config preset change
            Plugin.config.postPresetChangeEvent += (_, __) =>
            {
                if (AngrySceneManager.isInCustomLevel)
                    ReloadMapVars();
            };
            
            //Handle config preset reset
            Plugin.config.postPresetResetEvent += (_) =>
            {
                if(Directory.Exists(GetCurrentMapVarsDirectory()))
                    Directory.Delete(GetCurrentMapVarsDirectory(), true);

                if (AngrySceneManager.isInCustomLevel)
                    ReloadMapVars();
            };

            SceneManager.sceneLoaded += (_,__) =>
            {
                if (AngrySceneManager.isInCustomLevel)
                    InitializeMapVarHandlers();
            };

            InitializeMapVarHandlers();
        }

        //Called in level load.
        private void InitializeMapVarHandlers()
        {
            if(!AngrySceneManager.isInCustomLevel)
                return;

            allHandlers.Clear();

            //for Testing since I dont want to update the scripts in editor just yet.
            if(SceneHelper.CurrentScene == "hydra-level test one")
            {
                GameObject go = new GameObject("mapvarman");
                RudeMapVarHandler rude = go.AddComponent<RudeMapVarHandler>();
                rude.fileID = "MyCustomTestFile";
                rude.varList = new List<string> {  "testfloat.bundle", "testbool.bundle" };

                RudeMapVarHandler rude2 = go.AddComponent<RudeMapVarHandler>();
                rude2.fileID = "MyCustomTestFile2";
                rude2.varList = new List<string> { "teststring.bundle", "testinteger.bundle" };
            }

            if (SceneHelper.CurrentScene == "hydra-level test two")
            {
                GameObject go = new GameObject("mapvarman");
                RudeMapVarHandler rude = go.AddComponent<RudeMapVarHandler>();
                rude.fileID = "MyCustomTestFile";
                rude.varList = new List<string> { "testfloat.bundle", "testbool.bundle" };

                RudeMapVarHandler rude2 = go.AddComponent<RudeMapVarHandler>();
                rude2.fileID = "MyCustomTestFile2";
                rude2.varList = new List<string> { "teststring.bundle", "testinteger.bundle" };
            }

            //Default system
            UltrakillSessionVars = new MapVarHandler();
            UltrakillSessionVars.ReloadMapVars();
            allHandlers.Add(UltrakillSessionVars);

            UltrakillLevelVars = new PersistentMapVarHandler(GetLevelFilePath());
            UltrakillLevelVars.ReloadMapVars();
            Plugin.logger.LogInfo($"Level mapvars loaded: {UltrakillLevelVars.GetAllVariables().Count}");
            allHandlers.Add(UltrakillLevelVars);

            UltrakillBundleVars = new PersistentMapVarHandler(GetBundleFilePath());
            UltrakillBundleVars.ReloadMapVars();
            Plugin.logger.LogInfo($"Bundle mapvars loaded: {UltrakillLevelVars.GetAllVariables().Count}");
            allHandlers.Add(UltrakillBundleVars);

            //Rude system
            RudeMapVarHandler[] userDefinedHandlers = FindObjectsOfType<RudeMapVarHandler>();

            userDefinedMapVarKeyToFileMap = new Dictionary<string, string>();
            userDefined = new Dictionary<string, PersistentMapVarHandler>();

            //Loop through all the user defined handlers and register their mapvar keys with the fileID
            foreach (var handler in userDefinedHandlers)
            {
                foreach (var visibleVarKey in handler.varList)
                {
                    if (!userDefinedMapVarKeyToFileMap.ContainsKey(visibleVarKey))
                    {
                        userDefinedMapVarKeyToFileMap.Add(visibleVarKey, handler.fileID);
                    }
                    else if (userDefinedMapVarKeyToFileMap[visibleVarKey] != handler.fileID)
                    {
                        Plugin.logger.LogError($"Duplicate mapvar key found: {visibleVarKey} in {handler.fileID} and {userDefinedMapVarKeyToFileMap[visibleVarKey]}");
                    }
                }

                //Create a new handler and load the file
                if (!userDefined.ContainsKey(handler.fileID)) //Merge with existing.
                {
                    userDefined[handler.fileID] = new AngryMapVarHandler(Path.Combine(GetCurrentUserMapVarsDirectory(), handler.fileID + MAPVAR_FILE_EXTENSION), handler);
                    userDefined[handler.fileID].ReloadMapVars();
                    allHandlers.Add(userDefined[handler.fileID]);
                    Plugin.logger.LogInfo($"Loaded custom mapvar file ({handler.fileID}): {userDefined[handler.fileID].GetAllVariables().Count}");
                }
            }
        }


        public void StashStore()
        {
            foreach (var handler in allHandlers)
                handler.StashStore();
        }

        public void RestoreStashedStore()
        {
            foreach (var handler in allHandlers)
                handler.RestoreStashedStore();
        }

        public List<VariableSnapshot> GetAllVariables()
        {
            return allHandlers.SelectMany(handler => handler.GetAllVariables()).ToList();
        }

        public void ReloadMapVars()
        {
            foreach (var handler in allHandlers)
                handler.ReloadMapVars();
        }

        public void ResetStores()
        {
            foreach (var handler in allHandlers)
                handler.ResetStores();
        }

        public bool? GetBool(string key)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler userHandler = userDefined[fileID];
                return userHandler.GetBool(key);
            }

            MapVarHandler handler = allHandlers.Where(handler => handler.GetStore().ContainsBoolKey(key)).FirstOrDefault();
            
            if(handler == null)
                return null;

            return handler.GetBool(key);
        }

        public int? GetInt(string key)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler userHandler = userDefined[fileID];
                return userHandler.GetInt(key);
            }

            MapVarHandler handler = allHandlers.Where(handler => handler.GetStore().ContainsIntKey(key)).FirstOrDefault();

            if (handler == null)
                return null;

            return handler.GetInt(key);
        }

        public float? GetFloat(string key)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler userHandler = userDefined[fileID];
                return userHandler.GetFloat(key);
            }

            MapVarHandler handler = allHandlers.Where(handler => handler.GetStore().ContainsFloatKey(key)).FirstOrDefault();

            if (handler == null)
                return null;

            return handler.GetFloat(key);
        }

        public string GetString(string key)
        {
            if(userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler userHandler = userDefined[fileID];
                return userHandler.GetString(key);
            }
            
            MapVarHandler handler = allHandlers.Where(handler => handler.GetStore().ContainsStringKey(key)).FirstOrDefault();

            if (handler == null)
                return null;

            return handler.GetString(key);
        }

        public void SetBool(string key, bool value, VariablePersistence persistence = VariablePersistence.Session)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler handler = userDefined[fileID];
                handler.SetBool(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    UltrakillLevelVars.SetBool(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    UltrakillBundleVars.SetBool(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    UltrakillSessionVars.SetBool(key, value);
                    break;
            }
        }

        public void SetInt(string key, int value, VariablePersistence persistence = VariablePersistence.Session)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler handler = userDefined[fileID];
                handler.SetInt(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    UltrakillLevelVars.SetInt(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    UltrakillBundleVars.SetInt(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    UltrakillSessionVars.SetInt(key, value);
                    break;
            }
        }

        public void SetFloat(string key, float value, VariablePersistence persistence = VariablePersistence.Session)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler handler = userDefined[fileID];
                handler.SetFloat(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    UltrakillLevelVars.SetFloat(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    UltrakillBundleVars.SetFloat(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    UltrakillSessionVars.SetFloat(key, value);
                    break;
            }
        }

        public void SetString(string key, string value, VariablePersistence persistence = VariablePersistence.Session)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler handler = userDefined[fileID];
                handler.SetString(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    UltrakillLevelVars.SetString(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    UltrakillBundleVars.SetString(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    UltrakillSessionVars.SetString(key, value);
                    break;
            }
        }
    }

    
}
