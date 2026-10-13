using UnityEngine;

/// <summary>Places the visible model on the surface, independently of a station's import scale.</summary>
public static class WorldItemPlacement
{
    public static GameObject Spawn(ItemData item, Vector3 position, Quaternion orientation,
        Transform slot, GameObject station)
    {
        // Instantiate outside the scaled station first; parenting must retain world size.
        var instance = Object.Instantiate(item.prefab, Vector3.zero, item.prefab.transform.rotation);
        instance.transform.localScale *= Mathf.Max(0.1f, item.placementScale);
        if (item.itemType != null && item.itemType.Contains("武器")) LayFlat(instance);
        instance.transform.rotation = Quaternion.Euler(0, orientation.eulerAngles.y, 0) * instance.transform.rotation;
        instance.transform.position = position;
        float surfaceY = position.y;
        if (station != null)
        {
            var ray = new Ray(position + Vector3.up * 1f, Vector3.down);
            bool foundSurface = false;
            foreach (var collider in station.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || collider.GetComponentInParent<ItemBehaviour>() != null) continue;
                if (collider.Raycast(ray, out var hit, 2f) && hit.normal.y > 0.5f)
                {
                    surfaceY = foundSurface ? Mathf.Max(surfaceY, hit.point.y) : hit.point.y;
                    foundSurface = true;
                }
            }
        }
        if (TryGetBounds(instance, out var bounds))
        {
            // Some imported mesh pivots are at the hilt or below the model.
            instance.transform.position += new Vector3(position.x - bounds.center.x,
                surfaceY + 0.015f - bounds.min.y, position.z - bounds.center.z);
        }
        if (slot != null) instance.transform.SetParent(slot, true);
        return instance;
    }

    static void LayFlat(GameObject instance)
    {
        var original = instance.transform.rotation;
        var candidates = new System.Collections.Generic.List<Quaternion> { original };
        // Model children can also have import rotations. Test their axes, not only the prefab root.
        foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            foreach (var axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
                candidates.Add(Quaternion.FromToRotation(filter.transform.TransformDirection(axis), Vector3.up) * original);
        }
        float height = float.PositiveInfinity;
        var best = original;
        foreach (var candidate in candidates)
        {
            instance.transform.rotation = candidate;
            if (TryGetBounds(instance, out var bounds) && bounds.size.y < height)
            { height = bounds.size.y; best = candidate; }
        }
        instance.transform.rotation = best;
    }

    public static bool TryGetBounds(GameObject item, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var renderer in item.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer is ParticleSystemRenderer || renderer is TrailRenderer) continue;
            Bounds part;
            if (renderer is MeshRenderer)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0) continue;
                // Mesh bounds are available immediately, including during the spawn frame.
                var local = filter.sharedMesh.bounds;
                part = new Bounds(filter.transform.TransformPoint(local.min), Vector3.zero);
                for (int corner = 0; corner < 8; corner++)
                    part.Encapsulate(filter.transform.TransformPoint(new Vector3(
                        (corner & 1) == 0 ? local.min.x : local.max.x,
                        (corner & 2) == 0 ? local.min.y : local.max.y,
                        (corner & 4) == 0 ? local.min.z : local.max.z)));
            }
            else part = renderer.bounds;
            if (!found) { bounds = part; found = true; }
            else bounds.Encapsulate(part);
        }
        return found;
    }
}
