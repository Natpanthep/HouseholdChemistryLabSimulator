using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScienceLab;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RecipeRowUI : MonoBehaviour
{
    [Header("Primary UI (easy mode)")]
    [SerializeField] private TMP_Text combinedText; // ONE TMP_Text that shows ProductName, Display/Result, FunFact

    [Header("Optional (legacy fields)")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text fact;
    [SerializeField] private TMP_Text ingredients;
    [SerializeField] private Button deleteButton;

    [Header("Badges (optional)")]
    [SerializeField] private Transform badgesBar;
    [SerializeField] private GameObject badgeIconPrefab;

    // Runtime
    private string key;
    private bool _pendingFlash;

    private void Awake()
    {
        AutoWireIfMissing();
        EnsureVisibleDefaults();
        AdjustTextLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        // auto refresh text wrapping when resized
        AdjustTextLayout();
    }

    private void AutoWireIfMissing()
    {
        if (!icon)
            icon = transform.Find("Icon")?.GetComponent<Image>();

        if (!combinedText)
            combinedText = transform.Find("CombinedText")?.GetComponent<TMP_Text>();

        if (!title)
            title = transform.Find("Texts/Title")?.GetComponent<TMP_Text>();

        if (!ingredients)
            ingredients = transform.Find("Texts/Ingredients")?.GetComponent<TMP_Text>();

        if (!fact)
            fact = transform.Find("Texts/Fact")?.GetComponent<TMP_Text>();

        if (!deleteButton)
            deleteButton = transform.Find("DeleteBtn")?.GetComponent<Button>();

        if (!badgesBar)
            badgesBar = transform.Find("Badges");

#if UNITY_EDITOR
        if (!icon) Debug.LogWarning($"{name}: missing icon Image (will still work)");
        if (!combinedText) Debug.LogWarning($"{name}: missing combinedText TMP_Text (recommended)");
#endif
    }

    private void EnsureVisibleDefaults()
    {
        void EnableText(TMP_Text t)
        {
            if (!t) return;
            t.enabled = true;
            var c = t.color; c.a = 1f; t.color = c;
        }

        EnableText(combinedText);
        EnableText(title);
        EnableText(ingredients);
        EnableText(fact);

        if (icon) icon.enabled = true;
    }

    public void Bind(ReactionDefinition def, bool highlight = false)
    {
        if (!def) return;

        // Gather text values
        string productName = GetString(def, "productName");
        string displayName = GetString(def, "displayName") ?? GetString(def, "resultName");
        string funFactText = GetString(def, "funFact");

        // Combine text lines
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(productName)) lines.Add(productName);
        if (!string.IsNullOrWhiteSpace(displayName) &&
            (string.IsNullOrWhiteSpace(productName) || !string.Equals(productName, displayName)))
            lines.Add(displayName);
        if (!string.IsNullOrWhiteSpace(funFactText)) lines.Add(funFactText);
        if (lines.Count == 0) lines.Add(def.name);

        if (combinedText)
        {
            if (lines.Count > 0) lines[0] = $"<b>{lines[0]}</b>";
            combinedText.text = string.Join("\n", lines);
            AdjustTextLayout();
        }

        // Optional legacy
        if (title) title.text = productName ?? displayName ?? def.name;
        if (fact) fact.text = funFactText ?? string.Empty;
        if (ingredients && def.inputs != null && def.inputs.Count > 0)
        {
            var names = def.inputs.Where(i => i != null)
                                  .Select(i => string.IsNullOrWhiteSpace(i.displayName) ? i.name : i.displayName);
            ingredients.text = string.Join(" + ", names);
        }

        // Icon
        if (icon)
        {
            var spr = GetSpriteAny(def, "productIcon", "resultIcon", "icon");
            if (spr)
            {
                icon.sprite = spr;
                icon.preserveAspect = true;
                icon.enabled = true;
            }
            else icon.enabled = false;
        }

        // Unique key
        key = BuildKey(def);

        // Delete button
        if (deleteButton)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => RecipeBookManager.Instance?.Remove(key));
        }

        // Badges
        RefreshBadgesFromDefinition(def);

        if (highlight)
        {
            if (isActiveAndEnabled) StartCoroutine(Flash());
            else _pendingFlash = true;
        }
    }

    private void OnEnable()
    {
        if (_pendingFlash)
        {
            _pendingFlash = false;
            StartCoroutine(Flash());
        }
    }

    private System.Collections.IEnumerator Flash()
    {
        var t = transform;
        var s0 = t.localScale;
        t.localScale = s0 * 1.06f;
        yield return new WaitForSeconds(0.12f);
        t.localScale = s0;
    }

    // -------------- Helpers -----------------

    private void AdjustTextLayout()
    {
        if (!combinedText) return;
        combinedText.enableWordWrapping = true;
        combinedText.overflowMode = TextOverflowModes.Overflow;
        combinedText.rectTransform.pivot = new Vector2(0, 1);
        combinedText.alignment = TextAlignmentOptions.TopLeft;

        // auto-fit height if parent uses LayoutGroup
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)combinedText.transform);
    }

    private static string BuildKey(ReactionDefinition def)
    {
        if (def.inputs != null && def.inputs.Count > 0)
        {
            var parts = def.inputs
                .Where(i => i != null)
                .Select(i => !string.IsNullOrWhiteSpace(i.id)
                                ? i.id
                                : (string.IsNullOrWhiteSpace(i.name) ? "unknown" : i.name.Trim().ToLowerInvariant()))
                .OrderBy(s => s, System.StringComparer.Ordinal);
            return string.Join("+", parts);
        }
        var rn = GetString(def, "resultName");
        if (!string.IsNullOrWhiteSpace(rn)) return rn.Trim().ToLowerInvariant();
        return def.name.Trim().ToLowerInvariant();
    }

    private static string GetString(object obj, string fieldName)
    {
        if (obj == null) return null;
        var f = obj.GetType().GetField(fieldName);
        if (f == null) return null;
        var v = f.GetValue(obj) as string;
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static Sprite GetSpriteAny(object obj, params string[] fieldNames)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        foreach (var fn in fieldNames)
        {
            var f = t.GetField(fn);
            if (f == null) continue;
            var s = f.GetValue(obj) as Sprite;
            if (s) return s;
        }
        return null;
    }

    private void RefreshBadgesFromDefinition(ReactionDefinition def)
    {
        if (!badgesBar) return;
        for (int i = badgesBar.childCount - 1; i >= 0; i--)
            Destroy(badgesBar.GetChild(i).gameObject);

        if (!badgeIconPrefab) return;

        var type = def.GetType();
        var badgesField = type.GetField("badges");
        if (badgesField != null)
        {
            var listObj = badgesField.GetValue(def) as System.Collections.IEnumerable;
            if (listObj != null)
            {
                foreach (var item in listObj)
                {
                    if (item == null) continue;
                    var iconField = item.GetType().GetField("icon");
                    if (iconField == null) continue;
                    var sprite = iconField.GetValue(item) as Sprite;
                    if (!sprite) continue;
                    CreateBadge(sprite);
                }
                return;
            }
        }

        var extraIconsField = type.GetField("extraIcons");
        if (extraIconsField != null)
        {
            var list = extraIconsField.GetValue(def) as IEnumerable<Sprite>;
            if (list != null)
                foreach (var s in list)
                    if (s) CreateBadge(s);
        }
    }

    private void CreateBadge(Sprite s)
    {
        var go = Instantiate(badgeIconPrefab, badgesBar);
        var img = go.GetComponent<Image>() ?? go.GetComponentInChildren<Image>();
        if (img)
        {
            img.sprite = s;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("DIAG: Force Dummy Texts")]
    private void __Diag_ForceDummyTexts()
    {
        if (combinedText)
        {
            combinedText.enabled = true;
            combinedText.text = "<b>Product</b>\nDisplay Name\nFun fact here...";
            var c = combinedText.color; c.a = 1f; combinedText.color = c;
        }
        if (title) { title.text = "Dummy Title"; }
        if (ingredients) { ingredients.text = "A + B"; }
        if (fact) { fact.text = "Fun fact here..."; }
        if (icon) icon.enabled = true;
        Debug.Log("[RecipeRowUI] Dummy text applied");
    }
#endif
}

