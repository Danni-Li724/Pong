using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;
    public enum MusicType
    {
        Music1,
        Music2,
        Music3,
        Music4,
        Music5,
        Music6,
    }

    [Header("Selectable Clips")]
    public AudioClip music1;
    public AudioClip music2;
    public AudioClip music3;
    public AudioClip music4;
    public AudioClip music5;
    public AudioClip music6;
    
    private AudioSource audioSource;
    private MusicType currentMusicType = MusicType.Music1;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayMusic(music1);
    }

    private void PlayMusic(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        audioSource.Play();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership= false)]
    public void CycleMusicServerRpc()
    {
        Debug.Log("Music cycle requested");
        // Cycle to next music track
        int currentIndex = (int)currentMusicType;
        int nextIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(MusicType)).Length;
        currentMusicType = (MusicType)nextIndex;
        
        // Sync to all clients
        SwitchMusicClientRPC(currentMusicType);
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable, RequireOwnership= false)]
    public void SwitchMusicClientRPC(MusicType musicType)
    {
        if (!IsClient) return;
        currentMusicType = musicType;
        
        switch (musicType)
        {
            case MusicType.Music1: PlayMusic(music1); break;
            case MusicType.Music2: PlayMusic(music2); break;
            case MusicType.Music3: PlayMusic(music3); break;
            case MusicType.Music4: PlayMusic(music4); break;
            case MusicType.Music5: PlayMusic(music5); break;
            case MusicType.Music6: PlayMusic(music6); break;
        }
    }
    
    public MusicType GetCurrentMusicType()
    {
        return currentMusicType;
    }
}
