using UnityEngine;
using System.Collections; 

[RequireComponent(typeof(AudioSource))]
public class ZombieAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] idleSounds;         // Sounds played when idle (groans, shuffles)
    [SerializeField] private AudioClip[] agroSounds;         // Sounds played when becoming aggressive (roars, screams)
    [SerializeField] private AudioClip[] attackSounds;       // Sounds played during an attack
    [SerializeField] private AudioClip[] deathSounds;        // Sounds played upon death
    [SerializeField] private AudioClip[] stunSounds;         // Sounds played when stunned

    [Header("Sound Settings")]
    [SerializeField] private float minIdleSoundInterval = 5f; // Minimum time between idle sounds
    [SerializeField] private float maxIdleSoundInterval = 10f; // Maximum time between idle sounds

    private AudioSource audioSource;
    private MobAI mobAI; // Reference to the MobAI component
    private HealthSystem healthSystem; // Reference to the HealthSystem component

    private float nextIdleSoundTime; // When the next idle sound can play
    private bool hasPlayedAgroSoundSinceLastIdle = false; // Flag to prevent spamming agro sound
    private bool hasPlayedDeathSound = false; // Flag to prevent playing death sound multiple times

    // Removed redundant ZombieState enum and currentState variable.
    // The audio logic will now rely on the MobAI's state and direct method calls.


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        mobAI = GetComponent<MobAI>();
        healthSystem = GetComponent<HealthSystem>(); // Get HealthSystem component

        if (mobAI == null)
        {
            Debug.LogError("MobAI component not found on " + gameObject.name + "! ZombieAudio will be disabled.", this);
            enabled = false; // Disable this script if MobAI is missing, as it relies heavily on it
            return;
        }
        if (healthSystem == null)
        {
            Debug.LogWarning("HealthSystem component not found on " + gameObject.name + ". Death sound might not play correctly.", this);
            // We don't disable the script entirely, as other sounds might still be needed
        }

        // Optional: Subscribe to HealthSystem's damage event to play hurt sounds (if you have them)
        // if (healthSystem != null) healthSystem.OnTakeDamage += PlayHurtSound;
    }

    void Start()
    {
        SetNextIdleSoundTime(); // Schedule the first idle sound
    }

    void Update()
    {
        // If MobAI script is disabled (e.g., on death), handle death sound explicitly
        // Note: The MobAI script itself disables its NavMeshAgent and Rigidbody on death.
        // It's better if the MobAI *calls* PlayDeathSound when it dies, but this check can work as a fallback.
        if (!mobAI.enabled && healthSystem != null && healthSystem.IsDead() && !hasPlayedDeathSound)
        {
            PlayDeathSoundInternal(); // Play death sound if mobAI is disabled and mob is dead
            return; // Stop updating if dead or MobAI is disabled
        }
        
        // If MobAI is null (error case handled in Awake), stop updating
        if (mobAI == null) return;

        // Handle idle sounds based on timer and if the mob is currently considered "idle" by MobAI
        // Accessing MobAI's state directly requires MobAI to expose it (e.g., make MobState public)
        // A simpler approach is to assume idle sounds should play when not moving and not recently agroed/attacked.
        HandleIdleSounds();
    }

    // Handles playing idle sounds based on the timer and mob state
    private void HandleIdleSounds()
    {
        // Only play idle sounds if it's time, the mob is not dead, and MobAI's NavMeshAgent is effectively stopped
        // (Checking navMeshAgent.velocity.sqrMagnitude is a common way to see if it's moving)
        // Add a small threshold (e.g., 0.1f) because NavMeshAgent velocity can be slightly > 0 even when stopped.
        if (Time.time >= nextIdleSoundTime && (healthSystem == null || !healthSystem.IsDead()) && mobAI.navMeshAgent != null && mobAI.navMeshAgent.velocity.sqrMagnitude < 0.1f)
        {
            PlayRandomSound(idleSounds);
            SetNextIdleSoundTime(); // Schedule the next idle sound
            hasPlayedAgroSoundSinceLastIdle = false; // Reset agro flag when an idle sound plays
        }
    }

    // Sets the timer for the next idle sound
    private void SetNextIdleSoundTime()
    {
        nextIdleSoundTime = Time.time + Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
    }

    // Plays a random clip from a given array
    private void PlayRandomSound(AudioClip[] clips, float volume = 1.0f)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }

    // --- Methods called by MobAI or other scripts to trigger specific sounds ---

    // Called when the zombie becomes aggressive (transition from Idle to Chase/Attack)
    public void PlayAgroSound()
    {
        // Play the agro sound only if it hasn't been played since the last idle sound
        if (!hasPlayedAgroSoundSinceLastIdle)
        {
            PlayRandomSound(agroSounds);
            hasPlayedAgroSoundSinceLastIdle = true; // Set flag
        }
        // Note: We don't reset hasPlayedAgroSoundSinceLastIdle here. It's reset in HandleIdleSounds.
    }

    // Called when the zombie performs an attack
    public void PlayAttackSound()
    {
        if (healthSystem != null && healthSystem.IsDead()) return; // Don't play attack sound if dead
        PlayRandomSound(attackSounds);
    }

    // Called when the zombie dies
    public void PlayDeathSound()
    {
        PlayDeathSoundInternal(); // Call internal method to handle logic
    }

    // Internal logic for playing death sound
    private void PlayDeathSoundInternal()
    {
        if (!hasPlayedDeathSound) // Ensure it only plays once
        {
            // Stop any currently playing sounds before playing the death sound
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            PlayRandomSound(deathSounds);
            hasPlayedDeathSound = true; // Set flag
        }
    }

    // Called when the zombie is stunned
    public void PlayStunSound()
    {
        if (healthSystem != null && healthSystem.IsDead()) return; // Don't play stun sound if dead
        PlayRandomSound(stunSounds);
        // PlayOneShot does not need to be stopped explicitly later.
    }

    // Removed StopStunSound as it's unnecessary for PlayOneShot

    // Removed commented-out methods related to getting MobAI properties, as they are not used in the current logic.
}