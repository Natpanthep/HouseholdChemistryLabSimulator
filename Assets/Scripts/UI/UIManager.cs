using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // Call this from the Home Button
    public void GoToMainMenu()
    {
        // Replace "MainMenu" with your main menu scene name
        SceneManager.LoadScene("MainMenu");
    }

    // Optional: Quit Game button
    public void QuitGame()
    {
        Application.Quit();
    }
}
