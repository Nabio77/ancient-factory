using System.Collections.Generic;
using UnityEngine;

namespace CarbonWorld.Features.Grid
{
    /// <summary>
    /// Utility functions for hexagonal tilemap operations.
    /// Uses odd-r offset coordinates (pointy-top hexagons, odd rows shifted right).
    /// </summary>
    public static class HexUtils
    {
        // Neighbor offsets for odd-r offset hexagonal coordinates
        // For even rows (y % 2 == 0)
        private static readonly Vector3Int[] EvenRowOffsets =
        {
            new(-1, -1, 0), new(0, -1, 0),  // top-left, top-right
            new(-1, 0, 0), new(1, 0, 0),     // left, right
            new(-1, 1, 0), new(0, 1, 0)      // bottom-left, bottom-right
        };

        // For odd rows (y % 2 == 1)
        private static readonly Vector3Int[] OddRowOffsets =
        {
            new(0, -1, 0), new(1, -1, 0),   // top-left, top-right
            new(-1, 0, 0), new(1, 0, 0),     // left, right
            new(0, 1, 0), new(1, 1, 0)       // bottom-left, bottom-right
        };

        /// <summary>
        /// Gets all 6 neighbor positions for a hex cell in odd-r offset coordinates.
        /// </summary>
        public static Vector3Int[] GetNeighbors(Vector3Int pos)
        {
            var offsets = (pos.y & 1) == 0 ? EvenRowOffsets : OddRowOffsets;
            var neighbors = new Vector3Int[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = pos + offsets[i];
            }
            return neighbors;
        }

        /// <summary>
        /// Calculates the hex distance between two cells in offset coordinates.
        /// Converts to cube coordinates for accurate distance calculation.
        /// </summary>
        public static int Distance(Vector3Int a, Vector3Int b)
        {
            var cubeA = OffsetToCube(a);
            var cubeB = OffsetToCube(b);
            return CubeDistance(cubeA, cubeB);
        }

        /// <summary>
        /// Gets all hex cells in a ring around a center position.
        /// </summary>
        public static List<Vector3Int> GetRing(Vector3Int center, int radius)
        {
            if (radius <= 0)
                return new List<Vector3Int> { center };

            var results = new List<Vector3Int>(6 * radius);
            var centerCube = OffsetToCube(center);

            // Start at one corner and walk around the ring in cube coordinates
            var current = new Vector3Int(centerCube.x, centerCube.y + radius, centerCube.z - radius);

            // Direction vectors in cube coordinates
            Vector3Int[] cubeDirections =
            {
                new(1, -1, 0),   // SE
                new(0, -1, 1),   // S
                new(-1, 0, 1),   // SW
                new(-1, 1, 0),   // NW
                new(0, 1, -1),   // N
                new(1, 0, -1)    // NE
            };

            for (int side = 0; side < 6; side++)
            {
                for (int step = 0; step < radius; step++)
                {
                    results.Add(CubeToOffset(current));
                    current += cubeDirections[side];
                }
            }

            return results;
        }

        /// <summary>
        /// Gets all hex cells in a filled spiral pattern from center outward.
        /// </summary>
        public static List<Vector3Int> GetSpiral(Vector3Int center, int rings)
        {
            var results = new List<Vector3Int> { center };
            for (int r = 1; r <= rings; r++)
            {
                results.AddRange(GetRing(center, r));
            }
            return results;
        }

        // Cube coordinate helpers for accurate hex math
        private static Vector3Int OffsetToCube(Vector3Int offset)
        {
            int x = offset.x - (offset.y - (offset.y & 1)) / 2;
            int z = offset.y;
            int y = -x - z;
            return new Vector3Int(x, y, z);
        }

        private static Vector3Int CubeToOffset(Vector3Int cube)
        {
            int col = cube.x + (cube.z - (cube.z & 1)) / 2;
            int row = cube.z;
            return new Vector3Int(col, row, 0);
        }

        private static int CubeDistance(Vector3Int a, Vector3Int b)
        {
            return (System.Math.Abs(a.x - b.x) + System.Math.Abs(a.y - b.y) + System.Math.Abs(a.z - b.z)) / 2;
        }
    }
}
