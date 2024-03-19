using Logic;
using System.Collections.Generic;

namespace AngryLevelLoader.DataTypes.MapVarHandlers
{
    public class MapVarHandler
    {
        protected VarStore currentStore;
        protected VarStore stashedStore;

        public MapVarHandler()
        {
            currentStore = new VarStore();
        }

        public VarStore GetStore()
        {
            return currentStore;
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

        public virtual void ReloadMapVars()
        {
            ResetStores();
        }

        public virtual void ResetStores()
        {
            currentStore.Clear();
            stashedStore = null;
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

        public virtual void SetBool(string key, bool value)
        {
            currentStore.boolStore[key] = value;

            if (MapVarManager.Instance.boolSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.boolSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);
        }

        public virtual void SetInt(string key, int value)
        {
            currentStore.intStore[key] = value;

            if (MapVarManager.Instance.intSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.intSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);
        }

        public virtual void SetFloat(string key, float value)
        {
            currentStore.floatStore[key] = value;

            if (MapVarManager.Instance.floatSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.floatSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);
        }

        public virtual void SetString(string key, string value)
        {
            currentStore.stringStore[key] = value;

            if (MapVarManager.Instance.stringSubscribers.ContainsKey(key))
                foreach (var subscriber in MapVarManager.Instance.stringSubscribers[key])
                    subscriber?.Invoke(value);

            if (MapVarManager.Instance.globalSubscribers.Count > 0)
                foreach (var subscriber in MapVarManager.Instance.globalSubscribers)
                    subscriber?.Invoke(key, value);
        }

    }
}
