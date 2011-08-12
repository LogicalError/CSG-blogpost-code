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
using System.Diagnostics;

namespace RealtimeCSG
{
	public class CSGMesh
	{
		public readonly AABB			Bounds = new AABB();
		public readonly List<Polygon>	Polygons;
		public readonly List<HalfEdge>	Edges;
		public readonly List<Vector3>	Vertices;
		public readonly Plane[]			Planes;

		#region Constructors
		public CSGMesh(Plane[] planes, List<Polygon> polygons, List<HalfEdge> edges, List<Vector3> vertices, AABB bounds)
		{
			this.Planes = planes;
			this.Polygons = polygons;
			this.Edges = edges;
			this.Vertices = vertices;
			this.Bounds.Set(bounds);
		}
		#endregion

		// Creates a clone of the mesh
		#region Clone
		public CSGMesh Clone()
		{
			var newPlanes = new Plane[Planes.Length];
			for (int i = 0; i < Planes.Length; i++)
			{
				var plane = Planes[i];
				newPlanes[i] = new Plane(plane.A, plane.B, plane.C, plane.D);
			}
			var newPolygons = new List<Polygon>(Polygons.Count);
			foreach (var polygon in Polygons)
			{
				var newPolygon = new Polygon();
				newPolygon.FirstIndex = polygon.FirstIndex;
				newPolygon.Visible = polygon.Visible;
				newPolygon.Category = polygon.Category;
				newPolygon.PlaneIndex = polygon.PlaneIndex;
				newPolygon.Bounds.Set(polygon.Bounds);

				newPolygons.Add(newPolygon);
			}

			var newEdges = new List<HalfEdge>(Edges.Count);
			foreach (var edge in Edges)
			{
				var newEdge = new HalfEdge();
				newEdge.NextIndex = edge.NextIndex;
				newEdge.PolygonIndex = edge.PolygonIndex;
				newEdge.TwinIndex = edge.TwinIndex;
				newEdge.VertexIndex = edge.VertexIndex;
				newEdges.Add(newEdge);
			}

			var newVertices = new List<Vector3>(Vertices.Count);
			foreach (var vertex in Vertices)
			{
				var newVertex = new Vector3(vertex.X, vertex.Y, vertex.Z);
				newVertices.Add(newVertex);
			}

			var newBounds = new AABB(Bounds);
			var newMesh = new CSGMesh(
				newPlanes,
				newPolygons,
				newEdges,
				newVertices,
				newBounds);

			return newMesh;
		}
		#endregion

		// Creates a mesh from a brush (set of planes)
		#region CreateFromPlanes

		#region Helper classes

		#region EdgeIntersection
		sealed class EdgeIntersection
		{
			public EdgeIntersection(HalfEdge edge, short planeIndexA, short planeIndexB)
			{

				PlaneIndices[0] = planeIndexA;
				PlaneIndices[1] = planeIndexB;

				Edge = edge;
			}

			public short[] PlaneIndices = new short[2];
			public HalfEdge Edge;
		}
		#endregion

		#region PointIntersection
		sealed class PointIntersection
		{
			public PointIntersection(short vertexIndex, List<short> planes)
			{
				VertexIndex = vertexIndex;
				foreach (var plane in planes)
					PlaneIndices.Add(plane);
			}

			public readonly List<EdgeIntersection> Edges = new List<EdgeIntersection>();
			public readonly HashSet<short> PlaneIndices = new HashSet<short>();
			public readonly short VertexIndex;
		}
		#endregion

		#endregion

