using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource sfxSource;
    public Slider volSlider;
    float vol;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (volSlider != null)
            volSlider.value = AudioListener.volume;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void ChangeVolume()
    {
        if (volSlider != null)
        {
            AudioListener.volume = volSlider.value;
            vol = volSlider.value;
        }
    }


    public void MuteVolume()
    {
        AudioListener.volume = 0;
    }

    public void OpenVolume()
    {
        AudioListener.volume = vol;
    }
}
