using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSystem : MonoBehaviour
{
    [SerializeField] private float physicalDamage;
    [SerializeField] private float magicDamage;

    public float GetPhysicalDamage() { return physicalDamage; }
    public float GetMagicDamage() { return magicDamage; }
}
