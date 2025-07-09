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
        Gaslamp1,
        Gaslamp2,
        Space
    }

    [Header("Audio Clips")]
    public AudioClip Hillage1;
    public AudioClip Hillage2;
    public AudioClip Dust1;
    public AudioClip Dust2;
    public AudioClip Sabbath1;
    public AudioClip Sabbath2;
    public AudioClip Gaslamp1;
    public AudioClip Gaslamp2;
    public AudioClip Space;
    
    private AudioSource audioSource;
    private MusicType currentMusicType = MusicType.Hillage1;
    private MusicType? previousMusicType = null; // stores previously playing


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
        PlayMusic(Hillage1, 0.4f);
    }

    private void PlayMusic(AudioClip clip, float volume)
    {
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = Mathf.Clamp01(volume);
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
            MusicType.Gaslamp1 => "alien",
            MusicType.Gaslamp2 => "alien",
            MusicType.Space => "space",
            _ => "none"
        };

        VisualEventsManager.Instance.ToggleVisualsClientRpc(visualToShow);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership= false)]
    public void CycleMusicServerRpc()
    {
        Debug.Log("Music cycle requested");
        int totalMusicTypes = System.Enum.GetValues(typeof(MusicType)).Length;
        int currentIndex = (int)currentMusicType;
        int nextIndex = currentIndex;
        do
        {
            nextIndex = (nextIndex + 1) % totalMusicTypes;
        }
        while ((MusicType)nextIndex == MusicType.Space); // skipping this track cuz it's used especially for a mode
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
            case MusicType.Hillage1: PlayMusic(Hillage1, 0.3f); break;
            case MusicType.Hillage2: PlayMusic(Hillage2, 0.3f); break;
            case MusicType.Dust1: PlayMusic(Dust1, 0.6f); break;
            case MusicType.Dust2: PlayMusic(Dust2, 0.4f); break;
            case MusicType.Sabbath1: PlayMusic(Sabbath1, 0.4f); break;
            case MusicType.Sabbath2: PlayMusic(Sabbath2, 0.4f); break;
            case MusicType.Gaslamp1: PlayMusic(Gaslamp1, 0.5f); break;
            case MusicType.Gaslamp2: PlayMusic(Gaslamp2, 0.5f); break;
            case MusicType.Space: PlayMusic(Space, 0.4f); break;
        }
        UpdateVisualsForMusic(musicType);
    }
    
    public void ActivateSpaceshipModeMusic()
    {
        if (previousMusicType == null)
        {
            previousMusicType = currentMusicType;
        }
        currentMusicType = MusicType.Space;
        PlayMusic(Space, 0.4f);
        UpdateVisualsForMusic(MusicType.Space);
    }

    public void EndSpaceshipModeMusic()
    {
        if (previousMusicType != null)
        {
            MusicType toRestore = previousMusicType.Value;
            previousMusicType = null;
            currentMusicType = toRestore;

            // Play the previous music with the same volume scheme I had for that track
            switch (toRestore)
            {
                case MusicType.Hillage1: PlayMusic(Hillage1, 0.3f); break;
                case MusicType.Hillage2: PlayMusic(Hillage2, 0.3f); break;
                case MusicType.Dust1: PlayMusic(Dust1, 0.6f); break;
                case MusicType.Dust2: PlayMusic(Dust2, 0.4f); break;
                case MusicType.Sabbath1: PlayMusic(Sabbath1, 0.4f); break;
                case MusicType.Sabbath2: PlayMusic(Sabbath2, 0.4f); break;
                case MusicType.Gaslamp1: PlayMusic(Gaslamp1, 0.4f); break;
                case MusicType.Gaslamp2: PlayMusic(Gaslamp2, 0.5f); break;
                case MusicType.Space: PlayMusic(Space, 0.4f); break;  // Idk we'll see...
            }

            UpdateVisualsForMusic(toRestore);
        }
    }

    public MusicType GetCurrentMusicType()
    {
        return currentMusicType;
    }
}
