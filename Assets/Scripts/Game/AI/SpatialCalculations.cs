using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Admirals.AI
{
    /// <summary>
    /// Provides spatial calculation utilities for AI operations using snapshot data.
    /// All methods work with grid coordinates and do not require direct GridManager access.
    /// </summary>
    public static class SpatialCalculations
    {
        /// <summary>
        /// Calculates Manhattan distance between two grid coordinates.
        /// Manhattan distance is |x1-x2| + |y1-y2|.
        /// This is the standard for grid-based tactical calculations.
        /// </summary>
        /// <param name="a">First coordinate</param>
        /// <param name="b">Second coordinate</param>
        /// <returns>Manhattan distance as integer grid units</returns>
        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Calculates Euclidean distance between two grid coordinates.
        /// Only use when true geometric distance is required (rare).
        /// For most tactical calculations, prefer ManhattanDistance.
        /// </summary>
        /// <param name="a">First coordinate</param>
        /// <param name="b">Second coordinate</param>
        /// <returns>Euclidean distance as float</returns>
        public static float EuclideanDistance(Vector2Int a, Vector2Int b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Calculates the diagonal length of a rectangular grid.
        /// Used for normalizing distances to [0, 1] range in scoring algorithms.
        /// </summary>
        /// <param name="width">Grid width</param>
        /// <param name="height">Grid height</param>
        /// <returns>Diagonal length as float</returns>
        public static float GridDiagonal(int width, int height)
        {
            return Mathf.Sqrt(width * width + height * height);
        }
    }
}
