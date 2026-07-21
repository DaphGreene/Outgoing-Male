using System;
using UnityEngine;

public static class PlayerSkinState
{
    public const string DefaultSkinId = "pink_envelope";
    public const string BlueEnvelopeSkinId = "blue_envelope";
    public const string GreenEnvelopeSkinId = "green_envelope";

    private const string SelectedSkinKey = "PlayerSkin.Selected";
    private const string BlueEnvelopeUnlockedKey = "PlayerSkin.BlueEnvelope.Unlocked";
    private const string GreenEnvelopeUnlockedKey = "PlayerSkin.GreenEnvelope.Unlocked";

    public static event Action OnSkinStateChanged;

    public static bool IsBlueEnvelopeUnlocked => PlayerPrefs.GetInt(BlueEnvelopeUnlockedKey, 0) == 1;
    public static bool IsGreenEnvelopeUnlocked => PlayerPrefs.GetInt(GreenEnvelopeUnlockedKey, 0) == 1;

    public static bool IsUnlocked(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId) || skinId == DefaultSkinId)
            return true;

        return skinId switch
        {
            BlueEnvelopeSkinId => IsBlueEnvelopeUnlocked,
            GreenEnvelopeSkinId => IsGreenEnvelopeUnlocked,
            _ => false
        };
    }

    public static string SelectedSkinId
    {
        get
        {
            string selectedSkinId = PlayerPrefs.GetString(SelectedSkinKey, DefaultSkinId);
            if (selectedSkinId == BlueEnvelopeSkinId && !IsBlueEnvelopeUnlocked)
                return DefaultSkinId;
            if (selectedSkinId == GreenEnvelopeSkinId && !IsGreenEnvelopeUnlocked)
                return DefaultSkinId;

            return string.IsNullOrWhiteSpace(selectedSkinId) ? DefaultSkinId : selectedSkinId;
        }
    }

    public static bool TryUnlock(string skinId, int cost)
    {
        if (IsUnlocked(skinId))
            return true;

        if (!StampBank.TrySpendStamps(cost))
            return false;

        switch (skinId)
        {
            case BlueEnvelopeSkinId:
                PlayerPrefs.SetInt(BlueEnvelopeUnlockedKey, 1);
                break;
            case GreenEnvelopeSkinId:
                PlayerPrefs.SetInt(GreenEnvelopeUnlockedKey, 1);
                break;
            default:
                return false;
        }

        SelectSkin(skinId);
        return true;
    }

    public static void SelectSkin(string skinId)
    {
        string resolvedSkinId = ResolveSelectableSkinId(skinId);
        PlayerPrefs.SetString(SelectedSkinKey, resolvedSkinId);
        PlayerPrefs.Save();
        OnSkinStateChanged?.Invoke();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(SelectedSkinKey);
        PlayerPrefs.DeleteKey(BlueEnvelopeUnlockedKey);
        PlayerPrefs.DeleteKey(GreenEnvelopeUnlockedKey);
        PlayerPrefs.Save();
        OnSkinStateChanged?.Invoke();
    }

    private static string ResolveSelectableSkinId(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId) || skinId == DefaultSkinId)
            return DefaultSkinId;

        if (skinId == BlueEnvelopeSkinId && IsBlueEnvelopeUnlocked)
            return BlueEnvelopeSkinId;
        if (skinId == GreenEnvelopeSkinId && IsGreenEnvelopeUnlocked)
            return GreenEnvelopeSkinId;

        return DefaultSkinId;
    }
}