		public static CSGMesh CreateFromPlanes(Plane[] brushPlanes)
		{
			var planes = new Plane[brushPlanes.Length];
			for (int i = 0; i < brushPlanes.Length; i++)
			{
				var plane = brushPlanes[i];
				planes[i] = new Plane(plane.A, plane.B, plane.C, plane.D);
			}

			var pointIntersections = new List<PointIntersection>(planes.Length * planes.Length);
			var intersectingPlanes = new List<short>();
			var vertices = new List<Vector3>();
			var edges = new List<HalfEdge>();

			// Find all point intersections where 3 (or more planes) intersect
			for (short planeIndex1 = 0; planeIndex1 < planes.Length - 2; planeIndex1++)
			{
				var plane1 = planes[planeIndex1];
				for (short planeIndex2 = (short)(planeIndex1 + 1); planeIndex2 < planes.Length - 1; planeIndex2++)
				{
					var plane2 = planes[planeIndex2];
					for (short planeIndex3 = (short)(planeIndex2 + 1); planeIndex3 < planes.Length; planeIndex3++)
					{
						var plane3 = planes[planeIndex3];

						// Calculate the intersection
						var vertex = Plane.Intersection(plane1, plane2, plane3);

						// Check if the intersection is valid
						if (float.IsNaN(vertex.X) || float.IsNaN(vertex.Y) || float.IsNaN(vertex.Z) ||
							float.IsInfinity(vertex.X) || float.IsInfinity(vertex.Y) || float.IsInfinity(vertex.Z))
							continue;

						intersectingPlanes.Clear();
						intersectingPlanes.Add(planeIndex1);
						intersectingPlanes.Add(planeIndex2);
						intersectingPlanes.Add(planeIndex3);

						for (short planeIndex4 = 0; planeIndex4 < planes.Length; planeIndex4++)
						{
							if (planeIndex4 == planeIndex1 ||
								planeIndex4 == planeIndex2 ||
								planeIndex4 == planeIndex3)
								continue;

							var plane4 = planes[planeIndex4];
							var side = plane4.OnSide(vertex);
							if (side == PlaneSideResult.Intersects)
							{
								if (planeIndex4 < planeIndex3)
									// Already found this vertex
									goto SkipIntersection;

								// We've found another plane which goes trough our found intersection point
								intersectingPlanes.Add(planeIndex4);
							}
							else
								if (side == PlaneSideResult.Outside)
									// Intersection is outside of brush
									goto SkipIntersection;
						}

						var vertexIndex = (short)vertices.Count;
						vertices.Add(vertex);

						// Add intersection point to our list
						pointIntersections.Add(new PointIntersection(vertexIndex, intersectingPlanes));

					SkipIntersection:
						;
					}
				}
			}

			var foundPlanes = new short[2];
			// Find all our intersection edges which are formed by a pair of planes
			// (this could probably be done inside the previous loop)
			for (int i = 0; i < pointIntersections.Count; i++)
			{
				var pointIntersectionA = pointIntersections[i];
				for (int j = i + 1; j < pointIntersections.Count; j++)
				{
					var pointIntersectionB = pointIntersections[j];
					var planesIndicesA = pointIntersectionA.PlaneIndices;
					var planesIndicesB = pointIntersectionB.PlaneIndices;

					short foundPlaneIndex = 0;
					foreach (var currentPlaneIndex in planesIndicesA)
					{
						if (!planesIndicesB.Contains(currentPlaneIndex))
							continue;

						foundPlanes[foundPlaneIndex] = currentPlaneIndex;
						foundPlaneIndex++;

						if (foundPlaneIndex == 2)
							break;
					}

					// If foundPlaneIndex is 0 or 1 then either this combination does not exist, 
					// or only goes trough one point 
					if (foundPlaneIndex < 2)
						continue;

					// Create our found intersection edge
					var halfEdgeA = new HalfEdge();
					var halfEdgeAIndex = (short)edges.Count;
					edges.Add(halfEdgeA);

					var halfEdgeB = new HalfEdge();
					var halfEdgeBIndex = (short)edges.Count;
					edges.Add(halfEdgeB);

					halfEdgeA.TwinIndex = halfEdgeBIndex;
					halfEdgeB.TwinIndex = halfEdgeAIndex;

					halfEdgeA.VertexIndex = pointIntersectionA.VertexIndex;
					halfEdgeB.VertexIndex = pointIntersectionB.VertexIndex;

					// Add it to our points
					pointIntersectionA.Edges.Add(new EdgeIntersection(
														halfEdgeA,
														foundPlanes[0],
														foundPlanes[1]));
					pointIntersectionB.Edges.Add(new EdgeIntersection(
														halfEdgeB,
														foundPlanes[0],
														foundPlanes[1]));
				}
			}


			var polygons = new List<Polygon>();
			for (short i = 0; i < (short)planes.Length; i++)
			{
				var polygon = new Polygon();
				polygon.PlaneIndex = i;
				polygons.Add(polygon);
			}

			var bounds = new AABB();
			var direction = new Vector3();
			for (int i = pointIntersections.Count - 1; i >= 0; i--)
			{
				var pointIntersection = pointIntersections[i];
				var pointEdges = pointIntersection.Edges;

				// Make sure that we have at least 2 edges ...
				// This may happen when a plane only intersects at a single edge.
				if (pointEdges.Count <= 2)
				{
					pointIntersections.RemoveAt(i);
					continue;
				}

				var vertexIndex = pointIntersection.VertexIndex;
				var vertex = vertices[vertexIndex];

				for (int j = 0; j < pointEdges.Count - 1; j++)
				{
					var edge1 = pointEdges[j];
					for (int k = j + 1; k < pointEdges.Count; k++)
					{
						var edge2 = pointEdges[k];

						int planeIndex1 = -1;
						int planeIndex2 = -1;

						// Determine if and which of our 2 planes are identical
						if (edge1.PlaneIndices[0] == edge2.PlaneIndices[0]) { planeIndex1 = 0; planeIndex2 = 0; }
						else
							if (edge1.PlaneIndices[0] == edge2.PlaneIndices[1]) { planeIndex1 = 0; planeIndex2 = 1; }
							else
								if (edge1.PlaneIndices[1] == edge2.PlaneIndices[0]) { planeIndex1 = 1; planeIndex2 = 0; }
								else
									if (edge1.PlaneIndices[1] == edge2.PlaneIndices[1]) { planeIndex1 = 1; planeIndex2 = 1; }
									else
										continue;

						HalfEdge ingoing;
						HalfEdge outgoing;
						short outgoingIndex;

						var shared_plane = planes[edge1.PlaneIndices[planeIndex1]];
						var edge1_plane = planes[edge1.PlaneIndices[1 - planeIndex1]];
						var edge2_plane = planes[edge2.PlaneIndices[1 - planeIndex2]];

						direction = Vector3.CrossProduct(shared_plane.Normal, edge1_plane.Normal);

						// Determine the orientation of our two edges to determine 
						// which edge is in-going, and which one is out-going
						if (Vector3.DotProduct(direction, edge2_plane.Normal) < 0)
						{
							ingoing = edge2.Edge;
							outgoingIndex = edge1.Edge.TwinIndex;
							outgoing = edges[outgoingIndex];
						}
						else
						{
							ingoing = edge1.Edge;
							outgoingIndex = edge2.Edge.TwinIndex;
							outgoing = edges[outgoingIndex];
						}


						// Link the out-going half-edge to the in-going half-edge
						ingoing.NextIndex = outgoingIndex;


						// Add reference to polygon to half-edge, and make sure our 
						// polygon has a reference to a half-edge 
						// Since a half-edge, in this case, serves as a circular 
						// linked list this just works.
						var polygonIndex = edge1.PlaneIndices[planeIndex1];

						ingoing.PolygonIndex = polygonIndex;
						outgoing.PolygonIndex = polygonIndex;

						var polygon = polygons[polygonIndex];
						polygon.FirstIndex = outgoingIndex;
						polygon.Bounds.Add(vertex);
					}
				}

				// Add the intersection point to the area of our bounding box
				bounds.Add(vertex);
			}

			return new CSGMesh(planes, polygons, edges, vertices, bounds);
		}
		#endregion

