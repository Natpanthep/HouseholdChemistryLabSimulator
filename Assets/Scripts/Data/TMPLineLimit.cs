using TMPro;
using UnityEngine;

[ExecuteAlways]
public class TMPLineLimit : MonoBehaviour
{
    public int maxVisibleLines = 2;
    public bool applyInEditor = true;

    TMP_Text _tmp;

    void OnEnable()      { _tmp = GetComponent<TMP_Text>(); Apply(); }
    void OnValidate()    { if (applyInEditor) Apply(); }
    public void Apply()  { if (_tmp) _tmp.maxVisibleLines = Mathf.Max(1, maxVisibleLines); }
}
