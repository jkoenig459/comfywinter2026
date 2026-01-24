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
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float penguinSFXVolume = 1f;
    [Range(0f, 1f)] public float resourceSFXVolume = 1f;

    private AudioSource sfxSource;
    private AudioSource musicSource;

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

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;

        LoadVolumeSettings();
    }

    private void LoadVolumeSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        penguinSFXVolume = PlayerPrefs.GetFloat("PenguinSFXVolume", 1f);
        resourceSFXVolume = PlayerPrefs.GetFloat("ResourceSFXVolume", 1f);

        UpdateMusicVolume();
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("PenguinSFXVolume", penguinSFXVolume);
        PlayerPrefs.SetFloat("ResourceSFXVolume", resourceSFXVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
        SaveVolumeSettings();
    }

    public void SetPenguinSFXVolume(float volume)
    {
        penguinSFXVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
    }

    public void SetResourceSFXVolume(float volume)
    {
        resourceSFXVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
    }

    private void UpdateMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    private void PlayPenguinSFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, penguinSFXVolume);
    }

    private void PlayResourceSFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, resourceSFXVolume);
    }

    public void PlayPileFish()
    {
        if (pileFishSounds == null || pileFishSounds.Length == 0) return;
        var clip = pileFishSounds[Random.Range(0, pileFishSounds.Length)];
        PlayResourceSFX(clip);
    }

    public void PlayPileIce()
    {
        if (pileIceSounds == null || pileIceSounds.Length == 0) return;
        var clip = pileIceSounds[Random.Range(0, pileIceSounds.Length)];
        PlayResourceSFX(clip);
    }

    public void PlayPlaceIgloo()
    {
        PlayResourceSFX(placeIgloo);
    }
}