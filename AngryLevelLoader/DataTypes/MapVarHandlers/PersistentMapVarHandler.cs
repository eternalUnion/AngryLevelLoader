using AngryLevelLoader.Extensions;
using Logic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AngryLevelLoader.DataTypes.MapVarHandlers
{
    public class PersistentMapVarHandler : MapVarHandler
    {
        public string FilePath;

        public PersistentMapVarHandler(string filePath)
        {
            FilePath = filePath;
        }

        public override void SetBool(string key, bool value)
        {
            bool isDirty = true;
            if (currentStore.boolStore.ContainsKey(key))
            {
                if (currentStore.boolStore[key] == value)
                    isDirty = false;
            }

            base.SetBool(key, value);

            if (isDirty)
                Save();
        }

        public override void SetInt(string key, int value)
        {
            bool isDirty = true;
            if (currentStore.intStore.ContainsKey(key))
            {
                if (currentStore.intStore[key] == value)
                    isDirty = false;
            }

            base.SetInt(key, value);

            if (isDirty)
                Save();
        }

        public override void SetFloat(string key, float value)
        {
            bool isDirty = true;
            if (currentStore.floatStore.ContainsKey(key))
            {
                if (currentStore.floatStore[key] == value)
                    isDirty = false;
            }

            base.SetFloat(key, value);

            if (isDirty)
                Save();
        }

        public override void SetString(string key, string value)
        {
            bool isDirty = true;
            if (currentStore.stringStore.ContainsKey(key))
            {
                if (currentStore.stringStore[key] == value)
                    isDirty = false;
            }

            base.SetString(key, value);

            if (isDirty)
                Save();
        }

        public override void ReloadMapVars()
        {
            base.ReloadMapVars();
            RestorePersistent();
        }

        //Loads mapvars from the file
        public void RestorePersistent()
        {
            VarStore existingStore = null;
            if (!TryLoadAtPath(GetFilePath(), out existingStore))
            {
                existingStore = new VarStore();
            }

            currentStore = existingStore;
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

        public virtual string GetFilePath()
        {
            return FilePath;
        }

        private string GetFolder()
        {
            return Path.GetDirectoryName(GetFilePath());
        }

        //Saves current persistent mapvars to their respective files.
        public void Save()
        {
            IOUtils.TryCreateDirectory(GetFolder());
            UpdateWriteVarStore(GetFilePath(), currentStore);
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
}