		// Splits a half edge
		#region EdgeSplit
		HalfEdge EdgeSplit(HalfEdge edge, Vector3 vertex)
		{
			/*
			  original:
			 
						edge
			 *<====================== 
			  ---------------------->*
						twin
			 
			 split:
						 
				newEdge		thisEdge
			 *<=========*<=========== 
			  --------->*----------->*
				thisTwin	newTwin
			 
			*/

			var thisEdge = edge;
			var thisTwinIndex = edge.TwinIndex;
			var thisTwin = Edges[thisTwinIndex];
			var thisEdgeIndex = thisTwin.TwinIndex;

			var newEdge = new HalfEdge();
			var newEdgeIndex = (short)Edges.Count;

			var newTwin = new HalfEdge();
			var newTwinIndex = (short)(newEdgeIndex + 1);
			var vertexIndex = (short)Vertices.Count;

			newEdge.PolygonIndex = thisEdge.PolygonIndex;
			newTwin.PolygonIndex = thisTwin.PolygonIndex;

			newEdge.VertexIndex = thisEdge.VertexIndex;
			thisEdge.VertexIndex = vertexIndex;

			newTwin.VertexIndex = thisTwin.VertexIndex;
			thisTwin.VertexIndex = vertexIndex;

			newEdge.NextIndex = thisEdge.NextIndex;
			thisEdge.NextIndex = newEdgeIndex;

			newTwin.NextIndex = thisTwin.NextIndex;
			thisTwin.NextIndex = newTwinIndex;

			newEdge.TwinIndex = thisTwinIndex;
			thisTwin.TwinIndex = newEdgeIndex;

			thisEdge.TwinIndex = newTwinIndex;
			newTwin.TwinIndex = thisEdgeIndex;

			Edges.Add(newEdge);
			Edges.Add(newTwin);
			Vertices.Add(vertex);
			return newEdge;
		}
		#endregion

