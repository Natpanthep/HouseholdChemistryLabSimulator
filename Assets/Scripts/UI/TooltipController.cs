using UnityEngine;
using TMPro;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance;

    [SerializeField] private RectTransform panel; // TooltipRoot
    [SerializeField] private TMP_Text text;       // TooltipText
    [SerializeField] private Vector2 offset = new Vector2(16f, -16f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
        // DontDestroyOnLoad(gameObject); 
    }

    public void Show(string message)
    {
        if (!panel || !text) return;
        text.text = message;
        panel.gameObject.SetActive(true);
        UpdatePosition();
    }

    public void Hide()
    {
        if (panel) panel.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (panel && panel.gameObject.activeSelf) UpdatePosition();
    }

    void UpdatePosition()
{
    if (!panel) return;

    Vector2 m = Input.mousePosition;
    float pad = 24f; // distance from cursor

    // Choose a quadrant AWAY from the cursor so it never sits under the mouse
    Vector2 dir = new Vector2(
        m.x < Screen.width  * 0.5f ? 1f : -1f,
        m.y < Screen.height * 0.5f ? -1f : 1f
    );

    // Set pivot to match the quadrant (keeps panel fully on-screen)
    panel.pivot = new Vector2(dir.x > 0 ? 0f : 1f, dir.y > 0 ? 1f : 0f);

    Vector2 pos = m + new Vector2(dir.x * pad, dir.y * pad);

    // Clamp so it never goes off screen
    Vector2 size = panel.sizeDelta;
    pos.x = Mathf.Clamp(pos.x, 0f, Screen.width  - size.x);
    pos.y = Mathf.Clamp(pos.y, 0f, Screen.height - size.y);

    panel.position = pos;
}

}
