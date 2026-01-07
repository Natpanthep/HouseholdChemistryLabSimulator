using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using ScienceLab;


[CreateAssetMenu(menuName = "Lab/Reaction Database", fileName = "ReactionDatabase")]
public class ReactionDatabase : ScriptableObject
{
    public List<ReactionDefinition> reactions = new();

    Dictionary<string, ReactionDefinition> _lookup;

    static string KeyFor(IEnumerable<IngredientSO> inputs)
        => string.Join("+", inputs.Where(i => i != null)
                                  .Select(i => i.id)
                                  .OrderBy(s => s, StringComparer.Ordinal));

    public void Build()
    {
        _lookup = new Dictionary<string, ReactionDefinition>(StringComparer.Ordinal);
        foreach (var def in reactions)
        {
            if (def == null) continue;
            var key = KeyFor(def.inputs);
            if (_lookup.ContainsKey(key))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"ReactionDatabase: duplicate reaction ignored: {key}  (asset: {def.name})");
#endif
                continue; // keep the first one
            }
            _lookup[key] = def;
        }
    }

    public bool TryGetReaction(List<IngredientSO> inputs, out ReactionDefinition def)
    {
        if (_lookup == null) Build();
        return _lookup.TryGetValue(KeyFor(inputs), out def);
    }

#if UNITY_EDITOR
    void OnValidate() => Build();
#endif

    public IEnumerable<ReactionDefinition> UniqueReactions()
    {
        if (_lookup == null) Build();
        return _lookup.Values;
    }
}
