using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LabValidator : MonoBehaviour
{
    [ContextMenu("Validate Bottles")]
    public void Validate()
    {
        var bottles = FindObjectsOfType<Ingredient>(true);
        foreach (var b in bottles)
        {
            var ok = true;
            if (!b.TryGetComponent<SpriteRenderer>(out _)) { Debug.LogWarning($"{b.name}: missing SpriteRenderer"); ok=false; }
            if (!b.TryGetComponent<Rigidbody2D>(out _))     { Debug.LogWarning($"{b.name}: missing Rigidbody2D"); ok=false; }
            if (!b.TryGetComponent<Collider2D>(out _))      { Debug.LogWarning($"{b.name}: missing Collider2D"); ok=false; }
            if (!b.TryGetComponent<IngredientHover>(out _)) { Debug.LogWarning($"{b.name}: missing IngredientHover"); ok=false; }
            if (!b.data)                                    { Debug.LogWarning($"{b.name}: Ingredient.data is NULL"); ok=false; }
            if (ok) Debug.Log($"{b.name}: OK");
        }
    }
}
