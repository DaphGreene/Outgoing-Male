using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StampCatalog", menuName = "Data/Stamps/Stamp Catalog")]
public class StampCatalog : ScriptableObject
{
    [SerializeField] private List<StampDefinition> stamps = new();

    public IReadOnlyList<StampDefinition> Stamps => stamps;

    public StampDefinition FindById(string stampId)
    {
        if (string.IsNullOrWhiteSpace(stampId))
            return null;

        for (int i = 0; i < stamps.Count; i++)
        {
            StampDefinition stamp = stamps[i];
            if (stamp == null)
                continue;

            if (stamp.StampId == stampId)
                return stamp;
        }

        return null;
    }
}
