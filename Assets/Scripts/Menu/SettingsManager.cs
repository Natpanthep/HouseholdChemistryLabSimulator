using UnityEngine;
using UnityEngine.Audio;

#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager I { get; private set; }

    [Header("Audio Mixer (recommended)")]
    [Tooltip("Assign your Master mixer asset. Expose MasterVol/MusicVol/SFXVol.")]
    [SerializeField] private AudioMixer masterMixer;

    [Header("Exposed Mixer Parameter Names")]
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam  = "MusicVol";
    [SerializeField] private string sfxParam    = "SFXVol";

    [Header("Default Values (first run)")]
    [Range(0f, 1f)] [SerializeField] private float defaultMaster = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultMusic  = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultSfx    = 1f;
    [SerializeField] private bool   defaultFullscreen = true;

    [Header("Volume Mapping")]
    [Tooltip("Slider(0..1) → decibels. -80 dB ~= silent, 0 dB = full.")]
    [SerializeField] private float minVolumeDb = -80f;

    // PlayerPrefs keys (kept same as earlier versions)
    private const string KEY_VOL_MASTER = "vol_master";
    private const string KEY_VOL_MUSIC  = "vol_music";
    private const string KEY_VOL_SFX    = "vol_sfx";
    private const string KEY_FULLSCREEN = "fs";
    private const string KEY_QUALITY    = "q";

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        LoadAndApplyAll();
    }

    // -------------------- Public API --------------------

    public void SetMasterVolume(float v01) => SetVolume(masterParam, v01, KEY_VOL_MASTER, defaultMaster, affectAudioListenerFallback: true);
    public void SetMusicVolume (float v01) => SetVolume(musicParam,  v01, KEY_VOL_MUSIC,  defaultMusic,  affectAudioListenerFallback: false);
    public void SetSfxVolume   (float v01) => SetVolume(sfxParam,    v01, KEY_VOL_SFX,    defaultSfx,    affectAudioListenerFallback: false);

    public void SetFullscreen(bool on)
    {
        Screen.fullScreen = on;
        PlayerPrefs.SetInt(KEY_FULLSCREEN, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set Unity quality tier and optionally tweak URP basics.
    /// </summary>
    public void SetQualityLevel(int index, bool applyExpensiveChanges = true)
    {
        index = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(index, applyExpensiveChanges);
        PlayerPrefs.SetInt(KEY_QUALITY, index);
        PlayerPrefs.Save();

        // Optional: small URP nudges that often track quality
        #if UNITY_RENDER_PIPELINE_UNIVERSAL
        var rp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (rp != null)
        {
            // Example: MSAA higher on high tiers, renderScale = 1.0
            rp.msaaSampleCount = index >= 3 ? 4 : 0; // Low tiers: 0, High+: 4x
            rp.renderScale = 1.0f;                   // Keep native scale; manage resolution elsewhere
        }
        #endif
    }

    /// <summary>
    /// Loads saved prefs (or defaults on first run) and applies all.
    /// Call this after changing big things, or rely on Awake.
    /// </summary>
    public void LoadAndApplyAll()
    {
        // Backward-compat: if old plain "vol" exists but no master key, pull it in
        if (!PlayerPrefs.HasKey(KEY_VOL_MASTER) && PlayerPrefs.HasKey("vol"))
            PlayerPrefs.SetFloat(KEY_VOL_MASTER, Mathf.Clamp01(PlayerPrefs.GetFloat("vol", 1f)));

        float m = PlayerPrefs.GetFloat(KEY_VOL_MASTER, defaultMaster);
        float mu= PlayerPrefs.GetFloat(KEY_VOL_MUSIC,  defaultMusic);
        float s = PlayerPrefs.GetFloat(KEY_VOL_SFX,    defaultSfx);
        bool  fs= PlayerPrefs.GetInt(KEY_FULLSCREEN,   defaultFullscreen ? 1 : 0) == 1;
        int   q = PlayerPrefs.GetInt(KEY_QUALITY,      QualitySettings.GetQualityLevel());

        SetMasterVolume(m);
        SetMusicVolume(mu);
        SetSfxVolume(s);
        SetFullscreen(fs);
        SetQualityLevel(q, true);
    }

    // -------------------- Internals --------------------

    private void SetVolume(string exposedParam, float slider01, string key, float fallbackDefault, bool affectAudioListenerFallback)
    {
        slider01 = Mathf.Clamp01(slider01);
        PlayerPrefs.SetFloat(key, slider01);
        PlayerPrefs.Save();

        if (masterMixer != null && !string.IsNullOrEmpty(exposedParam))
        {
            // Convert 0..1 → dB curve, clamp to minVolumeDb..0
            float dB = (slider01 <= 0.0001f) ? minVolumeDb : Mathf.Lerp(minVolumeDb, 0f, Mathf.Log10(Mathf.Lerp(1e-4f, 1f, slider01)) / Mathf.Log10(1f));
            // A simpler mapping often used:
            // float dB = Mathf.Log10(Mathf.Max(slider01, 0.0001f)) * 20f; // maps 1->0dB, 0.5->-6dB, 0.1->-20dB, ~0->-80dB
            masterMixer.SetFloat(exposedParam, dB);
        }
        else
        {
            // Fallback if you don't assign a mixer:
            if (affectAudioListenerFallback)
                AudioListener.volume = slider01; // only master affects global listener
        }
    }

    // Optional utility to reset everything to defaults
    public void ResetToDefaults()
    {
        PlayerPrefs.SetFloat(KEY_VOL_MASTER, defaultMaster);
        PlayerPrefs.SetFloat(KEY_VOL_MUSIC,  defaultMusic);
        PlayerPrefs.SetFloat(KEY_VOL_SFX,    defaultSfx);
        PlayerPrefs.SetInt(KEY_FULLSCREEN,   defaultFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KEY_QUALITY,      QualitySettings.GetQualityLevel());
        PlayerPrefs.Save();
        LoadAndApplyAll();
    }
}
