using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource backgroundAudio; // referensi ke Audio Source
    public Slider backgroundSlider;     // referensi ke Slider Backsound

    void Start()
    {
        // Pastikan slider langsung menyesuaikan dengan volume awal
        if (backgroundAudio != null && backgroundSlider != null)
        {
            backgroundSlider.value = backgroundAudio.volume;
            backgroundSlider.onValueChanged.AddListener(SetBackgroundVolume);
        }
    }

    public void SetBackgroundVolume(float volume)
    {
        if (backgroundAudio != null)
        {
            backgroundAudio.volume = volume;
        }
    }
}
