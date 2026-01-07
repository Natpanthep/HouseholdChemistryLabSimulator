using UnityEngine;
public class HoverDebug : MonoBehaviour {
    void OnMouseEnter(){ Debug.Log("ENTER " + name); }
    void OnMouseExit(){ Debug.Log("EXIT " + name); }
}
