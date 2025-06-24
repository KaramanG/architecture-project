// PeaceModeManager.cs
using UnityEngine;

public static class PeaceModeManager
{
    public static bool IsPeacefulModeActive { get; private set; } = false; // По умолчанию мирный режим ВЫКЛЮЧЕН

    public static event System.Action<bool> OnPeacefulModeChanged;

    public static void SetPeacefulMode(bool isActive)
    {
        if (IsPeacefulModeActive == isActive) return;

        IsPeacefulModeActive = isActive;
        Debug.Log("Peaceful Mode " + (IsPeacefulModeActive ? "ENABLED" : "DISABLED"));
        OnPeacefulModeChanged?.Invoke(IsPeacefulModeActive);
    }
}