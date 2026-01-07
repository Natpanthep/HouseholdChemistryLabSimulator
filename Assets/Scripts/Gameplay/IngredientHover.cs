using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class IngredientHover : MonoBehaviour
{
    private Ingredient source;

    void Awake() { source = GetComponent<Ingredient>(); }

    void OnMouseEnter()
    {
        if (source && source.data)
            TooltipController.Instance?.Show(source.data.displayName);
    }

    void OnMouseExit()
    {
        TooltipController.Instance?.Hide();
    }
}
