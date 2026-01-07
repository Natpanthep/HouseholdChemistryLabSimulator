using UnityEngine;

public class SettingsManagerLite : MonoBehaviour
{
    private static bool _created;

    private void Awake()
    {
        if (_created) { Destroy(gameObject); return; }
        _created = true;
        DontDestroyOnLoad(gameObject);
        ApplySaved();
    }

    public static void ApplySaved()
    {
        // Defaults on first run
        if (!PlayerPrefs.HasKey("vol")) PlayerPrefs.SetFloat("vol", 1f);        // loudest by default
        if (!PlayerPrefs.HasKey("fs"))  PlayerPrefs.SetInt("fs", Screen.fullScreen ? 1 : 0);
        if (!PlayerPrefs.HasKey("q"))   PlayerPrefs.SetInt("q", QualitySettings.GetQualityLevel());
        PlayerPrefs.Save();

        float vol = Mathf.Clamp01(PlayerPrefs.GetFloat("vol", 1f));
        bool  fs  = PlayerPrefs.GetInt("fs", Screen.fullScreen ? 1 : 0) == 1;
        int   q   = Mathf.Clamp(PlayerPrefs.GetInt("q", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);

        AudioListener.volume = vol;
        Screen.fullScreen = fs;
        QualitySettings.SetQualityLevel(q, true);
    }
}