		// Splits a polygon into two pieces, or categorizes it as outside, inside or aligned
		#region PolygonSplit
		// Note: This method is not optimized! Code is simplified for clarity!
		//		  for example: Plane.Distance / Plane.OnSide should be inlined manually and shouldn't use enums, but floating point values directly!
		public PolygonSplitResult PolygonSplit(Plane cuttingPlane, Vector3 translation, ref Polygon inputPolygon, out Polygon outsidePolygon)
		{
			HalfEdge prev		= Edges[inputPolygon.FirstIndex];
			HalfEdge current	= Edges[prev.NextIndex];
			HalfEdge next		= Edges[current.NextIndex];
			HalfEdge last		= next;
			HalfEdge enterEdge	= null;
			HalfEdge exitEdge	= null;

			var prevVertex			= Vertices[prev.VertexIndex];
			var prevDistance		= cuttingPlane.Distance(prevVertex);		// distance to previous vertex
			var prevSide			= Plane.OnSide(prevDistance);				// side of plane of previous vertex

			var currentVertex		= Vertices[current.VertexIndex];
			var currentDistance		= cuttingPlane.Distance(currentVertex);		// distance to current vertex
			var currentSide			= Plane.OnSide(currentDistance);			// side of plane of current vertex

			do
			{
				var nextVertex		= Vertices[next.VertexIndex];
				var nextDistance	= cuttingPlane.Distance(nextVertex);		// distance to next vertex
				var nextSide		= Plane.OnSide(nextDistance);				// side of plane of next vertex

				if (prevSide != currentSide)							// check if edge crossed the plane ...
				{
					if (currentSide != PlaneSideResult.Intersects)		// prev:inside/outside - current:inside/outside - next:??
					{
						if (prevSide != PlaneSideResult.Intersects)		// prev:inside/outside - current:outside        - next:??
						{
							// Calculate intersection of edge with plane split the edge into two, inserting the new vertex
							var newVertex	= Plane.Intersection(prevVertex, currentVertex, prevDistance, currentDistance);
							var newEdge		= EdgeSplit(current, newVertex);

							if (prevSide == PlaneSideResult.Inside)		// prev:inside         - current:outside        - next:??
							{
								//edge01 exits:
								//						
								//      outside
								//         1
								//         *
								// ......./........ intersect
								//       /   
								//      0     
								//      inside

								exitEdge		= current;
							} else
							if (prevSide == PlaneSideResult.Outside)		// prev:outside - current:inside - next:??
							{
								//edge01 enters:
								//
								//      outside
								//      0	 
								//       \ 
								// .......\........ intersect
								//         *  
								//         1   
								//      inside

								enterEdge		= current;
							}

							prevDistance	= 0;
							prev			= Edges[prev.NextIndex];
							prevSide		= PlaneSideResult.Intersects;

							if (exitEdge != null &&
								enterEdge != null)
								break;

							current			= Edges[prev.NextIndex];
							currentVertex	= Vertices[current.VertexIndex];

							next			= Edges[current.NextIndex];
							nextVertex		= Vertices[next.VertexIndex];
						}
					} else												// prev:??                - current:intersects - next:??
					{
						if (prevSide == PlaneSideResult.Intersects ||	// prev:intersects        - current:intersects - next:??
							nextSide == PlaneSideResult.Intersects ||	// prev:??                - current:intersects - next:intersects
							prevSide == nextSide)						// prev:inside/outde      - current:intersects - next:inside/outde
						{
							if (prevSide == PlaneSideResult.Inside ||	// prev:inside            - current:intersects - next:intersects/inside
								nextSide == PlaneSideResult.Inside)		// prev:intersects/inside - current:intersects - next:inside
							{
								//      outside
								// 0       1
								// --------*....... intersect
								//          \
								//           2
								//       inside
								//
								//      outside
								//         1      2
								// ........*------- intersect
								//        / 
								//       0
								//      inside
								//
								//     outside
								//        1
								//........*....... intersect
								//       / \
								//      0   2
								//      inside
								//

								prevSide = PlaneSideResult.Inside;
								enterEdge = exitEdge = null;
								break;
							} else
							if (prevSide == PlaneSideResult.Outside ||		// prev:outside            - current:intersects - next:intersects/outside
								nextSide == PlaneSideResult.Outside)		// prev:intersects/outside - current:intersects - next:outside
							{
								//     outside
								//          2
								//         /
								//..------*....... intersect
								//  0     1
								//     inside
								//					 
								//     outside
								//      0
								//       \ 
								//........*------- intersect
								//        1      2
								//     inside
								//
								//     outside
								//      0   2
								//       \ /
								//........*....... intersect
								//        1
								//     inside
								//					 

								prevSide = PlaneSideResult.Outside;
								enterEdge = exitEdge = null;
								break;
							}
						} else											// prev:inside/outside - current:intersects - next:inside/outside
						{
							if (prevSide == PlaneSideResult.Inside)		// prev:inside         - current:intersects - next:outside
							{
								//find exit edge:
								//
								//      outside
								//           2
								//        1 /
								// ........*....... intersect
								//        / 
								//       0   
								//       inside

								exitEdge = current;
								if (enterEdge != null)
									break;
							} else										// prev:outside        - current:intersects - next:inside
							{
								//find enter edge:
								//
								//      outside
								//       0    
								//        \ 1
								// ........*....... intersect
								//          \
								//           2  
								//       inside

								enterEdge = current;
								if (exitEdge != null)
									break;
							}
						}
					}
				}

				prev	= current;
				current = next;
				next	= Edges[next.NextIndex];

				prevDistance	= currentDistance;
				currentDistance = nextDistance;
				prevSide		= currentSide;
				currentSide		= nextSide;
				prevVertex		= currentVertex;
				currentVertex	= nextVertex;
			} while (next != last);

			// We should never have only one edge crossing the plane ..
			Debug.Assert((enterEdge == null) == (exitEdge == null));

			// Check if we have an edge that exits and an edge that enters the plane and split the polygon into two if we do
			if (enterEdge != null && exitEdge != null)
			{
				//enter   .
				//        .
				//  =====>*----->
				//        .
				//
				//outside . inside
				//        .
				//  <-----*<=====
				//        .
				//        .  exit

				outsidePolygon = new Polygon();
				var outsidePolygonIndex = (short)this.Polygons.Count;
				this.Polygons.Add(outsidePolygon);

				var outsideEdge			= new HalfEdge();
				var outsideEdgeIndex	= (short)Edges.Count;

				var insideEdge			= new HalfEdge();
				var insideEdgeIndex		= (short)(outsideEdgeIndex + 1);

				outsideEdge.TwinIndex		= insideEdgeIndex;
				insideEdge.TwinIndex		= outsideEdgeIndex;

				//insideEdge.PolygonIndex	= inputPolygonIndex;// index does not change
				outsideEdge.PolygonIndex	= outsidePolygonIndex;

				outsideEdge.VertexIndex		= exitEdge.VertexIndex;
				insideEdge.VertexIndex		= enterEdge.VertexIndex;

				outsideEdge.NextIndex		= exitEdge.NextIndex;
				insideEdge.NextIndex		= enterEdge.NextIndex;

				exitEdge.NextIndex			= insideEdgeIndex;
				enterEdge.NextIndex			= outsideEdgeIndex;

				outsidePolygon.FirstIndex	= outsideEdgeIndex;
				inputPolygon.FirstIndex		= insideEdgeIndex;

				outsidePolygon.Visible		= inputPolygon.Visible;
				outsidePolygon.Category		= inputPolygon.Category;
				outsidePolygon.PlaneIndex	= inputPolygon.PlaneIndex;

				Edges.Add(outsideEdge);
				Edges.Add(insideEdge);


				// calculate the bounds of the polygons
				outsidePolygon.Bounds.Clear();
				var first = Edges[outsidePolygon.FirstIndex];
				var iterator = first;
				do
				{
					outsidePolygon.Bounds.Add(Vertices[iterator.VertexIndex]);
					iterator.PolygonIndex = outsidePolygonIndex;
					iterator = Edges[iterator.NextIndex];
				} while (iterator != first);

				inputPolygon.Bounds.Clear();
				first = Edges[inputPolygon.FirstIndex];
				iterator = first;
				do
				{
					inputPolygon.Bounds.Add(Vertices[iterator.VertexIndex]);
					iterator = Edges[iterator.NextIndex];
				} while (iterator != first);

				return PolygonSplitResult.Split;
			} else
			{
				outsidePolygon = null;
				switch (prevSide)
				{
					case PlaneSideResult.Inside:	return PolygonSplitResult.CompletelyInside;
					case PlaneSideResult.Outside:	return PolygonSplitResult.CompletelyOutside;
					default:
					case PlaneSideResult.Intersects:
					{
						var polygonPlane = Planes[inputPolygon.PlaneIndex];
						var result = Vector3.DotProduct(polygonPlane.Normal, cuttingPlane.Normal);
						if (result > 0)
							return PolygonSplitResult.PlaneAligned;
						else
							return PolygonSplitResult.PlaneOppositeAligned;
					}
				}
			}
		}
		#endregion

