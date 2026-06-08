using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip flipClip;
    public AudioClip goodMatchClip;
    public AudioClip badMatchClip;
    public AudioClip godRaysClip;
    public AudioClip incrementClip;
    public AudioClip omnomClip;
    public AudioClip rawrClip;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayFlip() => Play(flipClip);
    public void PlayGoodMatch() => Play(goodMatchClip);
    public void PlayBadMatch() => Play(badMatchClip);
    public void PlayGodRays() => Play(godRaysClip);
    public void PlayIncrement() => Play(incrementClip);
    public void PlayOmnom() => Play(omnomClip);
    public void PlayRawr() => Play(rawrClip);

    private void Play(AudioClip clip)
    {
        if (clip != null)
        {
            
            _audioSource.PlayOneShot(clip);
        }
    }
}
