using UnityEngine;
using System.Collections; // „D„|„‘ IEnumerator („u„ƒ„|„y „„€„~„p„t„€„q„‘„„„ƒ„‘ „x„p„t„u„‚„w„{„y)

[RequireComponent(typeof(AudioSource))]
public class ZombieAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] idleSounds;         // „H„r„…„{„y „q„u„x„t„u„z„ƒ„„„r„y„‘ („‚„„‰„p„~„y„u, „ƒ„„„€„~„)
    [SerializeField] private AudioClip[] agroSounds;         // „H„r„…„{ „„‚„y „€„q„~„p„‚„…„w„u„~„y„y „y„s„‚„€„{„p („p„s„‚„u„ƒ„ƒ„y„r„~„„z „‚„„{)
    [SerializeField] private AudioClip[] attackSounds;       // „H„r„…„{„y „p„„„p„{„y
    [SerializeField] private AudioClip[] deathSounds;        // „H„r„…„{„y „ƒ„}„u„‚„„„y
    [SerializeField] private AudioClip[] stunSounds;         // „H„r„…„{„y „„‚„y „€„s„|„…„Š„u„~„y„y

    [Header("Sound Settings")]
    [SerializeField] private float minIdleSoundInterval = 5f; // „M„y„~„y„}„p„|„„~„„z „y„~„„„u„‚„r„p„| „t„|„‘ „x„r„…„{„€„r „q„u„x„t„u„z„ƒ„„„r„y„‘
    [SerializeField] private float maxIdleSoundInterval = 10f; // „M„p„{„ƒ„y„}„p„|„„~„„z „y„~„„„u„‚„r„p„|

    private AudioSource audioSource;
    private MobAI mobAI; // „R„ƒ„„|„{„p „~„p „€„ƒ„~„€„r„~„€„z „ƒ„{„‚„y„„„ AI
    private HealthSystem healthSystem; // „R„ƒ„„|„{„p „~„p „ƒ„y„ƒ„„„u„}„… „x„t„€„‚„€„r„„‘

    private float nextIdleSoundTime;
    private bool hasPlayedAgroSoundSinceLastIdle = false;
    private bool hasPlayedDeathSound = false;

    // „R„€„ƒ„„„€„‘„~„y„u „t„|„‘ „€„„„ƒ„|„u„w„y„r„p„~„y„‘, „{„€„s„t„p „~„…„w„~„€ „„‚„€„y„s„‚„p„„„ „x„r„…„{ „q„u„x„t„u„z„ƒ„„„r„y„‘
    private enum ZombieState
    {
        Idle,
        Chasing,
        Attacking,
        Stunned, // „E„ƒ„|„y „… „„„u„q„‘ „u„ƒ„„„ „}„u„‡„p„~„y„{„p „€„s„|„…„Š„u„~„y„‘
        Dead
    }
    private ZombieState currentState = ZombieState.Idle;


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        mobAI = GetComponent<MobAI>();
        healthSystem = GetComponent<HealthSystem>(); // „P„€„|„…„‰„p„u„} HealthSystem

        if (mobAI == null)
        {
            Debug.LogError("MobAI component not found on this GameObject!", this);
            enabled = false; // „O„„„{„|„„‰„p„u„} „ƒ„{„‚„y„„„, „u„ƒ„|„y „~„u„„ MobAI
            return;
        }
        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem component not found on this GameObject!", this);
            // „M„€„w„~„€ „~„u „€„„„{„|„„‰„p„„„, „~„€ „~„u„{„€„„„€„‚„„u „†„…„~„{„ˆ„y„y „~„u „q„…„t„…„„ „‚„p„q„€„„„p„„„ „{„€„‚„‚„u„{„„„~„€
        }

        // „O„„ˆ„y„€„~„p„|„„~„€: „„€„t„„y„ƒ„p„„„„ƒ„‘ „~„p „ƒ„€„q„„„„y„u „„€„|„…„‰„u„~„y„‘ „…„‚„€„~„p, „u„ƒ„|„y „€„~„€ „u„ƒ„„„ „r HealthSystem
        // if (healthSystem != null) healthSystem.OnTakeDamage += PlayHurtSound;
    }

    void Start()
    {
        SetNextIdleSoundTime();
    }

    void Update()
    {
        if (mobAI == null || !mobAI.enabled) // „E„ƒ„|„y MobAI „€„„„{„|„„‰„u„~ („~„p„„‚„y„}„u„‚, „„€„ƒ„|„u „ƒ„}„u„‚„„„y)
        {
            if (healthSystem != null && healthSystem.IsDead() && !hasPlayedDeathSound)
            {
                // „^„„„€„„ „q„|„€„{ „~„p „ƒ„|„…„‰„p„z, „u„ƒ„|„y MobAI „€„„„{„|„„‰„p„u„„„ƒ„‘ „t„€ „r„„x„€„r„p PlayDeathSound
                PlayDeathSoundInternal();
            }
            return;
        }

        // „O„q„~„€„r„|„‘„u„} „ƒ„€„ƒ„„„€„‘„~„y„u „x„€„}„q„y „~„p „€„ƒ„~„€„r„u MobAI
        UpdateZombieState();

        // „O„q„‚„p„q„€„„„{„p „x„r„…„{„€„r „r „x„p„r„y„ƒ„y„}„€„ƒ„„„y „€„„ „ƒ„€„ƒ„„„€„‘„~„y„‘
        HandleIdleSounds();
    }

    private void UpdateZombieState()
    {
        if (healthSystem != null && healthSystem.IsDead())
        {
            currentState = ZombieState.Dead;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player"); // „N„u „€„‰„u„~„ „„†„†„u„{„„„y„r„~„€ „r Update, „~„€ „t„|„‘ „„‚„y„}„u„‚„p
        if (player == null)
        {
            currentState = ZombieState.Idle; // „E„ƒ„|„y „y„s„‚„€„{„p „~„u„„, „x„€„}„q„y „q„u„x„t„u„z„ƒ„„„r„…„u„„
            return;
        }
    }


    private void HandleIdleSounds()
    {
        if (currentState == ZombieState.Idle && Time.time >= nextIdleSoundTime)
        {
            PlayRandomSound(idleSounds);
            SetNextIdleSoundTime();
            hasPlayedAgroSoundSinceLastIdle = false; // „R„q„‚„p„ƒ„„r„p„u„} „†„|„p„s „p„s„‚„€, „{„€„s„t„p „ƒ„~„€„r„p „q„u„x„t„u„z„ƒ„„„r„…„u„„
        }
    }

    private void SetNextIdleSoundTime()
    {
        nextIdleSoundTime = Time.time + Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
    }

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

    // --- „P„…„q„|„y„‰„~„„u „}„u„„„€„t„ „t„|„‘ „r„„x„€„r„p „y„x MobAI „y„|„y „t„‚„…„s„y„‡ „ƒ„{„‚„y„„„„€„r ---

    // „B„„x„„r„p„u„„„ƒ„‘, „{„€„s„t„p „x„€„}„q„y „~„p„‰„y„~„p„u„„ „„‚„u„ƒ„|„u„t„€„r„p„~„y„u („„u„‚„u„‡„€„t „y„x Idle „r Agro/Chase)
    public void PlayAgroSound()
    {
        if (!hasPlayedAgroSoundSinceLastIdle && currentState != ZombieState.Idle) // „I„s„‚„p„u„} „„„€„|„„{„€ „u„ƒ„|„y „~„u „y„s„‚„p„|„y „ƒ „„€„ƒ„|„u„t„~„u„s„€ Idle
        {
            PlayRandomSound(agroSounds);
            hasPlayedAgroSoundSinceLastIdle = true;
        }
    }

    // „B„„x„„r„p„u„„„ƒ„‘ „„‚„y „p„„„p„{„u
    public void PlayAttackSound()
    {
        if (currentState == ZombieState.Dead) return; // „N„u „p„„„p„{„€„r„p„„„, „u„ƒ„|„y „}„u„‚„„„r
        PlayRandomSound(attackSounds);
    }


    // „B„„x„„r„p„u„„„ƒ„‘ „„‚„y „ƒ„}„u„‚„„„y
    public void PlayDeathSound()
    {
        PlayDeathSoundInternal();
    }

    private void PlayDeathSoundInternal()
    {
        if (!hasPlayedDeathSound)
        {
            // „O„ƒ„„„p„~„€„r„y„„„ „„„u„{„…„‹„y„u „x„r„…„{„y, „u„ƒ„|„y „~„…„w„~„€ („€„ƒ„€„q„u„~„~„€ „u„ƒ„|„y „„„„€ „q„„| „|„…„)
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            PlayRandomSound(deathSounds);
            hasPlayedDeathSound = true;
            currentState = ZombieState.Dead; // „T„q„u„t„y„}„ƒ„‘, „‰„„„€ „ƒ„€„ƒ„„„€„‘„~„y„u „€„q„~„€„r„|„u„~„€
        }
    }

    // „B„„x„„r„p„u„„„ƒ„‘ „„‚„y „€„s„|„…„Š„u„~„y„y („u„ƒ„|„y „u„ƒ„„„ „„„p„{„p„‘ „}„u„‡„p„~„y„{„p)
    public void PlayStunSound()
    {
        if (currentState == ZombieState.Dead) return;
        PlayRandomSound(stunSounds);
        // „M„€„w„~„€ „t„€„q„p„r„y„„„ „|„€„s„y„{„… „„‚„u„‚„„r„p„~„y„‘ „t„‚„…„s„y„‡ „x„r„…„{„€„r
        if (audioSource.isPlaying)
        {
            // audioSource.Stop(); // „Q„p„ƒ„{„€„}„}„u„~„„„y„‚„€„r„p„„„, „u„ƒ„|„y „x„r„…„{ „€„s„|„…„Š„u„~„y„‘ „t„€„|„w„u„~ „„‚„u„‚„„r„p„„„ „t„‚„…„s„y„u
        }
        currentState = ZombieState.Stunned; // „T„ƒ„„„p„~„p„r„|„y„r„p„u„} „ƒ„€„ƒ„„„€„‘„~„y„u
    }

    // „O„„ˆ„y„€„~„p„|„„~„€: „u„ƒ„|„y „~„…„w„~„€ „€„ƒ„„„p„~„€„r„y„„„ „x„r„…„{„y „€„s„|„…„Š„u„~„y„‘
    public void StopStunSound()
    {
        // „L„€„s„y„{„p „€„ƒ„„„p„~„€„r„{„y „x„r„…„{„p „€„s„|„…„Š„u„~„y„‘, „u„ƒ„|„y „€„~ „x„p„ˆ„y„{„|„u„~ „y„|„y „t„|„y„„„u„|„„~„„z
        // „D„|„‘ PlayOneShot „€„q„„‰„~„€ „~„u „„„‚„u„q„…„u„„„ƒ„‘
    }

    // „M„u„„„€„t„ „t„|„‘ MobAI, „‰„„„€„q„ „„€„|„…„‰„p„„„ „x„~„p„‰„u„~„y„‘ „„‚„y„r„p„„„~„„‡ „„€„|„u„z („u„ƒ„|„y „~„u „‡„€„‰„u„Š„ „t„u„|„p„„„ „y„‡ public)
    // „^„„„€ „„|„€„‡„p„‘ „„‚„p„{„„„y„{„p - „|„…„‰„Š„u, „‰„„„€„q„ MobAI „ƒ„p„} „…„„‚„p„r„|„‘„| „ƒ„r„€„u„z „|„€„s„y„{„€„z „y „r„„x„„r„p„| „}„u„„„€„t„ ZombieAudio
    // public float GetAgroRadiusFromAudio() => mobAI.GetAgroRadius();
    // public float GetStoppingDistanceFromAudio() => mobAI.GetStoppingDistance();
}