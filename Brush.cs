#region License
// Copyright (c) 2009 Sander van Rossen
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealtimeCSG
{
	// Note: This code is currently being refactored.
	public class Brush
	{
		public readonly List<Plane>		Planes		= new List<Plane>();

		sealed class EdgeIntersection
		{
			public EdgeIntersection(PointIntersection intersectionA, PointIntersection intersectionB, Plane planeA, Plane planeB)
			{
				Edges[0].Twin = Edges[1];
				Edges[1].Twin = Edges[0];
				
				Planes[0]			= planeA;
				Planes[1]			= planeB;
				Intersections[0]	= intersectionA;
				Intersections[1]	= intersectionB;
				Edges[0].Vertex		= intersectionA.Vertex;
				Edges[1].Vertex		= intersectionB.Vertex;
			}

			public PointIntersection[]		Intersections	= new PointIntersection[2];
			public Plane[]					Planes			= new Plane[2];
			public HalfEdge[]				Edges			= new HalfEdge[] { new HalfEdge(), new HalfEdge() };
		}
		

		sealed class PointIntersection
		{
			public PointIntersection(Vector3f vertex, List<int> planes) 
			{ 
				Vertex = vertex; 
				foreach(var plane in planes)
					PlaneIndices.Add(plane); 
			}

			public readonly List<EdgeIntersection>	Edges			= new List<EdgeIntersection>();
			public readonly HashSet<int>			PlaneIndices	= new HashSet<int>();
			public readonly Vector3f				Vertex;
		}


		public BrushMesh CreateMesh()
		{
			var mesh = new BrushMesh();
			for (int i = 0; i < Planes.Count; i++)
				mesh.Polygons.Add(new Polygon());

			var polygons			= mesh.Polygons;
			var pointIntersections	= new List<PointIntersection>(Planes.Count * Planes.Count);
			var intersectingPlanes	= new List<int>();
			for (int planeIndex1 = 0; planeIndex1 < Planes.Count - 2; planeIndex1++)
			{
				var	plane1		= Planes[planeIndex1];
				for (int planeIndex2 = planeIndex1 + 1; planeIndex2 < Planes.Count - 1; planeIndex2++)
				{
					var	plane2		= Planes[planeIndex2];
					for (int planeIndex3 = planeIndex2 + 1; planeIndex3 < Planes.Count; planeIndex3++)
					{
						var	plane3	= Planes[planeIndex3];
						var vertex	= Plane.Intersection(plane1, plane2, plane3);

						if (!vertex.IsValid())
							continue;

						intersectingPlanes.Clear();
						intersectingPlanes.Add(planeIndex1);
						intersectingPlanes.Add(planeIndex2);
						intersectingPlanes.Add(planeIndex3);

						for (int planeIndex4 = 0; planeIndex4 < Planes.Count; planeIndex4++)
						{
							if (planeIndex4 == planeIndex1 ||
								planeIndex4 == planeIndex2 ||
								planeIndex4 == planeIndex3)
								continue;

							var plane4	= Planes[planeIndex4];
							var side	= plane4.OnSide(vertex);
							if (side == Side.Intersects)
							{
								if (planeIndex4 < planeIndex3)
									// Already found this vertex
									goto SkipIntersection;

								intersectingPlanes.Add(planeIndex4);
							} else
							if (side == Side.Outside)
								// Intersection is outside of brush
								goto SkipIntersection;
						}

						pointIntersections.Add(new PointIntersection(vertex, intersectingPlanes));

					SkipIntersection:
						;
					}
				}
			}

			var foundPlanes	= new int[2] { };
			for (int i = 0; i < pointIntersections.Count; i++)
			{
				var pointIntersectionA		= pointIntersections[i];
				for (int j = i + 1; j < pointIntersections.Count; j++)
				{
					var pointIntersectionB	= pointIntersections[j];
					var planesIndicesA		= pointIntersectionA.PlaneIndices;
					var planesIndicesB		= pointIntersectionB.PlaneIndices;

					int	foundPlaneIndex = 0;
					foreach (var currentPlaneIndex in planesIndicesA)
					{
						if (!planesIndicesB.Contains(currentPlaneIndex))
							continue;

						foundPlanes[foundPlaneIndex] = currentPlaneIndex;
						foundPlaneIndex++;

						if (foundPlaneIndex == 2)
							break;
					}

					if (foundPlaneIndex < 2)
						continue;
					
					var edge = new EdgeIntersection(
										pointIntersectionA, 
										pointIntersectionB, 
										Planes[ foundPlanes[0] ], 
										Planes[ foundPlanes[1] ]);
					pointIntersectionA.Edges.Add(edge);
					pointIntersectionB.Edges.Add(edge);
				}
			}

			for (int i = 0; i < pointIntersections.Count; i++)
			{
				var pointIntersection	= pointIntersections[i];
				var edges				= pointIntersection.Edges;

				for (int j = 0; j < edges.Count - 1; j++)
				{
					var edge1		= edges[j];
					for (int k = j + 1; k < edges.Count; k++)
					{
						var edge2	= edges[k];

						int planeIndex1		= -1;
						int planeIndex2		= -1;
						int intersectIndex1 = -1;
						int intersectIndex2 = -1;

						if (edge1.Planes[0] == edge2.Planes[0]) { planeIndex1 = 0; planeIndex2 = 0; } else
						if (edge1.Planes[0] == edge2.Planes[1]) { planeIndex1 = 0; planeIndex2 = 1; } else
						if (edge1.Planes[1] == edge2.Planes[0]) { planeIndex1 = 1; planeIndex2 = 0; } else
						if (edge1.Planes[1] == edge2.Planes[1]) { planeIndex1 = 1; planeIndex2 = 1; } else 
							continue;

						if (edge1.Intersections[0] == edge2.Intersections[0]) { intersectIndex1 = 0; intersectIndex2 = 0; } else
						if (edge1.Intersections[0] == edge2.Intersections[1]) { intersectIndex1 = 0; intersectIndex2 = 1; } else
						if (edge1.Intersections[1] == edge2.Intersections[0]) { intersectIndex1 = 1; intersectIndex2 = 0; } else
						if (edge1.Intersections[1] == edge2.Intersections[1]) { intersectIndex1 = 1; intersectIndex2 = 1; } else 
							continue;

						HalfEdge ingoing;
						HalfEdge outgoing;

						var normal1		= edge1.Planes[    planeIndex1].Normal;
						var normal2		= edge1.Planes[1 - planeIndex1].Normal;
						var normal3		= edge2.Planes[1 - planeIndex2].Normal;

						var direction	= normal1.CrossProduct(normal2);					
						if (direction.Dotproduct(normal3) < 0) // edge1 is ingoing
						{
							ingoing		= edge2.Edges[    intersectIndex2];
							outgoing	= edge1.Edges[1 - intersectIndex1];
						} else
						{
							ingoing		= edge1.Edges[    intersectIndex1];
							outgoing	= edge2.Edges[1 - intersectIndex2];
						}

						ingoing.Next			= outgoing;

						var polygon				= polygons[planeIndex1];
						ingoing.Polygon			= polygon;
						ingoing.Polygon.First	= ingoing;

						outgoing.Polygon		= polygon;
						outgoing.Polygon.First	= outgoing;
					}
				}
			}

			return mesh;
		}
	}
}
