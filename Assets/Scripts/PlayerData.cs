using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public float health;
    public float mana;
    public float positionX;
    public float positionY;
    public float positionZ;

    public PlayerData(float health, float mana, Vector3 position)
    {
        this.health = health;
        this.mana = mana;
        positionX = position.x;
        positionY = position.y;
        positionZ = position.z;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(positionX, positionY, positionZ);
    }
}
