using UnityEngine;
public class UIAudio : MonoBehaviour
{
    public AudioSource source;
    public AudioClip click;
    public void PlayClick(){ if (source && click) source.PlayOneShot(click); }
}