// using System.Collections.Generic;
// using System.Linq;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using ScienceLab;

// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// [DisallowMultipleComponent]
// public class RecipeRowUI : MonoBehaviour
// {
//     // ---------- Primary (easy mode) ----------
//     [Header("Primary UI (easy mode)")]
//     [SerializeField] private TMP_Text combinedText; // ONE TMP_Text that shows ProductName, Display/Result, FunFact

//     // ---------- Optional legacy fields (you can leave them empty) ----------
//     [Header("Optional (legacy fields)")]
//     [SerializeField] private Image    icon;          // RecipeRow/Icon
//     [SerializeField] private TMP_Text title;         // RecipeRow/Texts/Title
//     [SerializeField] private TMP_Text fact;          // RecipeRow/Texts/Fact
//     [SerializeField] private TMP_Text ingredients;   // RecipeRow/Texts/Ingredients (optional line if you want it)
//     [SerializeField] private Button   deleteButton;  // RecipeRow/DeleteBtn

//     // ---------- Badges ----------
//     [Header("Badges (optional)")]
//     [SerializeField] private Transform   badgesBar;       // RecipeRow/Badges
//     [SerializeField] private GameObject  badgeIconPrefab; // Prefabs/UI/BadgeIcon (24x24 Image)

//     // runtime
//     private string key;
//     private bool _pendingFlash;

