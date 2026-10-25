using System;
using System.Linq;
using UnityEngine;

/// <summary>Moves complete station hierarchies at scene start; never changes saved scene assets.</summary>
public static class DailyChallengeLayout
{
    public static int Apply(int seed)
    {
        var random = new System.Random(seed);
        var stations = UnityEngine.Object.FindObjectsByType<PlacementSlots>(FindObjectsSortMode.None)
            .Where(s => s.CompareTag("craft") || s.CompareTag("wash") || s.CompareTag("blacksmith"))
            .OrderBy(s => s.transform.position.x).ThenBy(s => s.transform.position.z).ToArray();
        var positions = stations.Select(s => s.transform.position).ToArray();
        int[] destinations = Permutation(stations.Length, random);
        float middleX = positions.Length == 0 ? 0 : positions.Average(p => p.x);
        for (int i = 0; i < stations.Length; i++)
        {
            var root = stations[i].transform;
            var destination = positions[destinations[i]];
            // FBX models have different pivots and axes. Preserve their height, scale and tilt.
            bool oppositeWall = (positions[i].x - middleX) * (destination.x - middleX) < 0;
            if (oppositeWall) root.Rotate(Vector3.up, 180f, Space.World);
            root.position += new Vector3(destination.x - positions[i].x, 0, destination.z - positions[i].z);
        }

        var sources = UnityEngine.Object.FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None)
            .Where(i => i.CompareTag("Infinity") && i.ItemData != null)
            .OrderBy(i => i.ItemData.itemID).ToArray();
        // Swap shelf locations at the bottom of each visible item, preserving its own orientation.
        var bases = sources.Select(Base).ToArray();
        destinations = Permutation(sources.Length, random);
        for (int i = 0; i < sources.Length; i++)
            sources[i].transform.position += bases[destinations[i]] - bases[i];
        Physics.SyncTransforms();
        return stations.Length + sources.Length;
    }

    static Vector3 Base(ItemBehaviour source)
    {
        var renderers = source.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return source.transform.position;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    static int[] Permutation(int count, System.Random random)
    {
        var result = Enumerable.Range(0, count).ToArray();
        // Sattolo's cycle: every object changes location when there are at least two.
        for (int i = count - 1; i > 0; i--)
        { int j = random.Next(i); int old = result[i]; result[i] = result[j]; result[j] = old; }
        return result;
    }
}
