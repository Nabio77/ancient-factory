using System.Collections.Generic;
using UnityEngine;

namespace CarbonWorld.Features.Grid
{
    public static class HexUtils
    {
        public const float OUTER_RADIUS = 1f;
        public const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;

        private static readonly HexCoord[] NeighborOffsets =
        {
            new(1, -1), new(1, 0), new(0, 1),
            new(-1, 1), new(-1, 0), new(0, -1)
        };

        public static Vector3 HexToWorld(HexCoord coord)
        {
            float x = coord.Q * INNER_RADIUS * 2f + coord.R * INNER_RADIUS;
            float z = coord.R * OUTER_RADIUS * 1.5f;
            return new Vector3(x, 0f, z);
        }

        public static int Distance(HexCoord a, HexCoord b)
        {
            int dq = a.Q - b.Q;
            int dr = a.R - b.R;
            int ds = (-a.Q - a.R) - (-b.Q - b.R);
            return (System.Math.Abs(dq) + System.Math.Abs(dr) + System.Math.Abs(ds)) / 2;
        }

        public static HexCoord[] GetNeighbors(HexCoord coord)
        {
            var neighbors = new HexCoord[6];
            for (int i = 0; i < 6; i++)
            {
                var offset = NeighborOffsets[i];
                neighbors[i] = new HexCoord(coord.Q + offset.Q, coord.R + offset.R);
            }
            return neighbors;
        }

        public static List<HexCoord> GetRing(HexCoord center, int radius)
        {
            if (radius <= 0)
                return new List<HexCoord> { center };

            var results = new List<HexCoord>(6 * radius);

            // Start at the "top-left" of the ring and walk around
            var current = new HexCoord(center.Q, center.R - radius);

            // Walk directions in order to complete the ring
            int[][] directions =
            {
                new[] { 1, 0 },   // SE
                new[] { 0, 1 },   // S
                new[] { -1, 1 },  // SW
                new[] { -1, 0 },  // NW
                new[] { 0, -1 },  // N
                new[] { 1, -1 }   // NE
            };

            for (int side = 0; side < 6; side++)
            {
                for (int step = 0; step < radius; step++)
                {
                    results.Add(current);
                    current = new HexCoord(
                        current.Q + directions[side][0],
                        current.R + directions[side][1]
                    );
                }
            }

            return results;
        }

        public static List<HexCoord> GetSpiral(HexCoord center, int rings)
        {
            var results = new List<HexCoord> { center };
            for (int r = 1; r <= rings; r++)
            {
                results.AddRange(GetRing(center, r));
            }
            return results;
        }
    }
}