//     private void Awake()
//     {
//         AutoWireIfMissing();
//         EnsureVisibleDefaults();
//     }

//     private void AutoWireIfMissing()
//     {
//         // Try to find common child names if fields not assigned
//         if (!icon)
//             icon = transform.Find("Icon")?.GetComponent<Image>();

//         if (!combinedText)
//             combinedText = transform.Find("CombinedText")?.GetComponent<TMP_Text>();

//         if (!title)
//             title = transform.Find("Texts/Title")?.GetComponent<TMP_Text>();

//         if (!ingredients)
//             ingredients = transform.Find("Texts/Ingredients")?.GetComponent<TMP_Text>();

//         if (!fact)
//             fact = transform.Find("Texts/Fact")?.GetComponent<TMP_Text>();

//         if (!deleteButton)
//             deleteButton = transform.Find("DeleteBtn")?.GetComponent<Button>();

//         if (!badgesBar)
//             badgesBar = transform.Find("Badges");

// #if UNITY_EDITOR
//         if (!icon)         Debug.LogWarning($"{name}: RecipeRowUI missing 'icon' (Image). Will work without.");
//         if (!combinedText) Debug.LogWarning($"{name}: RecipeRowUI missing 'combinedText' (TMP_Text). Highly recommended!");
//         // title/ingredients/fact can be optional
//         if (!deleteButton) Debug.LogWarning($"{name}: RecipeRowUI missing 'deleteButton' (Button).");
//         // badgesBar/badgeIconPrefab are optional too
// #endif
//     }

//     private void EnsureVisibleDefaults()
//     {
//         // Make sure texts are enabled & visible
//         if (combinedText) { combinedText.enabled = true; var c = combinedText.color; c.a = 1f; combinedText.color = c; }
//         if (title)        { title.enabled        = true; var c = title.color;        c.a = 1f; title.color        = c; }
//         if (ingredients)  { ingredients.enabled  = true; var c = ingredients.color;  c.a = 1f; ingredients.color  = c; }
//         if (fact)         { fact.enabled         = true; var c = fact.color;         c.a = 1f; fact.color         = c; }
//         if (icon)         { icon.enabled         = true; icon.raycastTarget = false; }
//     }

