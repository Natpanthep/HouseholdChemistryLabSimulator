using UnityEngine;

public class AudioSanity : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[Audio] vol={AudioListener.volume}, paused={AudioListener.pause}");
        var src = FindObjectOfType<AudioSource>();
        if (src != null)
        {
            src.mute = false;
            src.volume = 1f;
            if (!src.isPlaying) src.Play();
            Debug.Log("[Audio] Found AudioSource and asked it to Play().");
        }
        else
        {
            Debug.LogWarning("[Audio] No AudioSource found in scene.");
        }
    }
}
