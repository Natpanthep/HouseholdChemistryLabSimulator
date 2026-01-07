using System.Collections.Generic;
using UnityEngine;

namespace ScienceLab
{
    /// <summary>
    /// Match what Beaker.PlayEffect() supports. Index 0 acts like "no special FX",
    /// so we name it ColorChange (just tint the liquid).
    /// </summary>
    public enum EffectType
    {
        ColorChange = 0,
        Bubbles,
        Smoke,
        Sparks,
        Heat
    }

    /// <summary>
    /// Small labeled icon (e.g., Toxic, Body scrub, Irritant).
    /// </summary>
    [System.Serializable]
    public class ReactionBadge
    {
        [Tooltip("Short label for the badge (tooltip/text).")]
        public string name;

        [Tooltip("Icon sprite shown in UI.")]
        public Sprite icon;

        [Tooltip("Optional tint the UI can use.")]
        public Color color = Color.white;

        // Back-compat read-only alias if some UI expects .label
        public string label => name;
    }

    /// <summary>
    /// ScriptableObject describing ONE two-ingredient reaction.
    /// </summary>
    [CreateAssetMenu(menuName = "ScienceLab/Reaction Definition", fileName = "Reaction_")]
    public class ReactionDefinition : ScriptableObject
    {
        // ----------------- Identity -----------------
        [Header("Identity")]
        [Tooltip("Stable key used by code (auto-filled as 'a+b').")]
        public string id;

        [Tooltip("Fallback display name (e.g., 'Blue Solution').")]
        public string displayName;

        // ----------------- Inputs -----------------
        [Header("Ingredients (exactly 2)")]
        public IngredientSO ingredientA;
        public IngredientSO ingredientB;

        // Back-compat: old code used 'inputs' (List). Provide a read-only alias.
        public List<IngredientSO> inputs
        {
            get
            {
                // Always return a 2-slot list in order A,B if present.
                _inputsBuffer.Clear();
                if (ingredientA) _inputsBuffer.Add(ingredientA);
                if (ingredientB) _inputsBuffer.Add(ingredientB);
                return _inputsBuffer;
            }
        }
        private readonly List<IngredientSO> _inputsBuffer = new(2);

        // ----------------- Visual Result -----------------
        [Header("Result Visuals")]
        [Tooltip("Liquid tint to apply in the beaker after mixing.")]
        public Color resultColor = Color.white;

        [Tooltip("Which particle/effect to trigger on success.")]
        public EffectType effect = EffectType.ColorChange;

        [Tooltip("Preferred display name in UI (over Display Name).")]
        public string productName;

        [Tooltip("Optional product icon shown in UI.")]
        public Sprite productIcon;

        [Tooltip("Extra small icons (hazards/usages) to show alongside the result.")]
        public List<ReactionBadge> badges = new();

        // Back-compat: old UI reads .resultName / .resultIcon
        public string resultName => NiceName;
        public Sprite resultIcon => productIcon;

        // ----------------- Audio -----------------
        [Header("Audio (optional)")]
        public AudioClip sfx;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        // ----------------- Text / Notes -----------------
        [Header("Fun Fact / Notes")]
        [TextArea(2, 6)]
        public string funFact;

        // ----------------- Progression -----------------
        [Header("Discovery Flags")]
        [Tooltip("If false, treat as locked/hidden until unlocked via gameplay.")]
        public bool unlocked = true;

        [Tooltip("Set true when the player has discovered this reaction at runtime.")]
        public bool discovered = false;

        // --------------- Convenience ---------------
        /// <summary>
        /// Nice name to show: productName if set, else displayName, else asset name.
        /// </summary>
        public string NiceName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(productName)) return productName;
                if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
                return name;
            }
        }

        /// <summary>
        /// Main icon to use in UI (falls back to null if none).
        /// </summary>
        public Sprite icon => productIcon;

        /// <summary>
        /// Order-agnostic ingredient match (A+B == B+A).
        /// </summary>
        public bool Matches(IngredientSO x, IngredientSO y)
        {
            if (x == null || y == null) return false;
            return (ingredientA == x && ingredientB == y) || (ingredientA == y && ingredientB == x);
        }

        /// <summary>
        /// Canonical key maker (order independent).
        /// </summary>
        public static string MakeKey(IngredientSO a, IngredientSO b)
        {
            if (a == null || b == null) return "";
            string ka = SafeId(a);
            string kb = SafeId(b);
            return string.CompareOrdinal(ka, kb) <= 0 ? $"{ka}+{kb}" : $"{kb}+{ka}";
        }

        /// <summary>
        /// Returns a stable identifier for an ingredient (use g.id if you have one).
        /// </summary>
        private static string SafeId(IngredientSO g)
        {
            return string.IsNullOrEmpty(g.name) ? "unknown" : g.name.Trim().ToLowerInvariant();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep 'id' in sync while editing
            if (ingredientA != null && ingredientB != null)
            {
                string newId = MakeKey(ingredientA, ingredientB);
                if (string.IsNullOrEmpty(id) || id != newId)
                {
                    id = newId;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
