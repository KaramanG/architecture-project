using UnityEngine;

public static class PeaceModeManager
{
    public static bool IsPeacefulModeActive = false; //������� �� ������ �����

    public static void SetPeacefulMode(bool isActive)
    {
        IsPeacefulModeActive = isActive;
    }

    public static void TogglePeacefulMode()
    {
        IsPeacefulModeActive = !IsPeacefulModeActive;
    }
}