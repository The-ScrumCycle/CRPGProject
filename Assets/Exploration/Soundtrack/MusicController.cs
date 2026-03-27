using UnityEngine;

public class MusicController : MonoBehaviour
{
    private static MusicController instance;

    public static MusicController Instance => instance;

    [Header("Music Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip currentClip;
    [SerializeField] private float volume = 1f;

    [Header("Songs Settings")]
    [SerializeField] private AudioClip explorationMusic;
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioClip sailingMusic;
    [SerializeField] private AudioClip ending;



    void Awake()
    {
        // singleton should not be destroyed when loading new scenes
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);


        audioSource = GetComponent<AudioSource>();
        audioSource.volume = volume;
    }

    public AudioClip GetMusic()
    {
        return audioSource.clip;
    }

    public void SetMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public AudioClip GetExplorationMusic()
    {
        return explorationMusic;
    }

    public AudioClip GetCombatMusic()
    {
        return battleMusic;
    }

    public AudioClip GetSailingMusic()
    {
        return sailingMusic;
    }

    public AudioClip GetEndingMusic()
    {
        return ending;
    }
}
