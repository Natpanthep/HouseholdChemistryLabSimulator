// Assets/Scripts/UI/LabUI.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class LabUI : MonoBehaviour
{
    [Header("Recipe Book")]
    public GameObject recipeBookPanel;   // assign: Canvas/RecipeBookPanel
    public GameObject reactionText;      // assign: the text object above the beaker

    private void Awake()
    {
        // Safety net: auto-find RecipeBookPanel if not wired
        if (!recipeBookPanel)
        {
            var go = GameObject.Find("RecipeBookPanel");
            if (go) recipeBookPanel = go;
        }
    }

    // ----------------- RESET / QUIT -----------------

    // Reset everything by reloading the active scene
    public void ResetScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    // ----------------- RECIPE BOOK -----------------

    // Old generic toggle, still safe to use if wired
    public void ToggleRecipeBook()
    {
        if (recipeBookPanel)
            recipeBookPanel.SetActive(!recipeBookPanel.activeSelf);
        else
            Debug.LogWarning("RecipeBookPanel is not assigned on LabUI.");
    }

    public void OpenRecipeBook()
    {
        if (recipeBookPanel) recipeBookPanel.SetActive(true);
        if (reactionText)   reactionText.SetActive(false);
    }

    public void CloseRecipeBook()
    {
        if (recipeBookPanel) recipeBookPanel.SetActive(false);
        if (reactionText)   reactionText.SetActive(true);
    }

    // ----------------- INGREDIENT CLEANUP (optional) -----------------

    // This only deletes loose Ingredient objects that are not yet consumed.
    // You can call this from a Trash button if you want.
    public void ClearLooseIngredients()
    {
        var all = FindObjectsOfType<Ingredient>();
        foreach (var ing in all)
        {
            if (!ing.consumed)
                // Destroy(ing.gameObject);

                // Do NOT destroy — just hide it
                ing.gameObject.SetActive(false);

                // Also reset its state
                ing.consumed = true;
        }
    }

    // ----------------- ESC CLOSE RECIPE BOOK -----------------

    private void Update()
    {
        if (recipeBookPanel && recipeBookPanel.activeSelf &&
            (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.BackQuote)))
        {
            CloseRecipeBook();
        }
    }
}

// // Assets/Scripts/UI/LabUI.cs
// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class LabUI : MonoBehaviour
// {
//     public GameObject recipeBookPanel; // <-- this is the slot you will assign

//     public void ResetScene()
//     {
//         SceneManager.LoadScene(SceneManager.GetActiveScene().name);
//     }

//     public void QuitGame()
//     {
//         #if UNITY_EDITOR
//                 UnityEditor.EditorApplication.isPlaying = false;
//         #else
//                 Application.Quit();
//         #endif
//     }

//     public void ToggleRecipeBook()
//     {
//         if (recipeBookPanel)
//             recipeBookPanel.SetActive(!recipeBookPanel.activeSelf);
//         else
//             Debug.LogWarning("RecipeBookPanel is not assigned on LabUI.");
//     }

//     public void ClearLooseIngredients()
//     {
//         var all = FindObjectsOfType<Ingredient>();
//         foreach (var ing in all)
//         {
//             if (!ing.consumed) Destroy(ing.gameObject);
//         }
//     }

//     // Safety net: auto-find by name if not assigned
//     private void Awake()
//     {
//         if (!recipeBookPanel)
//         {
//             var go = GameObject.Find("RecipeBookPanel");
//             if (go) recipeBookPanel = go;
//         }
//     }
//     public GameObject reactionText;

//     public void OpenRecipeBook()
//     {
//         if (recipeBookPanel) recipeBookPanel.SetActive(true);
//         if (reactionText) reactionText.SetActive(false);
//     }

//     public void CloseRecipeBook()
//     {
//         if (recipeBookPanel) recipeBookPanel.SetActive(false);
//         if (reactionText) reactionText.SetActive(true);
//     }

//     /* public void OpenRecipeBook()
//     {
//         if (recipeBookPanel) recipeBookPanel.SetActive(true);
//     }

//     public void CloseRecipeBook()
//     {
//         if (recipeBookPanel) recipeBookPanel.SetActive(false);
//     } */

//     private void Update()
//     {
//         if (recipeBookPanel && recipeBookPanel.activeSelf &&
//             (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.BackQuote)))
//         {
//             CloseRecipeBook();
//         }
//     }

// }
