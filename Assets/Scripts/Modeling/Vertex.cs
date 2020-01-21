﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSG
{
    /// <summary>
    /// A wrapper for Vector3 that includes useful data for clipping meshes
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// Index in the mesh's vertex array
        /// </summary>
        public int index;

        /// <summary>
        /// Location in model space
        /// </summary>
        public Vector3 value;

        /// <summary>
        /// Whether the vertex lies within the bounding shape
        /// </summary>
        public bool containedByBound;

        /// <summary>
        /// Whether this vertex has been identified to be in at least one loop
        /// </summary>
        public bool usedInLoop;

        /// <summary>
        /// A list of EdgeLoops that this vertex has been identified to be a part of
        /// </summary>
        public List<EdgeLoop> loops;

        /// <summary>
        /// A list of triangles this vertex appears in
        /// </summary>
        public List<Triangle> triangles;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="index"> Index in the mesh's vertex array </param>
        /// <param name="value"> Location in model space </param>
        /// <param name="containedByBound"> Whether the vertex lies within the bounding shape </param>
        public Vertex(int index, Vector3 value)
        {
            this.index = index;
            this.value = value;

            loops = new List<EdgeLoop>();
            usedInLoop = false;
            triangles = new List<Triangle>();
        }

        /// <summary>
        /// Determines whether this vertex and the given vertex both appear on the same triangle
        /// </summary>
        /// <param name="vertex">  The vertex to compare against </param>
        /// <returns>Whether this vertex and the given vertex both appear on the same triangle</returns>
        public bool SharesTriangle(Vertex vertex) => triangles.Any(t => vertex.triangles.Contains(t));

        public override string ToString()
        {
            return value.ToString("F4");
        }

        /// <summary>
        /// Determines whether this vertex lies inside the area of the given loop, assuming they share a plane
        /// </summary>
        /// <param name="loop">  The loop to check for containment </param>
        /// <returns>Whether this vertex lies inside the area of the given loop, assuming they share a plane</returns>
        public bool LiesWithinLoop(EdgeLoop loop)
        {
            // collect intersection points
            Vector3 castDirection = (loop.vertices[0].value - loop.vertices[1].value).normalized;
            List<Vector3> positiveIntersections = new List<Vector3>();
            //List<Vector3> negativeIntersections = new List<Vector3>();
            for (int i = 0; i < loop.vertices.Count; i++)
            {
                Point3 intersection = Raycast.RayToLineSegment(
                    this.value,
                    castDirection,
                    loop.vertices[i].value,
                    loop.vertices[(i + 1) % loop.vertices.Count].value);
                if (intersection != null)
                {
                    Vector3 directionToIntersection = (intersection.value - this.value).normalized;
                    if (Vector3.Dot(directionToIntersection, castDirection) > 0)
                    {
                        positiveIntersections.Add(intersection.value);
                    }
                }
            }

            // remove duplicates
            RemoveDuplicates(positiveIntersections);
            // RemoveDuplicates(negativeIntersections);

            // count # above, below
            // if both odd, return true, else return false
            return positiveIntersections.Count % 2 == 1; // && negativeIntersections.Count % 2 == 1;
        }

        /// <summary>
        /// Takes a List and merges any vertices that are too similar
        /// </summary>
        /// <param name="list"> The list to remove duplicates from </param>
        /// <param name="margin"> The distance cutoff to clip duplicates </param>
        private void RemoveDuplicates(List<Vector3> list, double margin = .0001)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                for (int k = i; k >= 0; k--)
                {
                    if (Vector3.Distance(list[i], list[k]) < margin)
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        //=> list.RemoveAll(a => list.Any(b => a!=b Vector3.Distance(a, b) < 0.0001)); // does not acount for A|B → B|A comparisons
    }
}
