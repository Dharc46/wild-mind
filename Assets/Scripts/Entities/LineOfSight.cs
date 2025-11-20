using UnityEngine;

/// <summary>
/// Utility helpers for line-of-sight checks and simple cover finding logic.
/// Intended for AI behaviours that need quick Physics2D raycasts without
/// duplicating boilerplate code.
/// </summary>
public static class LineOfSight
{
    /// <summary>
    /// Checks whether there is an unobstructed path between two transforms using Physics2D raycast.
    /// Returns true if no collider within the layerMask blocks the line.
    /// </summary>
    public static bool HasLineOfSight(Transform origin, Transform target, LayerMask obstructionMask, out RaycastHit2D hitInfo)
    {
        if (origin == null || target == null)
        {
            hitInfo = default;
            return false;
        }

        return HasLineOfSight((Vector2)origin.position, (Vector2)target.position, obstructionMask, out hitInfo);
    }

    /// <summary>
    /// Checks whether there is an unobstructed path between two positions using Physics2D raycast.
    /// Returns true if no collider within the layerMask blocks the line.
    /// </summary>
    public static bool HasLineOfSight(Vector2 origin, Vector2 target, LayerMask obstructionMask, out RaycastHit2D hitInfo)
    {
        Vector2 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            hitInfo = default;
            return true;
        }

        direction /= distance;
        hitInfo = Physics2D.Raycast(origin, direction, distance, obstructionMask);
        return hitInfo.collider == null;
    }

    /// <summary>
    /// Samples points around the supplied origin and tries to find a location that both increases
    /// distance to the threat and is blocked by an obstruction raycast (cover).
    /// Returns true if a cover position is found.
    /// </summary>
    /// <param name="origin">Current AI position.</param>
    /// <param name="threat">Transform to hide from (usually the player).</param>
    /// <param name="searchRadius">Distance to sample potential cover points.</param>
    /// <param name="sampleCount">Number of radial samples to test.</param>
    /// <param name="obstructionMask">Layer mask considered as cover.</param>
    /// <param name="coverPoint">Resulting cover position if found.</param>
    public static bool TryFindCover(Vector2 origin, Transform threat, float searchRadius, int sampleCount, LayerMask obstructionMask, out Vector2 coverPoint)
    {
        coverPoint = Vector2.zero;
        if (threat == null || searchRadius <= 0f || sampleCount <= 0)
            return false;

        Vector2 threatPos = threat.position;
        float currentDistance = Vector2.Distance(origin, threatPos);

        bool found = false;
        float bestSqrDist = float.MaxValue;
        for (int i = 0; i < sampleCount; i++)
        {
            float angle = (360f / sampleCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 sample = origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * searchRadius;

            // Prefer points that increase distance.
            if (Vector2.Distance(sample, threatPos) <= currentDistance)
                continue;

            if (!HasLineOfSight(sample, threatPos, obstructionMask, out _))
            {
                float sqrDist = (sample - origin).sqrMagnitude;
                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    coverPoint = sample;
                    found = true;
                }
            }
        }

        return found;
    }
}


