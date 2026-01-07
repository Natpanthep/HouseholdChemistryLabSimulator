// Assets/Scripts/Gameplay/Ingredient.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class Ingredient : MonoBehaviour
{
    public IngredientSO data;
    public bool consumed;

    private SpriteRenderer sr;

    private void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        Apply();
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        sr = GetComponent<SpriteRenderer>();
        Apply(); // updates in Editor when you tweak the SO
    }
#endif

    [ContextMenu("Reapply From SO")]
    public void Apply()
    {
        if (!sr) return;

        if (data != null)
        {
            // If the SO has a sprite, use it (otherwise keep whatever the renderer has)
            if (data.sprite) sr.sprite = data.sprite;

            // Apply tint (make sure alpha is 1)
            Color c = data.color;
            if (c.a <= 0.001f) c.a = 1f;
            sr.color = c;

            // Nice name in Hierarchy
            name = string.IsNullOrEmpty(data.displayName)
                 ? $"Ingredient_{data.id}"
                 : $"Bottle_{data.displayName}";
        }
    }
}
