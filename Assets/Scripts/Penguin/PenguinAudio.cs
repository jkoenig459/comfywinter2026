using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PenguinAudio : MonoBehaviour
{
    [Header("Work Sounds (One-Shot)")]
    public AudioClip fishingSound;
    public AudioClip iceBreakingSound;

    [Header("Footsteps")]
    public AudioClip[] footstepSounds;

    [Header("Footstep Settings")]
    [Tooltip("Time between footstep sounds.")]
    public float footstepInterval = 0.35f;

    [Range(0f, 1f)]
    public float footstepVolume = 0.5f;

    [Range(0f, 1f)]
    public float workVolume = 0.6f;

    private AudioSource audioSource;
    private Selectable selectable;
    private PenguinMover mover;

    private float footstepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        selectable = GetComponent<Selectable>();
        if (selectable == null)
            selectable = GetComponentInParent<Selectable>();

        mover = GetComponent<PenguinMover>();
        if (mover == null)
            mover = GetComponentInParent<PenguinMover>();
    }

    private void Update()
    {
        UpdateFootsteps();
    }

    private void UpdateFootsteps()
    {
        if (mover == null) return;
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        bool isWalking = mover.Velocity.sqrMagnitude > 0.01f;
        bool isSelected = IsSelected();

        if (!isWalking || !isSelected)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            PlayRandomFootstep();
            footstepTimer = footstepInterval;
        }
    }

    private bool IsSelected()
    {
        if (SelectionManager.I == null) return false;
        if (selectable == null) return false;

        var selectedObj = SelectionManager.I.SelectedObject;
        if (selectedObj == null) return false;

        return selectedObj == selectable.gameObject ||
               selectedObj.transform.IsChildOf(transform) ||
               transform.IsChildOf(selectedObj.transform);
    }

    private void PlayRandomFootstep()
    {
        if (footstepSounds.Length == 0) return;
        var clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        float volume = footstepVolume * (AudioManager.I != null ? AudioManager.I.penguinSFXVolume : 1f);
        audioSource.PlayOneShot(clip, volume);
    }

    public void PlayFishingSound()
    {
        if (!IsSelected()) return;

        if (fishingSound != null)
        {
            float volume = workVolume * (AudioManager.I != null ? AudioManager.I.penguinSFXVolume : 1f);
            audioSource.PlayOneShot(fishingSound, volume);
        }
    }

    public void PlayIceBreakingSound()
    {
        if (!IsSelected()) return;

        if (iceBreakingSound != null)
        {
            float volume = workVolume * (AudioManager.I != null ? AudioManager.I.penguinSFXVolume : 1f);
            audioSource.PlayOneShot(iceBreakingSound, volume);
        }
    }
}