//     public void Bind(ReactionDefinition def, bool highlight = false)
//     {
//         if (!def) return;

//         // -------- Read fields directly from ReactionDefinition --------
//         // Strings
//         string productName = GetString(def, "productName");
//         string displayName = GetString(def, "displayName") ?? GetString(def, "resultName");
//         string funFactText = GetString(def, "funFact");

//         // Compose lines in desired order: Product → Display → Fun Fact (skip empties)
//         var lines = new List<string>();
//         if (!string.IsNullOrWhiteSpace(productName)) lines.Add(productName);
//         if (!string.IsNullOrWhiteSpace(displayName) &&
//             (string.IsNullOrWhiteSpace(productName) || !string.Equals(productName, displayName)))
//             lines.Add(displayName);
//         if (!string.IsNullOrWhiteSpace(funFactText)) lines.Add(funFactText);

//         if (lines.Count == 0) lines.Add(def.name); // fallback

//         // Write combined text
//         if (combinedText)
//         {
//             if (lines.Count > 0) lines[0] = $"<b>{lines[0]}</b>"; // bold the first line
//             combinedText.text = string.Join("\n", lines);
//         }

//         // Optional: legacy fields if you still want them visible
//         if (title) title.text = productName ?? displayName ?? def.name;
//         if (fact)  fact.text  = funFactText ?? string.Empty;

//         // Optional: show “A + B” line if you want
//         if (ingredients && def.inputs != null && def.inputs.Count > 0)
//         {
//             var names = def.inputs.Where(i => i != null)
//                                   .Select(i => string.IsNullOrWhiteSpace(i.displayName) ? i.name : i.displayName);
//             ingredients.text = string.Join(" + ", names);
//         }

//         // ICON — prefer productIcon, then resultIcon, then icon
//         if (icon)
//         {
//             var spr = GetSpriteAny(def, "productIcon", "resultIcon", "icon");
//             if (spr)
//             {
//                 icon.sprite = spr;
//                 icon.preserveAspect = true;
//                 icon.enabled = true;
//             }
//             else icon.enabled = false;
//         }

//         // Unique key (sorted inputs)
//         key = BuildKey(def);

//         // Delete button
//         if (deleteButton)
//         {
//             deleteButton.onClick.RemoveAllListeners();
//             deleteButton.onClick.AddListener(() => RecipeBookManager.Instance?.Remove(key));
//         }

//         // Badges
//         RefreshBadgesFromDefinition(def);

//         // Tiny highlight flash
//         if (highlight)
//         {
//             if (isActiveAndEnabled) StartCoroutine(Flash());
//             else _pendingFlash = true;
//         }
//     }

//     private void OnEnable()
//     {
//         if (_pendingFlash)
//         {
//             _pendingFlash = false;
//             StartCoroutine(Flash());
//         }
//     }

//     private System.Collections.IEnumerator Flash()
//     {
//         var t = transform;
//         var s0 = t.localScale;
//         t.localScale = s0 * 1.06f;
//         yield return new WaitForSeconds(0.12f);
//         t.localScale = s0;
//     }

//     // ---------------- helpers ----------------

//     private static string BuildKey(ReactionDefinition def)
//     {
//         if (def.inputs != null && def.inputs.Count > 0)
//         {
//             var parts = def.inputs
//                 .Where(i => i != null)
//                 .Select(i => !string.IsNullOrWhiteSpace(i.id)
//                                 ? i.id
//                                 : (string.IsNullOrWhiteSpace(i.name) ? "unknown" : i.name.Trim().ToLowerInvariant()))
//                 .OrderBy(s => s, System.StringComparer.Ordinal);
//             return string.Join("+", parts);
//         }
//         // fallback
//         var rn = GetString(def, "resultName");
//         if (!string.IsNullOrWhiteSpace(rn)) return rn.Trim().ToLowerInvariant();
//         return def.name.Trim().ToLowerInvariant();
//     }

