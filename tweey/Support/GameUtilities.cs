﻿namespace Tweey.Support;

public static class GameUtilities
{
    public static IEnumerable<Vector2i> EnumerateNeighborLocations(Vector2 center, int radiusMin = 1, int radiusMax = 1) =>
        EnumerateNeighborLocations(new Vector2i((int)center.X, (int)center.Y), radiusMin, radiusMax);

    public static IEnumerable<Vector2i> EnumerateNeighborLocations(Vector2i center, int radiusMin = 1, int radiusMax = 1)
    {
        if (radiusMin == 0) yield return center;

        for (int radius = Math.Max(radiusMin, 1); radius <= radiusMax; radius++)
        {
            // top & bottom
            for (int i = -radius; i <= radius; i++)
            {
                yield return new(center.X + i, center.Y - radius);
                yield return new(center.X + i, center.Y + radius);
            }

            // left & right
            for (int i = 1 - radius; i <= radius - 1; i++)
            {
                yield return new(center.X - radius, center.Y + i);
                yield return new(center.X + radius, center.Y + i);
            }
        }
    }

    internal static IEnumerable<Entity> OrderByDistanceFrom<T>(this IEnumerable<T> source, Vector2 targetLocation, Func<T, Vector2> getLocation, Func<T, Entity> getEntity) =>
        source.OrderBy(b => (getLocation(b) - targetLocation).LengthSquared()).Select(getEntity);
}
