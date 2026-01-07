using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Volume")]
    [SerializeField] private bool applyVolumeImmediately = true; // false = only apply when pressing Apply()
    private float pendingVolume = 1f;

    [Header("Quality Dropdown Labels")]
    [SerializeField]
    private List<string> displayQualityLabels = new()
    { "Very Low", "Low", "Medium", "High", "Very High", "Ultra" };

    // dropdownIndex -> Unity QualitySettings index
    private List<int> _unityIndexByDropdown = new();

    [Header("Fullscreen")]
    [SerializeField] private bool useBorderlessFullscreen = true; // true = FullScreenWindow, false = ExclusiveFullScreen
    private int lastWindowedW = 1280;
    private int lastWindowedH = 720;

    [Header("Optional Links")]
    [SerializeField] private bool linkResolutionToQuality = false; // OFF = quality only
    [SerializeField] private bool linkUIScaleToQuality   = false; // OFF = UI scale unchanged

    private void Awake()
    {
        BuildQualityMappingAndOptions();
    }

    private void OnEnable()
    {
        // Load saved values and update UI WITHOUT firing events
        float vol = PlayerPrefs.HasKey("vol") ? PlayerPrefs.GetFloat("vol") : 1f;
        bool  fs  = PlayerPrefs.GetInt("fs", Screen.fullScreen ? 1 : 0) == 1;
        int   q   = Mathf.Clamp(PlayerPrefs.GetInt("q", QualitySettings.GetQualityLevel()),
                                0, QualitySettings.names.Length - 1);

        pendingVolume = vol; // remember current saved volume
        if (masterVolumeSlider) masterVolumeSlider.SetValueWithoutNotify(vol);
        if (fullscreenToggle)   fullscreenToggle.SetIsOnWithoutNotify(fs);

        // Select the dropdown item that maps to current Unity quality index
        int ddIndex = FindDropdownIndexForUnityIndex(q);
        if (qualityDropdown && ddIndex >= 0)
            qualityDropdown.SetValueWithoutNotify(ddIndex);

        // Load last windowed size (fallbacks if first run)
        lastWindowedW = PlayerPrefs.GetInt("win_w", Mathf.Max(640, Screen.width));
        lastWindowedH = PlayerPrefs.GetInt("win_h", Mathf.Max(360, Screen.height));
    }

    private void OnValidate()
    {
        // Keep mapping in sync when you edit labels in Inspector
        if (Application.isPlaying) return;
        BuildQualityMappingAndOptions();
    }

    // ---------- UI event handlers ----------

    // Wire THIS to the Slider's OnValueChanged(float)
    public void OnVolumeSliderChanged(float v)
    {
        v = Mathf.Clamp01(v);
        if (applyVolumeImmediately)
        {
            ApplyVolume(v);          // live apply while dragging
        }
        else
        {
            pendingVolume = v;       // only commit on Apply()
        }
        
        Debug.Log($"[Settings] Slider={v} applyNow={applyVolumeImmediately}");
    }

    // Legacy name (if you already wired this in Inspector, it's fine)
    public void OnMasterVolumeChanged(float v) => OnVolumeSliderChanged(v);

    public void OnFullscreenChanged(bool on)
    {
        if (!on)
        {
            // Going windowed: restore last saved window size
            if (!Screen.fullScreen)
            {
                // Already windowed → update memory from current size
                lastWindowedW = Screen.width;
                lastWindowedH = Screen.height;
                PlayerPrefs.SetInt("win_w", lastWindowedW);
                PlayerPrefs.SetInt("win_h", lastWindowedH);
            }

            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(lastWindowedW, lastWindowedH, false);
        }
        else
        {
#if UNITY_WEBGL
            Screen.fullScreen = true;
#else
            Screen.fullScreenMode = useBorderlessFullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.ExclusiveFullScreen;

            // Native desktop res for fullscreen
            int natW = Display.main != null ? Display.main.systemWidth  : Screen.currentResolution.width;
            int natH = Display.main != null ? Display.main.systemHeight : Screen.currentResolution.height;
            Screen.SetResolution(natW, natH, true);
#endif
        }

        PlayerPrefs.SetInt("fs", on ? 1 : 0);
        PlayerPrefs.Save();

        // Keep your quality-linked resolution/render scale consistent after switching mode
        int q = Mathf.Clamp(PlayerPrefs.GetInt("q", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        if (linkResolutionToQuality) ApplyResolutionForTier(q);

        if (linkUIScaleToQuality && qualityDropdown)
            OnQualityChanged(qualityDropdown.value);
    }

    public void OnQualityChanged(int dropdownIndex)
    {
        if (dropdownIndex < 0 || dropdownIndex >= _unityIndexByDropdown.Count) return;

        // Map dropdown -> Unity tier index
        int unityIndex = Mathf.Clamp(_unityIndexByDropdown[dropdownIndex], 0, QualitySettings.names.Length - 1);

        // Always change quality tier
        QualitySettings.SetQualityLevel(unityIndex, true);
        PlayerPrefs.SetInt("q", unityIndex);
        PlayerPrefs.Save();

        // Optional links
        if (linkResolutionToQuality)
            ApplyResolutionForTier(unityIndex);

        if (linkUIScaleToQuality)
        {
            var scaler = FindObjectOfType<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                string tier = QualitySettings.names[unityIndex];
                switch (tier)
                {
                    case "Very Low":  scaler.referenceResolution = new Vector2(1280, 720);  break;
                    case "Low":       scaler.referenceResolution = new Vector2(1440, 810);  break;
                    case "Medium":    scaler.referenceResolution = new Vector2(1600, 900);  break;
                    case "High":      scaler.referenceResolution = new Vector2(1920,1080);  break;
                    case "Very High": scaler.referenceResolution = new Vector2(2560,1440);  break;
                    case "Ultra":     scaler.referenceResolution = new Vector2(3840,2160);  break;
                    default:          scaler.referenceResolution = new Vector2(1920,1080);  break;
                }
                Canvas.ForceUpdateCanvases();
            }
        }

        Debug.Log($"[Settings] Quality => {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
    }

    public void Apply()
    {
        // Commit volume according to mode
        if (applyVolumeImmediately)
        {
            if (masterVolumeSlider) ApplyVolume(masterVolumeSlider.value);
        }
        else
        {
            ApplyVolume(pendingVolume);
        }

        if (fullscreenToggle) OnFullscreenChanged(fullscreenToggle.isOn);
        if (qualityDropdown)  OnQualityChanged(qualityDropdown.value);
    }

    public void Close() => gameObject.SetActive(false);

    // ---------- helpers ----------

    private void ApplyVolume(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("vol", AudioListener.volume);
        PlayerPrefs.Save();
    }

    // OPTIONAL: assign if you use a mixer; otherwise leave null.
    [SerializeField] private UnityEngine.Audio.AudioMixer masterMixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam  = "MusicVol";
    [SerializeField] private string sfxParam    = "SFXVol";

    #if UNITY_EDITOR
    [ContextMenu("Emergency Reset Audio Now")]
    #endif
    public void EmergencyResetAudioNow()
    {
        // Reset saved prefs to safe loud values
        PlayerPrefs.SetFloat("vol", 1f);
        PlayerPrefs.SetFloat("vol_master", 1f);
        PlayerPrefs.SetFloat("vol_music",  1f);
        PlayerPrefs.SetFloat("vol_sfx",    1f);
        PlayerPrefs.Save();

        // Unpause & set global volume
        AudioListener.pause  = false;
        AudioListener.volume = 1f;

        // Reset AudioMixer (if you used one)
        if (masterMixer != null)
        {
            // 0 dB = unity gain (not muted)
            masterMixer.SetFloat(masterParam, 0f);
            masterMixer.SetFloat(musicParam,  0f);
            masterMixer.SetFloat(sfxParam,    0f);
        }

        // Update the UI without firing events
        if (masterVolumeSlider) masterVolumeSlider.SetValueWithoutNotify(1f);

        Debug.Log("[Audio] Emergency reset applied.");
    }

    private void BuildQualityMappingAndOptions()
    {
        _unityIndexByDropdown.Clear();

        var unityNames = QualitySettings.names; // e.g., ["Very Low","Low","Medium","High","Very High","Ultra"]
        var lookup = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < unityNames.Length; i++)
            lookup[Normalize(unityNames[i])] = i;

        var options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < displayQualityLabels.Count; i++)
        {
            var label = displayQualityLabels[i];
            options.Add(new TMP_Dropdown.OptionData(label));

            int idx;
            if (!lookup.TryGetValue(Normalize(label), out idx))
            {
                // fallback: approximate by rank across available tiers
                float t = displayQualityLabels.Count <= 1 ? 0f : (float)i / (displayQualityLabels.Count - 1);
                idx = Mathf.RoundToInt(Mathf.Lerp(0, unityNames.Length - 1, t));
            }
            _unityIndexByDropdown.Add(idx);
        }

        if (qualityDropdown)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(options);
        }
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = char.ToLowerInvariant(s[i]);
            if (c == ' ' || c == '_' || c == '-') continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    private int FindDropdownIndexForUnityIndex(int unityIndex)
    {
        for (int i = 0; i < _unityIndexByDropdown.Count; i++)
            if (_unityIndexByDropdown[i] == unityIndex) return i;

        // choose closest if no exact mapping
        int closest = 0;
        int best = int.MaxValue;
        for (int i = 0; i < _unityIndexByDropdown.Count; i++)
        {
            int d = Mathf.Abs(_unityIndexByDropdown[i] - unityIndex);
            if (d < best) { best = d; closest = i; }
        }
        return closest;
    }

    private void ApplyResolutionForTier(int unityIndex)
    {
        string tier = QualitySettings.names[unityIndex];

        // Pick a render scale per tier (edit as needed)
        float scale = tier switch
        {
            "Very Low"  => 0.5f,
            "Low"       => 0.6f,
            "Medium"    => 0.75f,
            "High"      => 1.0f,
            "Very High" => 1.0f,
            "Ultra"     => 1.0f,
            _           => 1.0f
        };

        int baseW = Display.main != null ? Display.main.systemWidth  : Screen.currentResolution.width;
        int baseH = Display.main != null ? Display.main.systemHeight : Screen.currentResolution.height;
        int w = Mathf.Max(640, Mathf.RoundToInt(baseW * scale));
        int h = Mathf.Max(360, Mathf.RoundToInt(baseH * scale));
        Screen.SetResolution(w, h, Screen.fullScreen);

#if UNITY_RENDER_PIPELINE_UNIVERSAL
        var rp = QualitySettings.renderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (rp != null)
        {
            rp.renderScale = Mathf.Clamp(scale, 0.5f, 1.0f);
            rp.msaaSampleCount = tier switch
            {
                "Very Low" or "Low"                 => 0,
                "Medium"                             => 2,
                "High" or "Very High" or "Ultra"     => 4,
                _                                    => rp.msaaSampleCount
            };
        }
#endif
    }

    private void Update()
    {
        if (!Screen.fullScreen)
        {
            // If user manually resized the window, remember new size
            if (Screen.width != lastWindowedW || Screen.height != lastWindowedH)
            {
                lastWindowedW = Screen.width;
                lastWindowedH = Screen.height;
                PlayerPrefs.SetInt("win_w", lastWindowedW);
                PlayerPrefs.SetInt("win_h", lastWindowedH);
            }
        }
    }
}
