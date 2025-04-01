using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float damageAmount = 30f; // �T�����~ ���p�z�u���q���|�p, �}���w�~�� �~�p���������y���� �r �y�~�����u�{�������u
    public LayerMask mobLayer; // �R�|���z �}���q���r, �~�p���������z �r �y�~�����u�{�������u
    public float lifeTime = 3f; // �B���u�}�� �w�y�x�~�y ���p�z�u���q���|�p �r ���u�{���~�t�p��

    void Start()
    {
        Destroy(gameObject, lifeTime); // �T�~�y�������w�y���� ���p�z�u���q���| ���u���u�x �x�p�t�p�~�~���u �r���u�}��, �t�p�w�u �u���|�y �~�y�{���s�� �~�u �������p�t�u��
    }

    void OnTriggerEnter(Collider other)
    {
        if ((mobLayer.value & (1 << other.gameObject.layer)) != 0) // �P�����r�u�����u�}, ������ �������|�{�~���|�y���� �� ���q���u�{�����} �~�p ���|���u mobLayer
        {
            HealthBar mobHealthBar = other.GetComponent<HealthBar>();
            if (mobHealthBar != null)
            {
                mobHealthBar.TakeDamage(damageAmount); // �N�p�~�����y�} �������~ �}���q��
                Debug.Log("�U�p�z�u���q���| �������p�| �r �}���q�p! �N�p�~�u���u�~ �������~: " + damageAmount);
                Destroy(gameObject); // �T�~�y�������w�p�u�} ���p�z�u���q���| �������|�u �������p�t�p�~�y��
            }
        }
    }
}
