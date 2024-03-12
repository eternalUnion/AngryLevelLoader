using Logic;
using System.Collections.Generic;

namespace AngryLevelLoader.Extensions
{
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

        public static bool ContainsKey(this VarStore store, string key)
        {
            if (ContainsBoolKey(store, key))
                return true;

            if (ContainsIntKey(store, key))
                return true;

            if (ContainsFloatKey(store, key))
                return true;

            if (ContainsStringKey(store, key))
                return true;

            return false;
        }

        public static bool ContainsBoolKey(this VarStore store, string key)
        {
            foreach (var boolVal in store.boolStore)
            {
                if (boolVal.Key == key)
                    return true;
            }

            return false;
        }

        public static bool ContainsIntKey(this VarStore store, string key)
        {
            foreach (var intVal in store.intStore)
            {
                if (intVal.Key == key)
                    return true;
            }

            return false;
        }

        public static bool ContainsFloatKey(this VarStore store, string key)
        {
            foreach (var floatVal in store.floatStore)
            {
                if (floatVal.Key == key)
                    return true;
            }

            return false;
        }

        public static bool ContainsStringKey(this VarStore store, string key)
        {
            foreach (var stringVal in store.stringStore)
            {
                if (stringVal.Key == key)
                    return true;
            }

            return false;
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