//     private static string GetString(object obj, string fieldName)
//     {
//         if (obj == null) return null;
//         var f = obj.GetType().GetField(fieldName);
//         if (f == null) return null;
//         var v = f.GetValue(obj) as string;
//         return string.IsNullOrWhiteSpace(v) ? null : v;
//     }

//     private static Sprite GetSpriteAny(object obj, params string[] fieldNames)
//     {
//         if (obj == null) return null;
//         var t = obj.GetType();
//         foreach (var fn in fieldNames)
//         {
//             var f = t.GetField(fn);
//             if (f == null) continue;
//             var s = f.GetValue(obj) as Sprite;
//             if (s) return s;
//         }
//         return null;
//     }

//     private void RefreshBadgesFromDefinition(ReactionDefinition def)
//     {
//         if (!badgesBar) return;

//         // Clear old
//         for (int i = badgesBar.childCount - 1; i >= 0; i--)
//             Destroy(badgesBar.GetChild(i).gameObject);

//         if (!badgeIconPrefab) return;

//         // Support either List<ReactionBadge> badges or List<Sprite> extraIcons
//         var type = def.GetType();

//         var badgesField = type.GetField("badges");
//         if (badgesField != null)
//         {
//             var listObj = badgesField.GetValue(def) as System.Collections.IEnumerable;
//             if (listObj != null)
//             {
//                 foreach (var item in listObj)
//                 {
//                     if (item == null) continue;
//                     var iconField = item.GetType().GetField("icon");
//                     if (iconField == null) continue;
//                     var sprite = iconField.GetValue(item) as Sprite;
//                     if (!sprite) continue;
//                     CreateBadge(sprite);
//                 }
//                 return;
//             }
//         }

//         var extraIconsField = type.GetField("extraIcons");
//         if (extraIconsField != null)
//         {
//             var list = extraIconsField.GetValue(def) as IEnumerable<Sprite>;
//             if (list != null)
//                 foreach (var s in list) if (s) CreateBadge(s);
//         }
//     }

//     private void CreateBadge(Sprite s)
//     {
//         var go  = Instantiate(badgeIconPrefab, badgesBar);
//         var img = go.GetComponent<Image>() ?? go.GetComponentInChildren<Image>();
//         if (img)
//         {
//             img.sprite = s;
//             img.preserveAspect = true;
//             img.raycastTarget  = false;
//         }
//     }

//     // --- DIAGNOSTICS (optional, right-click the component) ---
//     [ContextMenu("DIAG: Log Field Status")]
//     private void __Diag_LogFieldStatus()
//     {
//         Debug.Log($"[RecipeRowUI] on '{name}' " +
//                   $"\n  icon: {icon}" +
//                   $"\n  combinedText: {combinedText}" +
//                   $"\n  title: {title}" +
//                   $"\n  ingredients: {ingredients}" +
//                   $"\n  fact: {fact}" +
//                   $"\n  deleteButton: {deleteButton}" +
//                   $"\n  badgesBar: {badgesBar}" +
//                   $"\n  badgeIconPrefab: {badgeIconPrefab}");
//     }

//     [ContextMenu("DIAG: Force Dummy Texts")]
//     private void __Diag_ForceDummyTexts()
//     {
//         if (combinedText) { combinedText.enabled = true; combinedText.text = "<b>Product</b>\nDisplay Name\nFun fact here..."; var c = combinedText.color; c.a = 1f; combinedText.color = c; }
//         if (title)        { title.enabled = true;        title.text = "Dummy Title";        var c = title.color; c.a = 1f; title.color = c; }
//         if (ingredients)  { ingredients.enabled = true;  ingredients.text = "A + B";        var c = ingredients.color; c.a = 1f; ingredients.color = c; }
//         if (fact)         { fact.enabled = true;         fact.text = "Fun fact here...";    var c = fact.color; c.a = 1f; fact.color = c; }
//         if (icon)         { icon.enabled = true; }
//         Debug.Log("[RecipeRowUI] Forced dummy texts & enabled components.");
//     }
// }
