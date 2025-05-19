using UnityEngine;
using System.Collections; // �D�|�� IEnumerator (�u���|�y �����~�p�t���q�������� �x�p�t�u���w�{�y)

[RequireComponent(typeof(AudioSource))]
public class ZombieAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] idleSounds;         // �H�r���{�y �q�u�x�t�u�z�����r�y�� (�������p�~�y�u, �������~��)
    [SerializeField] private AudioClip[] agroSounds;         // �H�r���{ �����y ���q�~�p�����w�u�~�y�y �y�s�����{�p (�p�s���u�����y�r�~���z �����{)
    [SerializeField] private AudioClip[] attackSounds;       // �H�r���{�y �p���p�{�y
    [SerializeField] private AudioClip[] deathSounds;        // �H�r���{�y ���}�u�����y
    [SerializeField] private AudioClip[] stunSounds;         // �H�r���{�y �����y ���s�|�����u�~�y�y

    [Header("Sound Settings")]
    [SerializeField] private float minIdleSoundInterval = 5f; // �M�y�~�y�}�p�|���~���z �y�~���u���r�p�| �t�|�� �x�r���{���r �q�u�x�t�u�z�����r�y��
    [SerializeField] private float maxIdleSoundInterval = 10f; // �M�p�{���y�}�p�|���~���z �y�~���u���r�p�|

    private AudioSource audioSource;
    private MobAI mobAI; // �R�����|�{�p �~�p �����~���r�~���z ���{���y���� AI
    private HealthSystem healthSystem; // �R�����|�{�p �~�p ���y�����u�}�� �x�t�������r����

    private float nextIdleSoundTime;
    private bool hasPlayedAgroSoundSinceLastIdle = false;
    private bool hasPlayedDeathSound = false;

    // �R�����������~�y�u �t�|�� �������|�u�w�y�r�p�~�y��, �{���s�t�p �~���w�~�� �������y�s���p���� �x�r���{ �q�u�x�t�u�z�����r�y��
    private enum ZombieState
    {
        Idle,
        Chasing,
        Attacking,
        Stunned, // �E���|�y �� ���u�q�� �u������ �}�u���p�~�y�{�p ���s�|�����u�~�y��
        Dead
    }
    private ZombieState currentState = ZombieState.Idle;


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        mobAI = GetComponent<MobAI>();
        healthSystem = GetComponent<HealthSystem>(); // �P���|�����p�u�} HealthSystem

        if (mobAI == null)
        {
            Debug.LogError("MobAI component not found on this GameObject!", this);
            enabled = false; // �O���{�|�����p�u�} ���{���y����, �u���|�y �~�u�� MobAI
            return;
        }
        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem component not found on this GameObject!", this);
            // �M���w�~�� �~�u �����{�|�����p����, �~�� �~�u�{�����������u �����~�{���y�y �~�u �q���t���� ���p�q�����p���� �{�������u�{���~��
        }

        // �O�����y���~�p�|���~��: �����t���y���p�������� �~�p �����q�����y�u �����|�����u�~�y�� �������~�p, �u���|�y ���~�� �u������ �r HealthSystem
        // if (healthSystem != null) healthSystem.OnTakeDamage += PlayHurtSound;
    }

    void Start()
    {
        SetNextIdleSoundTime();
    }

    void Update()
    {
        if (mobAI == null || !mobAI.enabled) // �E���|�y MobAI �����{�|�����u�~ (�~�p�����y�}�u��, �������|�u ���}�u�����y)
        {
            if (healthSystem != null && healthSystem.IsDead() && !hasPlayedDeathSound)
            {
                // �^������ �q�|���{ �~�p ���|�����p�z, �u���|�y MobAI �����{�|�����p�u������ �t�� �r���x���r�p PlayDeathSound
                PlayDeathSoundInternal();
            }
            return;
        }

        // �O�q�~���r�|���u�} �������������~�y�u �x���}�q�y �~�p �����~���r�u MobAI
        UpdateZombieState();

        // �O�q���p�q�����{�p �x�r���{���r �r �x�p�r�y���y�}�������y ���� �������������~�y��
        HandleIdleSounds();
    }

    private void UpdateZombieState()
    {
        if (healthSystem != null && healthSystem.IsDead())
        {
            currentState = ZombieState.Dead;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player"); // �N�u �����u�~�� �������u�{���y�r�~�� �r Update, �~�� �t�|�� �����y�}�u���p
        if (player == null)
        {
            currentState = ZombieState.Idle; // �E���|�y �y�s�����{�p �~�u��, �x���}�q�y �q�u�x�t�u�z�����r���u��
            return;
        }
    }


    private void HandleIdleSounds()
    {
        if (currentState == ZombieState.Idle && Time.time >= nextIdleSoundTime)
        {
            PlayRandomSound(idleSounds);
            SetNextIdleSoundTime();
            hasPlayedAgroSoundSinceLastIdle = false; // �R�q���p�����r�p�u�} ���|�p�s �p�s����, �{���s�t�p ���~���r�p �q�u�x�t�u�z�����r���u��
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

    // --- �P���q�|�y���~���u �}�u�����t�� �t�|�� �r���x���r�p �y�x MobAI �y�|�y �t�����s�y�� ���{���y�������r ---

    // �B���x���r�p�u������, �{���s�t�p �x���}�q�y �~�p���y�~�p�u�� �����u���|�u�t���r�p�~�y�u (���u���u�����t �y�x Idle �r Agro/Chase)
    public void PlayAgroSound()
    {
        if (!hasPlayedAgroSoundSinceLastIdle && currentState != ZombieState.Idle) // �I�s���p�u�} �����|���{�� �u���|�y �~�u �y�s���p�|�y �� �������|�u�t�~�u�s�� Idle
        {
            PlayRandomSound(agroSounds);
            hasPlayedAgroSoundSinceLastIdle = true;
        }
    }

    // �B���x���r�p�u������ �����y �p���p�{�u
    public void PlayAttackSound()
    {
        if (currentState == ZombieState.Dead) return; // �N�u �p���p�{���r�p����, �u���|�y �}�u�����r
        PlayRandomSound(attackSounds);
    }


    // �B���x���r�p�u������ �����y ���}�u�����y
    public void PlayDeathSound()
    {
        PlayDeathSoundInternal();
    }

    private void PlayDeathSoundInternal()
    {
        if (!hasPlayedDeathSound)
        {
            // �O�����p�~���r�y���� ���u�{�����y�u �x�r���{�y, �u���|�y �~���w�~�� (�������q�u�~�~�� �u���|�y ������ �q���| �|����)
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            PlayRandomSound(deathSounds);
            hasPlayedDeathSound = true;
            currentState = ZombieState.Dead; // �T�q�u�t�y�}����, ������ �������������~�y�u ���q�~���r�|�u�~��
        }
    }

    // �B���x���r�p�u������ �����y ���s�|�����u�~�y�y (�u���|�y �u������ ���p�{�p�� �}�u���p�~�y�{�p)
    public void PlayStunSound()
    {
        if (currentState == ZombieState.Dead) return;
        PlayRandomSound(stunSounds);
        // �M���w�~�� �t���q�p�r�y���� �|���s�y�{�� �����u�����r�p�~�y�� �t�����s�y�� �x�r���{���r
        if (audioSource.isPlaying)
        {
            // audioSource.Stop(); // �Q�p���{���}�}�u�~���y�����r�p����, �u���|�y �x�r���{ ���s�|�����u�~�y�� �t���|�w�u�~ �����u�����r�p���� �t�����s�y�u
        }
        currentState = ZombieState.Stunned; // �T�����p�~�p�r�|�y�r�p�u�} �������������~�y�u
    }

    // �O�����y���~�p�|���~��: �u���|�y �~���w�~�� �������p�~���r�y���� �x�r���{�y ���s�|�����u�~�y��
    public void StopStunSound()
    {
        // �L���s�y�{�p �������p�~���r�{�y �x�r���{�p ���s�|�����u�~�y��, �u���|�y ���~ �x�p���y�{�|�u�~ �y�|�y �t�|�y���u�|���~���z
        // �D�|�� PlayOneShot ���q�����~�� �~�u �����u�q���u������
    }

    // �M�u�����t�� �t�|�� MobAI, �������q�� �����|�����p���� �x�~�p���u�~�y�� �����y�r�p���~���� �����|�u�z (�u���|�y �~�u �������u���� �t�u�|�p���� �y�� public)
    // �^���� ���|�����p�� �����p�{���y�{�p - �|�������u, �������q�� MobAI ���p�} �������p�r�|���| ���r���u�z �|���s�y�{���z �y �r���x���r�p�| �}�u�����t�� ZombieAudio
    // public float GetAgroRadiusFromAudio() => mobAI.GetAgroRadius();
    // public float GetStoppingDistanceFromAudio() => mobAI.GetStoppingDistance();
}