		// Intersects a mesh with a brush (set of planes)
		#region Intersect
		public void Intersect(AABB		cuttingNodeBounds,
							  Plane[]	cuttingNodePlanes,
							  Vector3	cuttingNodeTranslation,
							  Vector3	inputPolygonTranslation,

							  List<Polygon> inputPolygons,

							  List<Polygon> inside,
							  List<Polygon> aligned,
							  List<Polygon> revAligned,
							  List<Polygon> outside)
		{
			var categories			= new PolygonSplitResult[cuttingNodePlanes.Length];
			var translatedPlanes	= new Plane[cuttingNodePlanes.Length];
			var translation			= Vector3.Subtract(cuttingNodeTranslation, inputPolygonTranslation);

			// translate the planes we cut our polygons with so that they're located at the same 
			// relative distance from the polygons as the brushes are from each other.
			for (int i = 0; i < cuttingNodePlanes.Length; i++)
				translatedPlanes[i] = Plane.Translated(cuttingNodePlanes[i], translation);

			var vertices = this.Vertices;
			var edges = this.Edges;
			var planes = this.Planes;
			for (int i = inputPolygons.Count - 1; i >= 0; i--)
			{
				var inputPolygon = inputPolygons[i];
				if (inputPolygon.FirstIndex == -1)
					continue;

				var bounds		= inputPolygon.Bounds;	
				var finalResult = PolygonSplitResult.CompletelyInside;

				// A quick check if the polygon lies outside the planes we're cutting our polygons with.
				if (!AABB.IsOutside(cuttingNodeBounds, translation, bounds))
				{
					PolygonSplitResult	intermediateResult;
					Polygon				outsidePolygon = null;
					for (int otherIndex = 0; otherIndex < translatedPlanes.Length; otherIndex++)
					{
						var translatedCuttingPlane = translatedPlanes[otherIndex];

						var side = cuttingNodePlanes[otherIndex].OnSide(bounds, translation.Negated());
						if (side == PlaneSideResult.Outside)
						{
							finalResult = PolygonSplitResult.CompletelyOutside;
							break;	// nothing left to process, so we exit
						} else
						if (side == PlaneSideResult.Inside)
							continue;

						var polygon = inputPolygon;
						intermediateResult = PolygonSplit(translatedCuttingPlane, inputPolygonTranslation, ref polygon, out outsidePolygon);
						inputPolygon = polygon;

						if (intermediateResult == PolygonSplitResult.CompletelyOutside)
						{
							finalResult = PolygonSplitResult.CompletelyOutside;
							break;	// nothing left to process, so we exit
						} else
						if (intermediateResult == PolygonSplitResult.Split)
						{
							if (outside != null)
								outside.Add(outsidePolygon);
							// Note: left over is still completely inside, 
							//		 or plane (opposite) aligned
						} else
						if (intermediateResult != PolygonSplitResult.CompletelyInside)
							finalResult = intermediateResult;
					}
				} else
					finalResult = PolygonSplitResult.CompletelyOutside;

				switch (finalResult)
				{
					case PolygonSplitResult.CompletelyInside:		inside .Add(inputPolygon); break;
					case PolygonSplitResult.CompletelyOutside:		outside.Add(inputPolygon); break;

					// The polygon can only be visible if it's part of the last brush that shares it's surface area, 
					// otherwise we'd get overlapping polygons if two brushes overlap.
					// When the (final) polygon is aligned with one of the cutting planes, we know it lies on the surface of
					// the CSG node we're cutting the polygons with. We also know that this node is not the node this polygon belongs to
					// because we've done that check earlier on. So we flag this polygon as being invisible.
					case PolygonSplitResult.PlaneAligned:			inputPolygon.Visible = false; aligned   .Add(inputPolygon); break;
					case PolygonSplitResult.PlaneOppositeAligned:	inputPolygon.Visible = false; revAligned.Add(inputPolygon); break;
				}
			}
		}
		#endregion

