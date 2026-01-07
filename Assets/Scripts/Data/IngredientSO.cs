using UnityEngine;

[CreateAssetMenu(menuName = "Lab/Ingredient")]
public class IngredientSO : ScriptableObject
{
    public string id;             // e.g. "vinegar"
    public string displayName;    // "Vinegar (Acetic Acid)"
    public Sprite sprite;         // optional: auto-assign
    public Color color = Color.white;
}
