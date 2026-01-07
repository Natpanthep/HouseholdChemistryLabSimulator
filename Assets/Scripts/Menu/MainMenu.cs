using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button infoButton;
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject infoPanel;       // optional: credits panel
    [SerializeField] private GameObject howToPlayPanel;  // <-- new

    [Header("Game")]
    [SerializeField] private string gameSceneName = "Main";

    [Header("Credits Text")]
    [SerializeField] private TMP_Text creditsText;
    [SerializeField, TextArea(3, 10)]
    private string creditsInfo = "Game Design: Natpanthep\n" +
                                 "Programming: Natpanthep\n" +
                                 "Art: Flaticon, Pngtree, Freepic, Vecteezy, Unsplash, Canva [Free]\n" +
                                 "UI: Kenney\n" +
                                 "Music & SFX: Pixabay [Free]\n" +
                                 "Powered by Unity";


    private void Start()
    {
        // Make sure panels start hidden
        if (settingsPanel) settingsPanel.SetActive(false);
        if (infoPanel)      infoPanel.SetActive(false);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);

        // Assign default text to credits
        if (creditsText != null)
            creditsText.text = creditsInfo;
    }

    // ----- Play -----
    public void OnPlay()
    {
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
    
    // ----- Settings -----
    public void OnOpenSettings() {
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void OnCloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // ----- Credits / Info (if you use it) -----
    public void OnOpenInfo()
    {
        if (infoPanel) infoPanel.SetActive(true);
    }

    public void OnCloseInfo()
    {
        if (infoPanel) infoPanel.SetActive(false);
    }
    
    // ----- How To Play -----
    public void OnOpenHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(true);
    }

    public void OnCloseHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
    }

    // ----- Quit -----
    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
