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
    //This will act as a replacement for the MapVarManager, it will sit on the same GameObject as the MapVarManager and will not be destroyed on scene change.
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

        private List<MapVarHandler> allHandlers;

        private MapVarHandler ultrakillSessionVars;
        private PersistentMapVarHandler ultrakillLevelVars;
        private PersistentMapVarHandler ultrakillBundleVars;

        private Dictionary<string, AngryMapVarHandler> userDefinedVars;
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

            //Handle scene change
            SceneManager.sceneLoaded += (_,__) => InitializeMapVarHandlers();

            InitializeMapVarHandlers();
        }

        //Called in level load.
        private void InitializeMapVarHandlers()
        {
            if(!AngrySceneManager.isInCustomLevel)
                return;

            allHandlers.Clear();

            //Default system
            ultrakillSessionVars = new MapVarHandler();
            ultrakillSessionVars.ReloadMapVars();
            allHandlers.Add(ultrakillSessionVars);

            //Initialize handler for level vars with the level's file path
            ultrakillLevelVars = new PersistentMapVarHandler(GetLevelFilePath());
            ultrakillLevelVars.ReloadMapVars();
            Plugin.logger.LogInfo($"Level mapvars loaded: {ultrakillLevelVars.GetAllVariables().Count}");
            allHandlers.Add(ultrakillLevelVars);

            //Initialize handler for bundle vars with the bundle's file path
            ultrakillBundleVars = new PersistentMapVarHandler(GetBundleFilePath());
            ultrakillBundleVars.ReloadMapVars();
            Plugin.logger.LogInfo($"Bundle mapvars loaded: {ultrakillLevelVars.GetAllVariables().Count}");
            allHandlers.Add(ultrakillBundleVars);

            //Rude system
            RudeMapVarHandler[] userDefinedHandlers = FindObjectsOfType<RudeMapVarHandler>();

            userDefinedMapVarKeyToFileMap = new Dictionary<string, string>();
            userDefinedVars = new Dictionary<string, AngryMapVarHandler>();

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
                if (!userDefinedVars.ContainsKey(handler.fileID)) //Merge with existing.
                {
                    userDefinedVars[handler.fileID] = new AngryMapVarHandler(Path.Combine(GetCurrentUserMapVarsDirectory(), handler.fileID + MAPVAR_FILE_EXTENSION), handler);
                    userDefinedVars[handler.fileID].ReloadMapVars();
                    allHandlers.Add(userDefinedVars[handler.fileID]);
                    Plugin.logger.LogInfo($"Loaded custom mapvar file ({handler.fileID}): {userDefinedVars[handler.fileID].GetAllVariables().Count}");
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
                MapVarHandler userHandler = userDefinedVars[fileID];
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
                MapVarHandler userHandler = userDefinedVars[fileID];
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
                MapVarHandler userHandler = userDefinedVars[fileID];
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
                MapVarHandler userHandler = userDefinedVars[fileID];
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
                MapVarHandler handler = userDefinedVars[fileID];
                handler.SetBool(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    ultrakillLevelVars.SetBool(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    ultrakillBundleVars.SetBool(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    ultrakillSessionVars.SetBool(key, value);
                    break;
            }
        }

        public void SetInt(string key, int value, VariablePersistence persistence = VariablePersistence.Session)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler handler = userDefinedVars[fileID];
                handler.SetInt(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    ultrakillLevelVars.SetInt(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    ultrakillBundleVars.SetInt(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    ultrakillSessionVars.SetInt(key, value);
                    break;
            }
        }

        public void SetFloat(string key, float value, VariablePersistence persistence = VariablePersistence.Session)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler handler = userDefinedVars[fileID];
                handler.SetFloat(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    ultrakillLevelVars.SetFloat(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    ultrakillBundleVars.SetFloat(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    ultrakillSessionVars.SetFloat(key, value);
                    break;
            }
        }

        public void SetString(string key, string value, VariablePersistence persistence = VariablePersistence.Session)
        {
            if (userDefinedMapVarKeyToFileMap.ContainsKey(key))
            {
                string fileID = userDefinedMapVarKeyToFileMap[key];
                MapVarHandler handler = userDefinedVars[fileID];
                handler.SetString(key, value);
                return;
            }

            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    ultrakillLevelVars.SetString(key, value);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    ultrakillBundleVars.SetString(key, value);
                    break;
                case VariablePersistence.Session:
                default:
                    ultrakillSessionVars.SetString(key, value);
                    break;
            }
        }
    }

    
}
