using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Work Sounds")]
    public AudioClip fishingLoop;
    public AudioClip iceBreakingLoop;

    [Header("Pile Sounds")]
    public AudioClip[] pileFishSounds;
    public AudioClip[] pileIceSounds;

    [Header("Building")]
    public AudioClip placeIgloo;

    [Header("Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource sfxSource;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayPileFish()
    {
        if (pileFishSounds == null || pileFishSounds.Length == 0) return;
        var clip = pileFishSounds[Random.Range(0, pileFishSounds.Length)];
        PlayOneShot(clip);
    }

    public void PlayPileIce()
    {
        if (pileIceSounds == null || pileIceSounds.Length == 0) return;
        var clip = pileIceSounds[Random.Range(0, pileIceSounds.Length)];
        PlayOneShot(clip);
    }

    public void PlayPlaceIgloo()
    {
        PlayOneShot(placeIgloo);
    }
}