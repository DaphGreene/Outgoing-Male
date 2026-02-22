using System;
using UnityEngine;

public static class StampBank
{
    private const string StampCountKey = "Stamp_Count";

    private static bool isLoaded;
    private static int cachedCount;

    public static event Action<int> OnStampCountChanged;

    public static int Count
    {
        get
        {
            EnsureLoaded();
            return cachedCount;
        }
    }

    public static void AddStamps(int amount)
    {
        if (amount <= 0)
            return;

        SetCount(Count + amount);
    }

    public static void SetCount(int newCount)
    {
        EnsureLoaded();

        cachedCount = Mathf.Max(0, newCount);
        PlayerPrefs.SetInt(StampCountKey, cachedCount);
        PlayerPrefs.Save();

        OnStampCountChanged?.Invoke(cachedCount);
    }

    public static void ReloadFromPlayerPrefs()
    {
        isLoaded = false;
        EnsureLoaded();
        OnStampCountChanged?.Invoke(cachedCount);
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        cachedCount = Mathf.Max(0, PlayerPrefs.GetInt(StampCountKey, 0));
        isLoaded = true;
    }
}
