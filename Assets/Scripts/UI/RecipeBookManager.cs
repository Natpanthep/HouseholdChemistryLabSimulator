using System.Collections;                    // <-- for IEnumerator
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ScienceLab;
using TMPro; // TMP_Text

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RecipeBookManager : MonoBehaviour
{
    public static RecipeBookManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private ReactionDatabase database;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform content;
    [SerializeField] private RecipeRowUI rowPrefab;
    [SerializeField] private ScrollRect scrollRect;

    private RectTransform ContentRT => content as RectTransform;

    // ----- runtime -----
    private readonly HashSet<string> unlockedKeys = new();              // keys
    private readonly List<ReactionDefinition> unlockedRecipes = new();  // actual objects for quick access
    private const string SaveKey = "lab.recipes";

    [Header("Progress UI")]
    [SerializeField] private TMP_Text progressText;   // assign ProgressText here
    [SerializeField] private Image progressFill;      // assign circular Image

    [Header("Congrats (100%)")]
    [SerializeField] private Image congratImage;      // drag your PNG Image here
    [SerializeField] private AudioSource congratsAudio;   // optional AudioSource (plays clip below if set)
    [SerializeField] private AudioClip congratsClip;      // optional fallback clip
    [SerializeField] private float fadeInTime  = 0.5f;    // seconds
    [SerializeField] private float holdTime    = 3.0f;    // seconds
    [SerializeField] private float fadeOutTime = 1.5f;    // seconds
    private bool hasShownCongrat = false;
    private Coroutine congratsCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ensure congrat image starts hidden
        if (congratImage != null)
        {
            var c = congratImage.color;
            c.a = 0f;
            congratImage.color = c;
            congratImage.gameObject.SetActive(false);
        }

        Load();
        if (panel) panel.SetActive(false);
        RefreshUI();
        UpdateProgressUI(); // initialize progress
    }

    private void Start()
    {
        // optional test
        if (database && database.reactions.Count > 0)
        {
            // Register(database.reactions[0]);
        }
    }

    // ----- Public API -----
    public void TogglePanel()
    {
        if (!panel) return;
        bool open = !panel.activeSelf;
        panel.SetActive(open);
        if (open)
        {
            RefreshUI();
            if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void Register(ReactionDefinition def)
    {
        if (!def) return;
        string key = MakeKey(def.inputs);

        if (unlockedKeys.Add(key))
        {
            unlockedRecipes.Add(def);
            AddRow(def, highlight: panel && panel.activeInHierarchy);
            Save();
            RebuildLayout();
            UpdateProgressUI(); // update bubble
        }
    }

    public bool RegisterAndReportNew(ReactionDefinition def)
    {
        if (!def) return false;

        string key = MakeKey(def.inputs);

        if (unlockedKeys.Add(key))
        {
            unlockedRecipes.Add(def);
            AddRow(def, highlight: panel && panel.activeInHierarchy);
            Save();
            RebuildLayout();
            UpdateProgressUI();
            return true;    // NEW recipe discovered
        }

        return false;       // It was already discovered before
    }

    public void Remove(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (unlockedKeys.Remove(key))
        {
            unlockedRecipes.RemoveAll(r => MakeKey(r.inputs) == key);
            Save();
            RefreshUI();
            UpdateProgressUI(); // update bubble
        }
    }

    public void Remove(ReactionDefinition def)
    {
        if (!def) return;
        Remove(MakeKey(def.inputs));
    }

    [ContextMenu("Clear Book Progress")]
    public void ClearProgress()
    {
        unlockedKeys.Clear();
        unlockedRecipes.Clear();
        hasShownCongrat = false;                  // allow congrats to show again next time
        StopCongratsIfRunning();
        Save();
        RefreshUI();
        UpdateProgressUI(); // reset bubble
    }

    // ----- UI -----
    public void RefreshUI()
    {
        if (!content) return;

        ClearChildren(content);

        if (!database) { RebuildLayout(); return; }

        foreach (var def in unlockedRecipes.OrderBy(r => r.displayName))
        {
            AddRow(def, highlight: false);
        }

        RebuildLayout();
    }

    private void AddRow(ReactionDefinition def, bool highlight = false)
    {
        if (!rowPrefab || !content) return;

        var row = Instantiate(rowPrefab, content);
        var rt = (RectTransform)row.transform;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;

        row.Bind(def, highlight);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

        #if UNITY_EDITOR
        Debug.Log($"[RecipeBook] Added row for: {def.displayName ?? def.name}");
        #endif
    }

    private void RebuildLayout()
    {
        if (!ContentRT) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRT);
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    // ----- Save/Load -----
    [System.Serializable] private class SaveWrap { public List<string> keys = new(); }

    private void Save()
    {
        var wrap = new SaveWrap { keys = unlockedKeys.ToList() };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrap));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        unlockedKeys.Clear();
        unlockedRecipes.Clear();
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            var wrap = JsonUtility.FromJson<SaveWrap>(json);
            if (wrap?.keys != null)
            {
                foreach (var k in wrap.keys)
                {
                    unlockedKeys.Add(k);
                    var def = database?.reactions.FirstOrDefault(r => MakeKey(r.inputs) == k);
                    if (def != null) unlockedRecipes.Add(def);
                }
            }
        }
    }

    // ----- Progress UI -----
    private void UpdateProgressUI()
    {
        if (database == null) return;

        int totalRecipes = database.reactions.Count;
        int unlockedCount = unlockedKeys.Count;

        float percent = totalRecipes > 0 ? unlockedCount / (float)totalRecipes : 0f;

        if (progressFill != null)
            progressFill.fillAmount = percent;

        if (progressText != null)
            progressText.text = Mathf.RoundToInt(percent * 100f) + "%";

        // Trigger congrats exactly once at 100%
        if (percent >= 1f && !hasShownCongrat)
        {
            hasShownCongrat = true;
            if (congratsCo != null) StopCoroutine(congratsCo);
            if (congratImage != null)
                congratsCo = StartCoroutine(ShowCongratRoutine());
        }
    }

    // ----- Congrats helpers -----
    private void StopCongratsIfRunning()
    {
        if (congratsCo != null)
        {
            StopCoroutine(congratsCo);
            congratsCo = null;
        }
        if (congratImage != null)
        {
            var c = congratImage.color;
            c.a = 0f;
            congratImage.color = c;
            congratImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowCongratRoutine()
    {
        if (congratImage == null)
            yield break;

        // show + start from transparent
        congratImage.gameObject.SetActive(true);
        var col = congratImage.color;
        col.a = 0f;
        congratImage.color = col;

        // play sound (AudioSource preferred, else PlayClipAtPoint)
        if (congratsAudio != null)
        {
            if (congratsClip != null) { congratsAudio.clip = congratsClip; }
            if (congratsAudio.clip != null) congratsAudio.Play();
        }
        else if (congratsClip != null)
        {
            AudioSource.PlayClipAtPoint(congratsClip, Vector3.zero, 1f);
        }

        // fade in
        float t = 0f;
        float fi = Mathf.Max(0.01f, fadeInTime);
        while (t < fi)
        {
            t += Time.deltaTime;
            col.a = Mathf.SmoothStep(0f, 1f, t / fi);
            congratImage.color = col;
            yield return null;
        }
        col.a = 1f; congratImage.color = col;

        // hold
        yield return new WaitForSeconds(Mathf.Max(0f, holdTime));

        // fade out
        t = 0f;
        float fo = Mathf.Max(0.01f, fadeOutTime);
        while (t < fo)
        {
            t += Time.deltaTime;
            col.a = Mathf.SmoothStep(1f, 0f, t / fo);
            congratImage.color = col;
            yield return null;
        }
        col.a = 0f; congratImage.color = col;
        congratImage.gameObject.SetActive(false);

        congratsCo = null;
    }

    // ----- Helpers -----
    private static string MakeKey(IEnumerable<IngredientSO> inputs)
    {
        return string.Join("+",
            inputs.Where(i => i != null)
                  .Select(i => i.id)
                  .OrderBy(s => s, System.StringComparer.Ordinal));
    }

    private static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    [ContextMenu("DIAG: Spawn Test Row")]
    private void __Diag_SpawnTestRow()
    {
        if (!rowPrefab || !content)
        {
            Debug.LogError("[RecipeBookManager] rowPrefab or content is not assigned.");
            return;
        }
        var row = Instantiate(rowPrefab, content);
        var rt = (RectTransform)row.transform;
        rt.localScale = Vector3.one; rt.anchoredPosition = Vector2.zero;

        row.SendMessage("__Diag_ForceDummyTexts", SendMessageOptions.DontRequireReceiver);
        Debug.Log("[RecipeBookManager] Spawned a manual test row with dummy text.");
    }

    [ContextMenu("DIAG: Log Wiring")]
    private void __Diag_LogWiring()
    {
        Debug.Log($"[RecipeBookManager] panel={panel}, content={content}, rowPrefab={rowPrefab}, scrollRect={scrollRect}, database={(database ? database.name : "NULL")}");
    }
}
