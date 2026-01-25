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
    public AudioClip iglooUpgrade;

    [Header("Penguin Sounds")]
    public AudioClip[] penguinChirps;
    public AudioClip penguinGather;

    [Header("Resource Sounds")]
    public AudioClip pebblePickup01;
    public AudioClip pebblePickup02;

    [Header("UI Sounds")]
    public AudioClip uiButtonClick;
    public AudioClip startButtonClick;

    [Header("Music")]
    public AudioClip musicLoop1;
    public AudioClip musicLoop2;
    public AudioClip musicLoop3;

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
        DontDestroyOnLoad(gameObject);

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

    public void PlayIglooUpgrade()
    {
        PlayResourceSFX(iglooUpgrade);
    }

    public void PlayPenguinChirp()
    {
        if (penguinChirps == null || penguinChirps.Length == 0) return;
        var clip = penguinChirps[Random.Range(0, penguinChirps.Length)];
        PlayPenguinSFX(clip);
    }

    public void PlayPenguinGather()
    {
        PlayPenguinSFX(penguinGather);
    }

    public void PlayPebblePickup()
    {
        // Randomly play one of the two pebble sounds
        AudioClip clip = Random.Range(0, 2) == 0 ? pebblePickup01 : pebblePickup02;
        PlayResourceSFX(clip);
    }

    public void PlayUIButtonClick()
    {
        if (uiButtonClick == null) return;
        sfxSource.PlayOneShot(uiButtonClick, musicVolume);
    }

    public void PlayStartButton()
    {
        if (startButtonClick == null) return;
        sfxSource.PlayOneShot(startButtonClick, musicVolume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayMusicLoop1()
    {
        PlayMusic(musicLoop1);
    }

    public void PlayMusicLoop2()
    {
        PlayMusic(musicLoop2);
    }

    public void PlayMusicLoop3()
    {
        PlayMusic(musicLoop3);
    }
}