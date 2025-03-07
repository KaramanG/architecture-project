using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float mouseSensitivity = 100f; // Чувствительность мыши
    public Transform cameraPivot; // Пустой объект, к которому прикреплена камера
    private float xRotation = 0f;

    void Update()
    {
        // Вращение камеры с помощью мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Вертикальное вращение (вверх/вниз)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Ограничиваем угол поворота камеры

        // Применяем вращение камеры
        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Горизонтальное вращение (влево/вправо)
        transform.Rotate(Vector3.up * mouseX);
    }
}
