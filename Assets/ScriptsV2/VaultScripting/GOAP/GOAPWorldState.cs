using System.Collections.Generic;

[System.Serializable]
public class GOAPWorldState
{
    public Dictionary<string, bool> states = new Dictionary<string, bool>();

    public bool HasState(string key)
    {
        return states.ContainsKey(key) && states[key];
    }

    public void SetState(string key, bool value)
    {
        if (states.ContainsKey(key))
            states[key] = value;
        else
            states.Add(key, value);
    }

    public void Merge(GOAPWorldState other)
    {
        foreach (var kvp in other.states)
            SetState(kvp.Key, kvp.Value);
    }

    // ✅ Indexer for cleaner access
    public bool this[string key]
    {
        get => HasState(key);
        set => SetState(key, value);
    }
}
