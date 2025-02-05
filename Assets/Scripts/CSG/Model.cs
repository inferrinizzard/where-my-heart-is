using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace CSG
{
	/// <summary>
	/// Representation of a Mesh used for CSG operations
	/// </summary>
	public class Model
	{
		public List<Vertex> vertices;
		public List<Triangle> triangles;
		public List<Edge> edges;

		public Matrix4x4 worldToLocal;
		public Matrix4x4 localToWorld;

		/// <summary>
		/// Initializes a new empty Model
		/// </summary>
		public Model()
		{
			vertices = new List<Vertex>();
			triangles = new List<Triangle>();
			edges = new List<Edge>();
		}

		public Model(Mesh mesh, Transform transform) : this(mesh)
		{
			worldToLocal = transform.worldToLocalMatrix;
			localToWorld = transform.localToWorldMatrix;
		}

		/// <summary>
		/// Parses the given Mesh and creates a CSG.Model version for manipulation
		/// </summary>
		/// <param name="mesh">The Mesh to parse</param>
		public Model(Mesh mesh)
		{
			if (mesh.vertices.Length == mesh.uv.Length)
			{
				vertices = mesh.vertices.Select((vertex, index) => new Vertex(index, vertex, mesh.uv[index])).ToList();
			}
			else
			{
				vertices = mesh.vertices.Select((vertex, index) => new Vertex(index, vertex, new Vector2(0, 0))).ToList();
			}

			List<Vertex> uniqueVertices = new List<Vertex>();
			foreach (Vertex vertex in vertices)
			{
				Vertex existingVertex = uniqueVertices.Find(testVertex => testVertex.value == vertex.value); // TODO: slow
				if (existingVertex == null)
				{
					uniqueVertices.Add(vertex);
				}
				else
				{
					uniqueVertices.Add(existingVertex);
				}
			}
			vertices = uniqueVertices;

			triangles = new List<Triangle>();

			int[] meshTriangles = mesh.triangles;

			for (int i = 0; i < meshTriangles.Length; i += 3)
			{
				Triangle triangle = new Triangle(vertices[meshTriangles[i]], vertices[meshTriangles[i + 1]], vertices[meshTriangles[i + 2]]);
				triangles.Add(triangle);
			}

			uniqueVertices = new List<Vertex>();
			foreach (Vertex vertex in vertices)
			{
				Vertex existingVertex = uniqueVertices.Find(testVertex => testVertex == vertex);
				if (existingVertex == null)
				{
					uniqueVertices.Add(vertex);
				}
			}

			vertices = uniqueVertices;

			CreateEdges();
		}

		/// <summary>
		/// Creates an edge object for each unique pair of vertices in this model that share a triangle
		/// </summary>
		public void CreateEdges()
		{
			edges = new List<Edge>();
			//Debug.Log(triangles.Count);
			foreach (Triangle triangle in triangles)
			{
                triangle.edges.Clear();

				for (int i = 0; i < 3; i++)
				{
					Edge preexistingEdge = edges.Find(edge =>
						edge.vertices.Contains(triangle.vertices[i]) &&
						edge.vertices.Contains(triangle.vertices[(i + 1) % 3])
					);

					if (preexistingEdge == null) // if there are no edges that contain both the current vertices
					{
						Edge createdEdge = new Edge(triangle.vertices[i], triangle.vertices[(i + 1) % 3]);
						edges.Add(createdEdge);
						triangle.edges.Add(createdEdge);
						createdEdge.triangles.Add(triangle);
					}
					else // otherwise, the edge already exists
					{
						triangle.edges.Add(preexistingEdge);
						preexistingEdge.triangles.Add(triangle);
					}
				}
			}

		}

		/// <summary>
		/// Finds all intersections of this model's edges with the given model's triangles and stores them in this model
		/// </summary>
		/// <param name="other">The model to find intersections with</param>
		public List<Vertex> IntersectWith(Model other)
		{

            ClearCutMetadata();
			other.ClearCutMetadata();

			List<Vertex> createdVertices = new List<Vertex>();

			createdVertices.AddRange(IntersectAWithB(this, other));
			createdVertices.AddRange(IntersectAWithB(other, this));

			return createdVertices;
		}

		private void ClearCutMetadata()
		{
			vertices.ForEach(vertex => vertex.ClearCutMetadata());
			edges.ForEach(edge => edge.ClearCutMetadata());
			triangles.ForEach(triangle => triangle.ClearCutMetadata());
		}

		/// <summary>
		/// Collects and stores all intersections of the edges of modelA with the triangles of modelB
		/// for use in CSG.Operations
		/// </summary>
		/// <returns>The list of newly created vertices</returns>
		private List<Vertex> IntersectAWithB(Model modelA, Model modelB)
		{
			List<Vertex> createdVertices = new List<Vertex>();
			foreach (Edge edge in modelA.edges)
			{
				foreach (Triangle triangle in modelB.triangles)
				{
					Intersection intersection = edge.IntersectWithTriangle(triangle);

					if (intersection != null)
					{
						createdVertices.Add(intersection.vertex);
						edge.intersections.Add(intersection);

						triangle.internalIntersections.Add(intersection);
					}
				}
            }
			return createdVertices;
		}

		/// <summary>
		/// Creates a unity Mesh from a list of CSG.Vertex and a list of CSG.Triangle
		/// </summary>
		/// <param name="vertices">The vertices of the mesh</param>
		/// <param name="triangles">The triangles of the mesh</param>
		/// <returns>The parsed Mesh</returns>
		public Mesh ToMesh(Matrix4x4 worldToLocalMatrix)
		{
			//TODO: each face may need to have its vertices inputed as separate things
			Mesh mesh = new Mesh();

			Dictionary<int, int> vertexIndices = new Dictionary<int, int>();
			// reindex vertices and add them to the mesh
			List<Vector3> createdVertices = vertices.Select((vertex, index) =>
			{
				vertexIndices.Add(vertex.GetHashCode(), index);

				return worldToLocalMatrix.MultiplyPoint(vertex.value);
			}).ToList();

			mesh.SetVertices(createdVertices);

            // add triangles to mesh
            int[] newTriangles = triangles.SelectMany(triangle => triangle.vertices.Select(vertex => vertexIndices[vertex.GetHashCode()])).ToArray();

			mesh.SetTriangles(newTriangles, 0);
			mesh.RecalculateNormals();

			return mesh;
		}

		/// <summary>
		/// Converts the location of each vertex of this Model to be expressed with respect with "to" instead of with respect to "from"
		/// </summary>
		/// <param name="from">The transform representing the model's current coordinate system</param>
		/// <param name="to">The transform representing the coordinate system to convert to</param>
		public void ConvertCoordinates(Transform from, Transform to)
		{
			foreach (Vertex vertex in vertices)
			{
				vertex.value = to.worldToLocalMatrix.MultiplyPoint3x4(from.localToWorldMatrix.MultiplyPoint3x4(vertex.value));
			}
		}

		/// <summary>
		/// Converts the locations of the vertices of this model from the local space of the given transform to world space
		/// </summary>
		public void ApplyTransformation(Matrix4x4 transformationMatrix)
		{
			vertices.ForEach(vertex => vertex.value = transformationMatrix.MultiplyPoint3x4(vertex.value));
		}

		public void ConvertToWorld()
		{
			ApplyTransformation(localToWorld);
		}

		public void ConvertToLocal()
		{
			ApplyTransformation(worldToLocal);
		}

		/// <summary>
		/// Returns whether any vertices of this model are contained by the given model
		/// </summary>
		/// <param name="other">The model to check for intersections</param>
		/// <param name="error">The equality comparison error constant</param>
		/// <returns>True if a contained vertex is found, false otherwise</returns>
		public bool Intersects(Model other, float error, bool useEdgeFace = false)
		{
            if(useEdgeFace)
            {
                foreach(Edge edge in edges)
                {
                    foreach (Triangle triangle in other.triangles)
                    {
                        if(Raycast.LineSegmentToTriangle(edge.vertices[0].value, edge.vertices[1].value, triangle, error) != null)
                        {
                            return true;
                        }
                    }
                }
            }

			foreach (Vertex vertex in vertices)
			{
				if (vertex.ContainedBy(other, error))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Adds the given triangle and its vertices to the model
		/// </summary>
		/// <param name="triangle">The triangle to add</param>
		public void AddTriangle(Triangle triangle)
		{
			triangles.Add(triangle);
			triangle.vertices.ForEach(vertex =>
			{
				if (!vertices.Contains(vertex))
				{
					vertices.Add(vertex);
				}
			});
		}

		/// <summary>
		/// Adds a list of triangles to the the model
		/// </summary>
		public void AddTriangles(List<Triangle> trianglesToAdd)
		{
			trianglesToAdd.ForEach(triangle =>
			{
				triangles.Add(triangle);
				triangle.vertices.ForEach(vertex =>
				{
					if (!vertices.Contains(vertex))
					{
						vertices.Add(vertex);
					}
				});
			});
		}

		public void RemoveVertex(Vertex vertex)
		{
			vertex.triangles.ForEach(triangle => RemoveTriangle(triangle));
			edges = edges.Where(edge => !edge.vertices.Contains(vertex)).ToList();
		}

		public void RemoveTriangle(Triangle triangle)
		{
			triangles.Remove(triangle);
		}

		/// <summary>
		/// Flips the normals of all this model's triangles
		/// </summary>
		public void FlipNormals()
		{
			triangles.ForEach(triangle => triangle.FlipNormal());
		}

        /// <summary>
        /// Recalculates the normal vectors for each triangle of the model
        /// </summary>
        public void RecalculateNormals()
        {
            triangles.ForEach(triangle => triangle.CalculateNormal());
        }

		/// <summary>
		/// Combines the triangles of the two given models into one model
		/// </summary>
		/// <returns>The combined model</returns>
		public static Model Combine(Model modelA, Model modelB)
		{
			Model result = new Model();

			modelA.triangles.ForEach(triangle =>
			{
				result.AddTriangle(triangle);
			});

			modelB.triangles.ForEach(triangle =>
			{
				result.AddTriangle(triangle);
			});

			return result;
		}

		public void Draw(Color color)
		{
			edges.ForEach(edge => edge.Draw(color));
			triangles.ForEach(triangle => triangle.DrawNormal(Color.green));
		}
	}
}
