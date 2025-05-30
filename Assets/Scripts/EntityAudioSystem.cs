using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAudioSystem : MonoBehaviour
{
    private AudioSource audioSource;

    [SerializeField] private AudioClip idleSounds;
    [SerializeField] private AudioClip agroSounds;  
    [SerializeField] private AudioClip attackSounds;
    [SerializeField] private AudioClip deathSounds;
    [SerializeField] private AudioClip stunSounds;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null ) { return; }
    }
}
