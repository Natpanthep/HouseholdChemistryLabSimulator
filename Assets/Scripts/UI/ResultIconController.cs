using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ReactionType { None, ColorChange, Corrosive, Bubble, Fire, Foam, Gas, Layer, Scrub, Smell, Soap, Sticky, Toxic, HouseCleaning }

[Serializable]
public struct ReactionSprite {
    public ReactionType type;
    public Sprite sprite;
}

[Serializable]
public struct NameAlias {
    [Tooltip("e.g. \"Color Change\", \"color_change\", \"Foam\"")]
    public string name;
    public ReactionType reaction;
}

[DisallowMultipleComponent]
public class ResultIconController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage; // drag ResultIcon's Image here

    [Header("Sprite Map (ReactionType → Sprite)")]
    [SerializeField] private List<ReactionSprite> sprites = new();

    [Header("Name Aliases (optional: effect/result name → ReactionType)")]
    [SerializeField] private List<NameAlias> nameAliases = new();

    [Header("Options")]
    [SerializeField] private bool hideWhenUnknown = true;
    [SerializeField] private bool preserveAspect = true;

    private Dictionary<ReactionType, Sprite> map;
    private Dictionary<string, ReactionType> nameMap; // normalized string → enum

    // ---------- Unity lifecycle ----------
    void Awake() {
        if (!targetImage) targetImage = GetComponent<Image>();
        BuildMaps();
        if (targetImage && preserveAspect) targetImage.preserveAspect = true;
    }

    void OnValidate() {
        // Keep references & maps fresh in editor
        if (!targetImage) targetImage = GetComponent<Image>();
        BuildMaps();
        if (targetImage) targetImage.preserveAspect = preserveAspect;
    }

    // ---------- Map builders ----------
    private void BuildMaps() {
        // ReactionType → Sprite
        if (map == null) map = new Dictionary<ReactionType, Sprite>();
        else map.Clear();
        foreach (var rs in sprites) map[rs.type] = rs.sprite;

        // Name(string) → ReactionType (auto fill enum names + custom aliases)
        if (nameMap == null) nameMap = new Dictionary<string, ReactionType>(StringComparer.OrdinalIgnoreCase);
        else nameMap.Clear();

        foreach (ReactionType rt in Enum.GetValues(typeof(ReactionType))) {
            nameMap[Normalize(rt.ToString())] = rt;          // "ColorChange"
            nameMap[Normalize(rt.ToString().Replace("_"," "))] = rt; // safety
        }
        foreach (var a in nameAliases) {
            if (!string.IsNullOrWhiteSpace(a.name))
                nameMap[Normalize(a.name)] = a.reaction;
        }
        // Common friendly spellings
        nameMap[Normalize("Color Change")] = ReactionType.ColorChange;
        nameMap[Normalize("House Cleaning")] = ReactionType.HouseCleaning;
    }

    private static string Normalize(string s) {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        // lower + remove spaces, underscores, hyphens
        Span<char> buf = stackalloc char[s.Length];
        int j = 0;
        for (int i = 0; i < s.Length; i++) {
            char c = char.ToLowerInvariant(s[i]);
            if (c == ' ' || c == '_' || c == '-') continue;
            buf[j++] = c;
        }
        return new string(buf.Slice(0, j));
    }

    // ---------- Public API ----------
    public void SetIcon(ReactionType type) {
        if (targetImage == null) return;
        if (map != null && map.TryGetValue(type, out var sp) && sp != null) {
            targetImage.sprite = sp;
            targetImage.enabled = true;
        } else {
            if (hideWhenUnknown) targetImage.enabled = false;
        }
    }

    public void SetByName(string effectOrResultName) {
        if (targetImage == null) return;
        if (nameMap != null && nameMap.TryGetValue(Normalize(effectOrResultName), out var rt)) {
            SetIcon(rt);
        } else if (hideWhenUnknown) {
            targetImage.enabled = false;
        }
    }

    public void SetSprite(Sprite s) {
        if (targetImage == null) return;
        if (s != null) {
            targetImage.sprite = s;
            targetImage.enabled = true;
        } else if (hideWhenUnknown) {
            targetImage.enabled = false;
        }
    }

    /// <summary>Pick the first non-null sprite and show it (e.g., badgeIcon, productIcon, fallback).</summary>
    public void SetFromCandidates(params Sprite[] candidates) {
        if (candidates != null) {
            for (int i = 0; i < candidates.Length; i++) {
                var s = candidates[i];
                if (s != null) { SetSprite(s); return; }
            }
        }
        if (hideWhenUnknown && targetImage) targetImage.enabled = false;
    }

    public void Clear() {
        if (targetImage != null) targetImage.enabled = false;
    }
}



// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public enum ReactionType { None, ColorChange, Corrosive, Bubble, Fire, Foam, Gas, Layer, Scrub, Smell, Soap, Sticky, Toxic, HouseCleaning }

// [Serializable]
// public struct ReactionSprite {
//     public ReactionType type;
//     public Sprite sprite;
// }

// [DisallowMultipleComponent]
// public class ResultIconController : MonoBehaviour
// {
//     [Header("Target")]
//     [SerializeField] private Image targetImage; // drag ResultIcon's Image here

//     [Header("Sprite Map")]
//     [SerializeField] private List<ReactionSprite> sprites = new();

//     private Dictionary<ReactionType, Sprite> map;

//     void Awake() {
//         if (!targetImage) targetImage = GetComponent<Image>();
//         map = new Dictionary<ReactionType, Sprite>();
//         foreach (var rs in sprites) map[rs.type] = rs.sprite;
//         targetImage.preserveAspect = true; // looks nicer for icons
//     }

//     public void SetIcon(ReactionType type) {
//         if (map != null && map.TryGetValue(type, out var sp)) {
//             targetImage.sprite = sp;
//             targetImage.enabled = sp != null;
//         } else {
//             targetImage.enabled = false; // hide if unknown
//         }
//     }

//     public void Clear() => targetImage.enabled = false;
// }