		// Combines multiple meshes into one
		#region Combine
		public static CSGMesh Combine(Vector3 offset, IDictionary<CSGNode, CSGMesh> brushMeshes)
		{
			var planeLookup = new Dictionary<Plane, short>();
			var vertexLookup = new Dictionary<Vector3, short>();

			var planes = new List<Plane>();
			var polygons = new List<Polygon>();
			var edges = new List<HalfEdge>();
			var vertices = new List<Vector3>();

			var bounds = new AABB();

			bounds.Clear();
			int edgeIndex = 0;
			int polygonIndex = 0;
			foreach (var item in brushMeshes)
			{
				var node = item.Key;
				var translation = Vector3.Subtract(node.Translation, offset);
				var mesh = item.Value;
				foreach (var edge in mesh.Edges)
				{
					short vertexIndex;
					var vertex = Vector3.Add(mesh.Vertices[edge.VertexIndex], translation);
					if (!vertexLookup.TryGetValue(vertex, out vertexIndex))
					{
						vertexIndex = (short)vertices.Count;
						vertices.Add(vertex);
						vertexLookup.Add(vertex, vertexIndex);
					}

					var newEdge = new HalfEdge();
					newEdge.VertexIndex = vertexIndex;
					newEdge.NextIndex = (short)(edge.NextIndex + edgeIndex);
					newEdge.TwinIndex = (short)(edge.TwinIndex + edgeIndex);
					newEdge.PolygonIndex = (short)(edge.PolygonIndex + polygonIndex);

					edges.Add(newEdge);
				}

				foreach (var polygon in mesh.Polygons)
				{
					if (polygon.FirstIndex == -1)
						continue;
					short planeIndex;
					var plane = mesh.Planes[polygon.PlaneIndex];
					if (!planeLookup.TryGetValue(plane, out planeIndex))
					{
						planeIndex = (short)planes.Count;
						planes.Add(plane);
						planeLookup.Add(plane, planeIndex);
					}

					var newPolygon = new Polygon();
					newPolygon.PlaneIndex = planeIndex;
					newPolygon.FirstIndex = (short)(polygon.FirstIndex + edgeIndex);
					newPolygon.Category = polygon.Category;
					newPolygon.Visible = polygon.Visible;
					newPolygon.Bounds.Set(polygon.Bounds, translation);

					polygons.Add(newPolygon);

					if (newPolygon.Visible)
					{
						var first = edges[newPolygon.FirstIndex];
						var iterator = first;
						do
						{
							bounds.Add(vertices[iterator.VertexIndex]);
							iterator = edges[iterator.NextIndex];
						} while (iterator != first);
					}
				}
				edgeIndex = edges.Count;
				polygonIndex = polygons.Count;
			}
			return new CSGMesh(planes.ToArray(), polygons, edges, vertices, bounds);
		}
		#endregion
	}
}
