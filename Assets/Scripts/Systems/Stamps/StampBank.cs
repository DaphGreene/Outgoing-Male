using System;
using System.Collections.Generic;
using UnityEngine;

public static class StampBank
{
    private const string StampCountKey = "Stamp_Count";
    private const string DiscoveredStampIdsKey = "Stamp_DiscoveredIds";

    private static bool isLoaded;
    private static int cachedCount;
    private static bool areDiscoveredStampIdsLoaded;
    private static HashSet<string> discoveredStampIds = new();

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

    public static bool TrySpendStamps(int amount)
    {
        if (amount <= 0)
            return true;

        if (Count < amount)
            return false;

        SetCount(Count - amount);
        return true;
    }

    public static void ReloadFromPlayerPrefs()
    {
        isLoaded = false;
        areDiscoveredStampIdsLoaded = false;
        EnsureLoaded();
        OnStampCountChanged?.Invoke(cachedCount);
    }

    public static bool RegisterStampDiscovery(string stampId)
    {
        if (string.IsNullOrWhiteSpace(stampId))
            return false;

        EnsureDiscoveredStampIdsLoaded();
        if (!discoveredStampIds.Add(stampId))
            return false;

        SaveDiscoveredStampIds();
        return true;
    }

    public static bool HasDiscoveredStamp(string stampId)
    {
        if (string.IsNullOrWhiteSpace(stampId))
            return false;

        EnsureDiscoveredStampIdsLoaded();
        return discoveredStampIds.Contains(stampId);
    }

    public static void ClearDiscoveredStamps()
    {
        EnsureDiscoveredStampIdsLoaded();
        discoveredStampIds.Clear();
        PlayerPrefs.DeleteKey(DiscoveredStampIdsKey);
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        cachedCount = Mathf.Max(0, PlayerPrefs.GetInt(StampCountKey, 0));
        isLoaded = true;
    }

    private static void EnsureDiscoveredStampIdsLoaded()
    {
        if (areDiscoveredStampIdsLoaded)
            return;

        discoveredStampIds.Clear();

        string rawIds = PlayerPrefs.GetString(DiscoveredStampIdsKey, string.Empty);
        if (!string.IsNullOrEmpty(rawIds))
        {
            string[] splitIds = rawIds.Split('|', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitIds.Length; i++)
                discoveredStampIds.Add(splitIds[i]);
        }

        areDiscoveredStampIdsLoaded = true;
    }

    private static void SaveDiscoveredStampIds()
    {
        EnsureDiscoveredStampIdsLoaded();
        PlayerPrefs.SetString(DiscoveredStampIdsKey, string.Join("|", discoveredStampIds));
        PlayerPrefs.Save();
    }
}
