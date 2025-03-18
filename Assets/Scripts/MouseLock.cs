using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLock : MonoBehaviour
{
    private void Start()
    {
        HideCursor();
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            ShowCursor();
            return;
        }
        HideCursor();
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
