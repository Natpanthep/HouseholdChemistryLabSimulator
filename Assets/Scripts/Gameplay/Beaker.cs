using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ScienceLab; // ReactionDefinition, EffectType, IngredientSO, etc.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Beaker : MonoBehaviour
{
    [Header("Data")]
    public ReactionDatabase database;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer liquidRenderer;

    [Header("Particles")]
    [SerializeField] private ParticleSystem bubblesFX;
    [SerializeField] private ParticleSystem smokeFX;
    [SerializeField] private ParticleSystem sparksFX;
    [SerializeField] private ParticleSystem heatFX;

    [Header("Click FX")]
    [SerializeField] private ParticleSystem clickFX;

    [Header("Audio (one source per effect)")]
    [SerializeField] private AudioSource bubblesAudio;
    [SerializeField] private AudioSource smokeAudio;
    [SerializeField] private AudioSource sparksAudio;
    [SerializeField] private AudioSource heatAudio;

    [Header("Click Audio")]
    [SerializeField] private AudioSource clickAudio;
    [SerializeField] private AudioClip  clickClip;
    [SerializeField] private float      clickCooldown = 0.1f;
    private bool canPlayClick = true;

    [Header("Success/Fail Audio")]
    [SerializeField] private AudioSource successAudio;
    [SerializeField] private AudioClip  successClip;
    [SerializeField] private AudioSource failAudio;
    [SerializeField] private AudioClip  failClip;

    [Header("Bubble Control")]
    // [SerializeField] private bool loopParticles = true; // not used now
    [SerializeField] private bool  loopSound      = false;
    [SerializeField] private float bubbleInterval = 4f;
    private Coroutine bubbleLoopCo;

    [Header("Result UI")]
    public GameObject resultRoot;
    public TMP_Text   resultLabel;
    public Image      resultIconImage;
    public Transform  resultBadgeBar;
    public GameObject badgeIconPrefab;
    [SerializeField] private float resultLabelDuration = 2.5f;

    [Header("Success Stack UI")]
    public GameObject successStackPanel; // parent panel
    public TMP_Text   successStackText;  // text showing combo/fail
    public Image      successStackIcon;  // icon only on success
    private int       currentCombo = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly List<IngredientSO> current = new();
    private Coroutine hideTextCo;

    // ---------------- Unity lifecycle ----------------

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void Start()
    {
        if (resultLabel)      resultLabel.text = "";
        if (resultIconImage)  resultIconImage.enabled = false;
        if (resultBadgeBar)   resultBadgeBar.gameObject.SetActive(false);
        if (resultRoot)       resultRoot.SetActive(false);

        if (successStackText) successStackText.text = "";
        if (successStackIcon) successStackIcon.enabled = false;
        if (successStackPanel) successStackPanel.SetActive(true);
    }

    private void OnDisable()
    {
        if (hideTextCo != null)
        {
            StopCoroutine(hideTextCo);
            hideTextCo = null;
        }
        StopBubbleLoop();
    }

    // ---------------- Trigger / input ----------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ing = other.GetComponent<Ingredient>();
        if (ing == null || ing.data == null || ing.consumed) return;

        if (debugLogs) Debug.Log($"[Beaker] Saw ingredient: {ing.data.name}");

        // Click sound
        if (clickAudio && clickClip && canPlayClick)
        {
            clickAudio.PlayOneShot(clickClip, 1f);
            StartCoroutine(ClickCooldown());
        }

        // Click FX
        if (clickFX != null) clickFX.Play();

        // Add ingredient to current mixture
        current.Add(ing.data);
        ing.consumed = true;

        // Hide bottle instead of destroying (for reset later)
        ing.gameObject.SetActive(false);

        TryReact();
    }

    private IEnumerator ClickCooldown()
    {
        canPlayClick = false;
        yield return new WaitForSeconds(clickCooldown);
        canPlayClick = true;
    }

    // ---------------- Core reaction logic ----------------

    private void TryReact()
    {
        if (!database)
        {
            if (debugLogs) Debug.LogWarning("[Beaker] ReactionDatabase is not assigned.");
            return;
        }

        // Only evaluate when we have at least 2 ingredients
        if (current.Count >= 2)
        {
            if (database.TryGetReaction(current, out var def))
            {
                HandleSuccess(def);
                current.Clear();       // clear beaker contents after a valid reaction
            }
            else
            {
                HandleFailed();
                current.Clear();       // clear beaker contents after a fail
            }
        }
    }

    private void HandleSuccess(ReactionDefinition def)
    {
        if (liquidRenderer) liquidRenderer.color = def.resultColor;

        PlayEffect(def.effect, def.resultColor, 1f);
        ShowResultUI(def);

        // --- NEW: combo only when recipe is NEW ---
        bool isNewRecipe = true;

        if (RecipeBookManager.Instance != null)
        {
            var mgr = RecipeBookManager.Instance;
            var t   = mgr.GetType();
            var m   = t.GetMethod("RegisterAndReportNew");

            if (m != null)
            {
                // use bool RegisterAndReportNew(ReactionDefinition def) if it exists
                object result = m.Invoke(mgr, new object[] { def });
                if (result is bool b) isNewRecipe = b;
            }
            else
            {
                // fallback to old behaviour
                mgr.Register(def);
                isNewRecipe = true; // behaves like before if you haven't added the new method
            }
        }

        if (isNewRecipe)
        {
            currentCombo++;
        }
        // -----------------------------------------

        UpdateComboUI(success: true);

        if (successAudio && successClip)
            successAudio.PlayOneShot(successClip);

        if (hideTextCo != null) StopCoroutine(hideTextCo);
        hideTextCo = StartCoroutine(HideTextAfter(resultLabelDuration));
    }

    private void HandleFailed()
    {
        // break combo
        currentCombo = 0;
        UpdateComboUI(success: false);

        if (failAudio && failClip)
            failAudio.PlayOneShot(failClip);

        if (hideTextCo != null) StopCoroutine(hideTextCo);
        hideTextCo = StartCoroutine(HideTextAfter(resultLabelDuration));
    }

    private void UpdateComboUI(bool success = true)
    {
        if (!successStackText || !successStackIcon) return;

        if (success)
        {
            successStackText.text = currentCombo.ToString();
            successStackIcon.enabled = true;
        }
        else
        {
            successStackText.text = "Failed";
            successStackIcon.enabled = false;
        }
    }

    private IEnumerator HideTextAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (resultLabel)     resultLabel.text = "";
        if (resultIconImage) resultIconImage.enabled = false;
        if (resultBadgeBar)  resultBadgeBar.gameObject.SetActive(false);
        if (resultRoot)      resultRoot.SetActive(false);
        hideTextCo = null;
    }

    // ---------------- FX helpers ----------------

    private void PlayEffect(EffectType type)
        => PlayEffect(type, liquidRenderer ? liquidRenderer.color : Color.white, 1f);

    private void PlayEffect(EffectType type, Color liquidColor, float intensity, bool overrideParticleColor = true)
    {
        StopAllFx();
        StopAllSfx();

        void PlayPS(ParticleSystem ps, Color? tint = null, float burstMult = 1f,
                    bool loopSfx = false, AudioSource sfxSource = null, AudioClip sfxClip = null, float sfxVolume = 1f)
        {
            if (ps == null) return;

            var main = ps.main;
            if (overrideParticleColor && tint.HasValue)
                main.startColor = tint.Value;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.EnableKeyword("_EMISSION");
                Color particleColor = Color.white;
                if (main.startColor.mode == ParticleSystemGradientMode.Color)
                    particleColor = main.startColor.color;
                renderer.material.SetColor("_EmissionColor", particleColor);
            }

            var emission = ps.emission;
            if (emission.burstCount > 0)
            {
                var b = emission.GetBurst(0);
                var c = b.count;
                if (c.mode == ParticleSystemCurveMode.Constant)
                    b.count = new ParticleSystem.MinMaxCurve(Mathf.Max(1f, c.constant * Mathf.Max(0.1f, burstMult)));
                emission.SetBurst(0, b);
            }

            ps.Clear(true);
            ps.Play();

            if (loopSfx && sfxSource != null && sfxClip != null)
            {
                sfxSource.clip   = sfxClip;
                sfxSource.loop   = true;
                sfxSource.volume = sfxVolume;
                sfxSource.Play();
            }
        }

        switch (type)
        {
            case EffectType.Bubbles:
                PlayPS(bubblesFX, liquidColor, intensity,
                       loopSfx: loopSound,
                       sfxSource: bubblesAudio,
                       sfxClip: bubblesAudio?.clip,
                       sfxVolume: 1f);
                break;

            case EffectType.Smoke:
                PlayPS(smokeFX, null, intensity,
                       loopSfx: true,
                       sfxSource: smokeAudio,
                       sfxClip: smokeAudio?.clip,
                       sfxVolume: 1f);
                break;

            case EffectType.Sparks:
                PlayPS(sparksFX, Color.white, intensity,
                       loopSfx: true,
                       sfxSource: sparksAudio,
                       sfxClip: sparksAudio?.clip,
                       sfxVolume: 1f);
                break;

            case EffectType.Heat:
                PlayPS(heatFX, null, intensity,
                       loopSfx: true,
                       sfxSource: heatAudio,
                       sfxClip: heatAudio?.clip,
                       sfxVolume: 1f);
                break;

            case EffectType.ColorChange:
            default:
                break;
        }
    }

    private void StopAllFx()
    {
        if (bubblesFX != null) bubblesFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (smokeFX   != null) smokeFX.Stop(true,   ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sparksFX  != null) sparksFX.Stop(true,  ParticleSystemStopBehavior.StopEmittingAndClear);
        if (heatFX    != null) heatFX.Stop(true,    ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void StopAllSfx()
    {
        if (bubblesAudio != null) { bubblesAudio.Stop(); bubblesAudio.loop = false; }
        if (smokeAudio   != null) { smokeAudio.Stop();   smokeAudio.loop   = false; }
        if (sparksAudio  != null) { sparksAudio.Stop();  sparksAudio.loop  = false; }
        if (heatAudio    != null) { heatAudio.Stop();    heatAudio.loop    = false; }
    }

    private void StartBubbleLoop()
    {
        StopBubbleLoop();
        bubbleLoopCo = StartCoroutine(BubbleLoopCoroutine());
    }

    private void StopBubbleLoop()
    {
        if (bubbleLoopCo != null)
        {
            StopCoroutine(bubbleLoopCo);
            bubbleLoopCo = null;
        }
        if (bubblesAudio != null) bubblesAudio.Stop();
    }

    private IEnumerator BubbleLoopCoroutine()
    {
        while (true)
        {
            if (bubblesAudio != null && bubblesAudio.clip != null)
                bubblesAudio.PlayOneShot(bubblesAudio.clip, 1f);
            yield return new WaitForSeconds(bubbleInterval);
        }
    }

    // ---------------- Result UI ----------------

    private void ShowResultUI(ReactionDefinition r)
    {
        if (resultRoot != null)
            resultRoot.SetActive(true);

        if (resultLabel != null)
        {
            string productName = GetStringField(r, "productName");
            string displayName = GetStringField(r, "displayName");
            if (string.IsNullOrEmpty(displayName))
                displayName = GetStringField(r, "resultName");

            string funFact = GetStringField(r, "funFact");

            // ingredients line
            string ingredientsLine = "";
            if (r.inputs != null && r.inputs.Count > 0)
            {
                List<string> names = new List<string>();
                foreach (var ing in r.inputs)
                {
                    if (ing == null) continue;
                    string nm = !string.IsNullOrEmpty(ing.displayName) ? ing.displayName : ing.name;
                    if (!string.IsNullOrEmpty(nm)) names.Add(nm);
                }
                if (names.Count > 0)
                    ingredientsLine = string.Join(" + ", names.ToArray());
            }

            var lines = new List<string>();

            if (!string.IsNullOrEmpty(productName))
                lines.Add(productName);
            else if (!string.IsNullOrEmpty(displayName))
                lines.Add(displayName);
            else
                lines.Add(r.name);

            if (!string.IsNullOrEmpty(ingredientsLine))
                lines.Add(ingredientsLine);

            if (!string.IsNullOrEmpty(funFact))
                lines.Add(funFact);

            resultLabel.text = string.Join("\n", lines.ToArray());
        }

        if (resultIconImage != null)
        {
            var icon = r.icon;
            if (icon != null)
            {
                resultIconImage.sprite = icon;
                resultIconImage.preserveAspect = true;
                resultIconImage.enabled = true;
            }
            else
                resultIconImage.enabled = false;
        }

        if (resultBadgeBar != null)
        {
            bool hasBadges = r.badges != null && r.badges.Count > 0;
            resultBadgeBar.gameObject.SetActive(hasBadges);

            if (hasBadges)
                RefreshBadges(resultBadgeBar, badgeIconPrefab, r.badges);
            else
                ClearChildren(resultBadgeBar);
        }
    }

    private static void RefreshBadges(Transform bar, GameObject prefab, List<ReactionBadge> badges)
    {
        if (bar == null || prefab == null) return;
        ClearChildren(bar);
        foreach (var b in badges)
        {
            if (b == null || b.icon == null) continue;
            var go  = Object.Instantiate(prefab, bar);
            var img = go.GetComponent<Image>() ?? go.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.sprite         = b.icon;
                img.preserveAspect = true;
                img.raycastTarget  = false;
                img.color          = b.color;
            }
        }
    }

    private static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.Destroy(t.GetChild(i).gameObject);
    }

    private static string GetStringField(object obj, string fieldName)
    {
        if (obj == null) return null;
        var type = obj.GetType();
        var f    = type.GetField(fieldName);
        if (f == null) return null;
        var v = f.GetValue(obj) as string;
        return string.IsNullOrEmpty(v) ? null : v;
    }

    // ---------------- Full reset (editor / debug) ----------------

    [ContextMenu("Clear (Reset)")]
    public void Clear()
    {
        current.Clear();
        StopAllFx();
        StopAllSfx();
        StopBubbleLoop();

        if (liquidRenderer != null)  liquidRenderer.color = Color.white;
        if (resultLabel != null)     resultLabel.text = "";
        if (resultIconImage != null) resultIconImage.enabled = false;
        if (resultBadgeBar != null)  resultBadgeBar.gameObject.SetActive(false);
        if (resultRoot != null)      resultRoot.SetActive(false);

        currentCombo = 0;
        if (successStackText != null) successStackText.text = "";
        if (successStackIcon != null) successStackIcon.enabled = false;
    }

    // ---------------- Visual-only reset (for Reset button) ----------------

    public void VisualReset()
    {
        // Empty the beaker contents, BUT keep combo count
        current.Clear();

        // Stop current FX & sounds
        StopAllFx();
        StopAllSfx();
        StopBubbleLoop();

        // Clear beaker visuals
        if (liquidRenderer)    liquidRenderer.color = Color.white;
        if (resultLabel)       resultLabel.text = "";
        if (resultIconImage)   resultIconImage.enabled = false;
        if (resultBadgeBar)    resultBadgeBar.gameObject.SetActive(false);
        if (resultRoot)        resultRoot.SetActive(false);

        // Important: DO NOT touch currentCombo or the combo UI
    }

        // ---------------- Combo-only reset (for Delete button) ----------------
    public void ResetComboOnly()
    {
        currentCombo = 0;

        if (successStackText != null)
            successStackText.text = "";

        if (successStackIcon != null)
            successStackIcon.enabled = false;
    }

}
