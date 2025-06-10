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
        Music7,
        Music8,
    }

    [Header("Selectable Clips")]
    public AudioClip music1;
    public AudioClip music2;
    public AudioClip music3;
    public AudioClip music4;
    public AudioClip music5;
    public AudioClip music6;
    public AudioClip music7;
    public AudioClip music8;
    
    private AudioSource audioSource;

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

    private void PlayMusic(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        audioSource.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMusicChangeServerRpc(MusicType musicType, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (ScoreManager.instance.TrySpendPoints(clientId, 3))
        {
            SwitchMusicClientRPC(musicType);
        }
        else
        {
            Debug.Log($"Client {clientId} tried changing music but doesnt have enough points");
        }
    }

    [ClientRpc]
    public void SwitchMusicClientRPC(MusicType musicType)
    {
        switch (musicType)
        {
            case MusicType.Music1: PlayMusic(music1); break;
            case MusicType.Music2: PlayMusic(music2); break;
            case MusicType.Music3: PlayMusic(music3); break;
            case MusicType.Music4: PlayMusic(music4); break;
            case MusicType.Music5: PlayMusic(music5); break;
            case MusicType.Music6: PlayMusic(music6); break;
            case MusicType.Music7: PlayMusic(music7); break;
            case MusicType.Music8: PlayMusic(music8); break;
        }
            
    }
}
