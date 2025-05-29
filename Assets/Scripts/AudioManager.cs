using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;
    public AudioClip backgroundMusic;
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
            audioSource = GetComponent<AudioSource>();
           
            PlayBackgroundMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void PlayBackgroundMusic()
    {
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        audioSource.Play();
    }
}
