using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public Transform target;         // �O�q���u�{��, �r���{�����s �{�����������s�� �q���t�u�� �r���p���p�������� �{�p�}�u���p (���u�������~�p�w)
    public float distance = 5.0f;    // �Q�p�����������~�y�u �{�p�}�u���� ���� ���u�|�y
    public float xSpeed = 120.0f;    // �R�{������������ �r���p���u�~�y�� ���� �s�����y�x���~���p�|�y
    public float ySpeed = 120.0f;    // �R�{������������ �r���p���u�~�y�� ���� �r�u�����y�{�p�|�y

    public float yMinLimit = -20f;   // �M�y�~�y�}�p�|���~���z ���s���| ���q�x�����p ���� �r�u�����y�{�p�|�y
    public float yMaxLimit = 80f;    // �M�p�{���y�}�p�|���~���z ���s���| ���q�x�����p ���� �r�u�����y�{�p�|�y

    public CharacterMovement characterMovement; // �R�����|�{�p �~�p ���{���y���� �t�r�y�w�u�~�y�� ���u�������~�p�w�p

    private float x = 0.0f;
    private float y = 0.0f;
    private float fixedYPosition; // �P�u���u�}�u�~�~�p�� �t�|�� �����p�~�u�~�y�� ���y�{���y�����r�p�~�~���z Y �����x�y���y�y �{�p�}�u���� �r�� �r���u�}�� �������w�{�p

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("�P���w�p�|���z�����p, �~�p�x�~�p�������u ���u�|�� (���u�������~�p�w�p) �t�|�� �r���p���u�~�y�� �{�p�}�u���� �r �y�~�����u�{�������u.");
            enabled = false; // �O���{�|�����p�u�} ���{���y����, �u���|�y ���u�|�� �~�u �~�p�x�~�p���u�~�p
            return;
        }

        //characterMovement = target.GetComponent<CharacterMovement>(); // �P���|�����p�u�} �������|�{�� �~�p ���{���y���� �t�r�y�w�u�~�y�� ���u�������~�p�w�p
        if (characterMovement == null)
        {
            Debug.LogError("�R�{���y���� CharacterMovement �~�u �~�p�z�t�u�~ �~�p ���q���u�{���u ���u�|�y. �T�q�u�t�y���u����, ������ ���~ �����y�{���u���|�u�~ �{ ���u�������~�p�w��.");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        fixedYPosition = transform.position.y; // �I�~�y���y�p�|�y�x�y�����u�} ���y�{���y�����r�p�~�~���� Y �����x�y���y�� �~�p���p�|���~���} �x�~�p���u�~�y�u�}
    }

    void LateUpdate()
    {
        if (target)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

            y = ClampAngle(y, yMinLimit, yMaxLimit); // �O�s���p�~�y���y�r�p�u�} �r�u�����y�{�p�|���~���z ���s���| ���q�x�����p

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 targetPosition = target.position; // �P���|�����p�u�} �����x�y���y�� ���u�|�y

            Vector3 position = rotation * negDistance + targetPosition;

            // �E���|�y ���u�������~�p�w �������s�p�u��, ���y�{���y�����u�} Y �����x�y���y�� �{�p�}�u����
            if (characterMovement.isActuallyJumping)
            {
                position.y = fixedYPosition; // �I�������|���x���u�} ���y�{���y�����r�p�~�~���� Y �����x�y���y��
            }
            else
            {
                fixedYPosition = position.y; // �O�q�~���r�|���u�} ���y�{���y�����r�p�~�~���� Y �����x�y���y��, �{���s�t�p ���u�������~�p�w �~�u �������s�p�u��, �������q�� �{�p�}�u���p �������t���|�w�p�|�p ���|�u�t���r�p���� �x�p ���q���u�z �r�����������z
            }

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
