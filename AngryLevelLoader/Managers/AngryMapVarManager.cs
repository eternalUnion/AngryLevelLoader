using Logic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AngryLevelLoader.Managers
{
    //The original MapVarManager is lacking scoped persistence entirely.
    //This is more or less a re-write with the features needed. (presumably how PITR intended it to work)
    public class AngryMapVarManager : MonoBehaviour
    {
        public static AngryMapVarManager Instance { get; private set; }

        private VarStore currentStore;
        private VarStore stashedStore;

        private HashSet<string> levelPersistentKeys;
        private HashSet<string> bundlePersistentKeys;

        private const string FILE_EXTENSION = ".vars.json";
        private string angryMapVarsDirectory => Path.Combine(MapVarSaver.MapVarDirectory, Plugin.PLUGIN_NAME);
        private string GetBundleDirectory() => Path.Combine(angryMapVarsDirectory, AngrySceneManager.currentBundleContainer.bundleData.bundleGuid);

        //for storing bundle persistent mapvars
        private string GetBundleFilePath() => Path.Combine(GetBundleDirectory(), AngrySceneManager.currentBundleContainer.bundleData.bundleGuid + FILE_EXTENSION);

        //for storing level persistent mapvars
        private string GetLevelFilePath() => Path.Combine(GetBundleDirectory(), AngrySceneManager.currentLevelData.uniqueIdentifier + FILE_EXTENSION);

        private void Awake()
        {
            levelPersistentKeys = new HashSet<string>();
            bundlePersistentKeys = new HashSet<string>();
            currentStore = new VarStore();

            Instance = this;
        }

        public void StashStore()
        {
            if (currentStore.intStore.Count == 0 && currentStore.boolStore.Count == 0 && currentStore.floatStore.Count == 0 && currentStore.stringStore.Count == 0)
            {
                stashedStore = null;
                return;
            }

            stashedStore = currentStore.DuplicateStore();
        }

        public void RestoreStashedStore()
        {
            if (stashedStore == null)
                return;

            currentStore = stashedStore.DuplicateStore();
        }

        public List<VariableSnapshot> GetAllVariables()
        {
            List<VariableSnapshot> snapshots = new List<VariableSnapshot>();

            foreach (var boolVar in currentStore.boolStore)
            {
                snapshots.Add(new VariableSnapshot()
                {
                    type = typeof(bool),
                    value = boolVar.Value,
                    name = boolVar.Key
                });
            }

            foreach (var intVar in currentStore.intStore)
            {
                snapshots.Add(new VariableSnapshot()
                {
                    type = typeof(int),
                    value = intVar.Value,
                    name = intVar.Key
                });
            }

            foreach (var floatVar in currentStore.floatStore)
            {
                snapshots.Add(new VariableSnapshot()
                {
                    type = typeof(float),
                    value = floatVar.Value,
                    name = floatVar.Key
                });
            }

            foreach (var stringVar in currentStore.stringStore)
            {
                snapshots.Add(new VariableSnapshot()
                {
                    type = typeof(string),
                    value = stringVar.Value,
                    name = stringVar.Key
                });
            }

            return snapshots;
        }

        public void ReloadMapVars()
        {
            ResetStores();
            RestorePersistent();
        }

        private void ResetStores()
        {
            currentStore.Clear();
            stashedStore = null;

            bundlePersistentKeys.Clear();
            levelPersistentKeys.Clear();
        }



        public bool? GetBool(string key)
        {
            if (currentStore.boolStore.TryGetValue(key, out bool value))
                return new bool?(value);

            return null;
        }

        public int? GetInt(string key)
        {
            if (currentStore.intStore.TryGetValue(key, out int value))
                return new int?(value);

            return null;
        }

        public float? GetFloat(string key)
        {
            if (currentStore.floatStore.TryGetValue(key, out float value))
                return new float?(value);

            return null;
        }

        public string GetString(string key)
        {
            if (currentStore.stringStore.TryGetValue(key, out string value))
                return value;

            return null;
        }

        public void SetBool(string key, bool value, VariablePersistence persistence = VariablePersistence.Session)
        {
            bool isDirty = true;

            if (currentStore.boolStore.ContainsKey(key))
            {
                if (currentStore.boolStore[key] == value)
                    isDirty = false;
            }

            currentStore.boolStore[key] = value;

            if (MapVarManager.Instance.boolSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.boolSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);

            //Mark it's persistence
            SetMapVarPersistence(key, persistence);

            //Save the store
            if (persistence != VariablePersistence.Session && isDirty)
                Save();
        }

        public void SetInt(string key, int value, VariablePersistence persistence = VariablePersistence.Session)
        {
            bool isDirty = true;

            if (currentStore.intStore.ContainsKey(key))
            {
                if (currentStore.intStore[key] == value)
                    isDirty = false;
            }

            currentStore.intStore[key] = value;
            
            if (MapVarManager.Instance.intSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.intSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);

            //Mark it's persistence
            SetMapVarPersistence(key, persistence);

            //Save the store
            if (persistence != VariablePersistence.Session && isDirty)
                Save();
        }

        public void SetFloat(string key, float value, VariablePersistence persistence = VariablePersistence.Session)
        {
            bool isDirty = true;

            if (currentStore.floatStore.ContainsKey(key))
            {
                if (currentStore.floatStore[key] == value)
                    isDirty = false;
            }

            currentStore.floatStore[key] = value;

            if (MapVarManager.Instance.floatSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.floatSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);

            //Mark it's persistence
            SetMapVarPersistence(key, persistence);

            //Save the store
            if (persistence != VariablePersistence.Session && isDirty)
                Save();
        }

        public void SetString(string key, string value, VariablePersistence persistence = VariablePersistence.Session)
        {
            bool isDirty = true;

            if (currentStore.stringStore.ContainsKey(key))
            {
                if (currentStore.stringStore[key] == value)
                    isDirty = false;
            }

            currentStore.stringStore[key] = value;

            if (MapVarManager.Instance.stringSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.stringSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);

            //Mark it's persistence
            SetMapVarPersistence(key, persistence);

            //Save the store
            if (persistence != VariablePersistence.Session && isDirty)
                Save();
        }

        //Marks the key with the provided persistence type.
        private void SetMapVarPersistence(string key, VariablePersistence persistence)
        {
            currentStore.persistentKeys.Remove(key); //We're not using this.
            switch (persistence)
            {
                case VariablePersistence.SavedAsMap:
                    levelPersistentKeys.Add(key);
                    bundlePersistentKeys.Remove(key);
                    break;
                case VariablePersistence.SavedAsCampaign:
                    levelPersistentKeys.Remove(key);
                    bundlePersistentKeys.Add(key);
                    break;
                case VariablePersistence.Session:
                default:
                    levelPersistentKeys.Remove(key);
                    bundlePersistentKeys.Remove(key);
                    break;
            }
        }

        //Serialization

        private void RestorePersistent()
        {
            currentStore = new VarStore();

            //Load existing persistent keys from files
            VarStore campaignStore = null;
            if (!TryLoadAtPath(GetBundleFilePath(), out campaignStore))
                campaignStore = new VarStore();

            VarStore levelStore = null;
            if (!TryLoadAtPath(GetLevelFilePath(), out levelStore))
                levelStore = new VarStore();

            //Append the stores and extract the keys for the current store.

            currentStore.AppendDistinct(levelStore);
            levelPersistentKeys = levelStore.ExtractAllKeys();

            currentStore.AppendDistinct(campaignStore);
            bundlePersistentKeys = campaignStore.ExtractAllKeys();
        }

        //Attempts to load a VarStore object at a filepath
        private bool TryLoadAtPath(string filePath, out VarStore store)
        {
            store = null;

            if (!File.Exists(filePath))
                return false;

            try
            {
                string json = File.ReadAllText(filePath);
                PersistentSavedStore savedStore = JsonConvert.DeserializeObject<PersistentSavedStore>(json);
                if (savedStore == null)
                    return false;

                store = new VarStore();

                foreach (SavedVariable variable in savedStore.variables)
                {
                    if (variable == null)
                        continue;

                    LoadVariable(variable, store);
                }

                return true;
            }
            catch (Exception e)
            {
                Plugin.logger.LogError("Failed to load mapvars at " + filePath + " with exception: " + e);
            }

            return false;
        }

        //VarStore.LoadVariable does not deserialize properly, so this is a fix.
        private static void LoadVariable(SavedVariable variable, VarStore store)
        {
            switch (variable.value.type)
            {
                case "System.Boolean":
                    store.boolStore[variable.name] = bool.Parse(variable.value.value.ToString());
                    break;
                case "System.Int32":
                    store.intStore[variable.name] = int.Parse(variable.value.value.ToString());
                    break;
                case "System.Single":
                    store.floatStore[variable.name] = float.Parse(variable.value.value.ToString());
                    break;
                case "System.String":
                    store.stringStore[variable.name] = variable.value.value.ToString();
                    break;
            }
        }

        //Serializes a varstore to a file using the same method as the original MapVarManager
        private void WriteStore(string filePath, VarStore store)
        {
            List<SavedVariable> savedMapVar = new List<SavedVariable>();
            foreach (var boolVar in store.boolStore)
                savedMapVar.Add(new SavedVariable()
                {
                    name = boolVar.Key,
                    value = new SavedValue
                    {
                        type = typeof(bool).FullName,
                        value = boolVar.Value
                    }
                });

            foreach (var intVar in store.intStore)
                savedMapVar.Add(new SavedVariable()
                {
                    name = intVar.Key,
                    value = new SavedValue
                    {
                        type = typeof(int).FullName,
                        value = intVar.Value
                    }
                });

            foreach (var floatVar in store.floatStore)
                savedMapVar.Add(new SavedVariable()
                {
                    name = floatVar.Key,
                    value = new SavedValue
                    {
                        type = typeof(float).FullName,
                        value = floatVar.Value
                    }
                });

            foreach (var stringVar in store.stringStore)
                savedMapVar.Add(new SavedVariable()
                {
                    name = stringVar.Key,
                    value = new SavedValue
                    {
                        type = typeof(string).FullName,
                        value = stringVar.Value
                    }
                });


            if (savedMapVar.Count == 0)
                return;

            try
            {
                string json = JsonConvert.SerializeObject(new PersistentSavedStore()
                {
                    variables = savedMapVar
                });

                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                Plugin.logger.LogError("Failed to save mapvars at " + filePath + " with exception: " + e);
            }
        }

        //Saves current persistent mapvars to their respective files.
        private void Save()
        {
            //Save the stores.
            if (levelPersistentKeys.Count > 0)
            {
                VarStore levelPersistentVars = currentStore.ExtractSet(levelPersistentKeys);
                UpdateWriteVarStore(GetLevelFilePath(), levelPersistentVars);
            }

            if (bundlePersistentKeys.Count > 0)
            {
                VarStore bundlePersistentVars = currentStore.ExtractSet(bundlePersistentKeys);
                UpdateWriteVarStore(GetBundleFilePath(), bundlePersistentVars);
            }
        }

        //Appends and updates the store values to the file. If the file doesn't exist, it will create a new one.
        private void UpdateWriteVarStore(string filePath, VarStore store)
        {
            if (!TryLoadAtPath(filePath, out VarStore existing))
                existing = new VarStore();

            existing.Update(store);
            WriteStore(filePath, existing);
        }
    }

    public static class VarStoreExtensions
    {
        //Adds distinct values from the source store to the target store.
        public static void AppendDistinct(this VarStore target, VarStore source)
        {
            foreach (var boolVal in source.boolStore)
            {
                if (!target.boolStore.ContainsKey(boolVal.Key))
                    target.boolStore.Add(boolVal.Key, boolVal.Value);
            }

            foreach (var intVal in source.intStore)
            {
                if (!target.intStore.ContainsKey(intVal.Key))
                    target.intStore.Add(intVal.Key, intVal.Value);
            }

            foreach (var floatVal in source.floatStore)
            {
                if (!target.floatStore.ContainsKey(floatVal.Key))
                    target.floatStore.Add(floatVal.Key, floatVal.Value);
            }

            foreach (var stringVal in source.stringStore)
            {
                if (!target.stringStore.ContainsKey(stringVal.Key))
                    target.stringStore.Add(stringVal.Key, stringVal.Value);
            }
        }

        //Creates a new VarStore with only the keys within the provided set.
        public static VarStore ExtractSet(this VarStore store, HashSet<string> keys)
        {
            VarStore newStore = new VarStore();

            foreach (var key in keys)
            {
                if (store.boolStore.ContainsKey(key))
                    newStore.boolStore.Add(key, store.boolStore[key]);

                if (store.intStore.ContainsKey(key))
                    newStore.intStore.Add(key, store.intStore[key]);

                if (store.floatStore.ContainsKey(key))
                    newStore.floatStore.Add(key, store.floatStore[key]);

                if (store.stringStore.ContainsKey(key))
                    newStore.stringStore.Add(key, store.stringStore[key]);
            }

            return newStore;
        }

        //Returns all keys from the VarStore.
        public static HashSet<string> ExtractAllKeys(this VarStore varstore)
        {
            HashSet<string> keys = new HashSet<string>();
            keys.UnionWith(varstore.boolStore.Keys);
            keys.UnionWith(varstore.intStore.Keys);
            keys.UnionWith(varstore.floatStore.Keys);
            keys.UnionWith(varstore.stringStore.Keys);
            return keys;
        }

        //Updates the store with the values from the source store, replacing existing and adding new ones without removing any.
        public static void Update(this VarStore target, VarStore source)
        {
            foreach (var boolVal in source.boolStore)
            {
                if (!target.boolStore.ContainsKey(boolVal.Key))
                    target.boolStore.Add(boolVal.Key, boolVal.Value);
                else
                    target.boolStore[boolVal.Key] = boolVal.Value;
            }

            foreach (var intVal in source.intStore)
            {
                if (!target.intStore.ContainsKey(intVal.Key))
                    target.intStore.Add(intVal.Key, intVal.Value);
                else
                    target.intStore[intVal.Key] = intVal.Value;
            }

            foreach (var floatVal in source.floatStore)
            {
                if (!target.floatStore.ContainsKey(floatVal.Key))
                    target.floatStore.Add(floatVal.Key, floatVal.Value);
                else
                    target.floatStore[floatVal.Key] = floatVal.Value;
            }

            foreach (var stringVal in source.stringStore)
            {
                if (!target.stringStore.ContainsKey(stringVal.Key))
                    target.stringStore.Add(stringVal.Key, stringVal.Value);
                else
                    target.stringStore[stringVal.Key] = stringVal.Value;
            }
        }
    }
}
