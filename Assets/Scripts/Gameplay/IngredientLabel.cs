using UnityEngine;
using TMPro;

[ExecuteAlways]
public class IngredientLabel : MonoBehaviour
{
    [SerializeField] private Ingredient source;   // the bottle
    [SerializeField] private TMP_Text tmp;        // the label text

    void Reset()
    {
        if (!source) source = GetComponentInParent<Ingredient>();
        if (!tmp)     tmp    = GetComponent<TMP_Text>();           // for TMP UI
        if (!tmp)     tmp    = GetComponentInChildren<TMP_Text>(); // just in case
        UpdateText();
    }

    void OnEnable()   => UpdateText();
#if UNITY_EDITOR
    void OnValidate() => UpdateText();
#endif

    public void UpdateText()
    {
        if (!source || !source.data || !tmp) return;
        tmp.text = string.IsNullOrEmpty(source.data.displayName)
            ? source.data.id
            : source.data.displayName;
    }
}
