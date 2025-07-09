using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;
    public enum MusicType
    {
        Hillage1,
        Hillage2,
        Dust1,
        Dust2,
        Sabbath1,
        Sabbath2,
    }

    [Header("Audio Clips")]
    public AudioClip Hillage1;
    public AudioClip Hillage2;
    public AudioClip Dust1;
    public AudioClip Dust2;
    public AudioClip Sabbath1;
    public AudioClip Sabbath2;
    
    private AudioSource audioSource;
    private MusicType currentMusicType = MusicType.Hillage1;

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
        PlayMusic(Hillage1);
    }

    private void PlayMusic(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        audioSource.Play();
    }
    
    private void UpdateVisualsForMusic(MusicType musicType)
    {
        if (!VisualEventsManager.Instance) return;

        string visualToShow = musicType switch
        {
            MusicType.Hillage1 => "circles",
            MusicType.Hillage2 => "circles",
            MusicType.Dust1 => "city",
            MusicType.Dust2 => "city",
            MusicType.Sabbath1 => "horror",
            MusicType.Sabbath2 => "horror",
            _ => "none"
        };

        VisualEventsManager.Instance.ToggleVisualsClientRpc(visualToShow);
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
            case MusicType.Hillage1: PlayMusic(Hillage1); break;
            case MusicType.Hillage2: PlayMusic(Hillage2); break;
            case MusicType.Dust1: PlayMusic(Dust1); break;
            case MusicType.Dust2: PlayMusic(Dust2); break;
            case MusicType.Sabbath1: PlayMusic(Sabbath1); break;
            case MusicType.Sabbath2: PlayMusic(Sabbath2); break;
        }
        UpdateVisualsForMusic(musicType);
    }
    
    public MusicType GetCurrentMusicType()
    {
        return currentMusicType;
    }
}
