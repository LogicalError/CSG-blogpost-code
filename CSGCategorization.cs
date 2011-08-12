using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RealtimeCSG
{
	public static class CSGCategorization
	{
		// Categorize the given inputPolygons as being inside/outside or (reverse-)aligned 
		// with the shape that is defined by the current brush or csg-branch.
		// When an inputPolygon crosses the node, it is split into pieces and every individual
		// piece is then categorized.
		#region Categorize
		public static void Categorize(CSGNode			processedNode,
									  CSGMesh			processedMesh,
								 	  
									  CSGNode			categorizationNode,

									  List<Polygon>		inputPolygons,

									  List<Polygon>		inside,
									  List<Polygon>		aligned,
									  List<Polygon>		revAligned,
									  List<Polygon>		outside)
		{
			// When you go deep enough in the tree it's possible that all categories point to the same
			// destination. So we detect that and potentially avoid a lot of wasted work.
			if (inside == revAligned &&
				inside == aligned &&
				inside == outside)
			{
				inside.AddRange(inputPolygons);
				return;
			}

		Restart:
			if (processedNode == categorizationNode)
			{
				// When the currently processed node is the same node as we categorize against, then
				// we know that all our polygons are visible and we set their default category
				// (usually aligned, unless it's an instancing node in which case it's precalculated)
				foreach (var polygon in inputPolygons)
				{
					switch (polygon.Category)
					{
						case PolygonCategory.Aligned:			aligned.Add(polygon); break;
						case PolygonCategory.ReverseAligned:	revAligned.Add(polygon); break;
						case PolygonCategory.Inside:			inside.Add(polygon); break;
						case PolygonCategory.Outside:			outside.Add(polygon); break;
					}

					// When brushes overlap and they share the same surface area we only want to keep
					// the polygons of the last brush in the tree, and skip all others.
					// At this point in the tree we know that this polygon belongs to this brush, so
					// we set it to visible. If the polygon is found to share the surface area with another
					// brush further on in the tree it'll be set to invisible again in mesh.Intersect.
					polygon.Visible = true;
				}
				return;
			}

			var leftNode	= categorizationNode.Left;
			var rightNode	= categorizationNode.Right;

			switch (categorizationNode.NodeType)
			{
				case CSGNodeType.Brush:
				{	
					processedMesh.Intersect( categorizationNode.Bounds, 
											 categorizationNode.Planes, 
											 categorizationNode.Translation, 
											 processedNode.Translation, 
								   
											 inputPolygons, 
								   
											 inside, aligned, revAligned, outside);
					break;
				}

				case CSGNodeType.Addition:
				{
					//  ( A ||  B)
					var relativeLeftTrans	= Vector3.Subtract(processedNode.Translation, leftNode.Translation);
					var relativeRightTrans	= Vector3.Subtract(processedNode.Translation, rightNode.Translation);
					if (AABB.IsOutside(processedNode.Bounds, relativeLeftTrans, leftNode.Bounds))
					{
						if (AABB.IsOutside(processedNode.Bounds, relativeRightTrans, rightNode.Bounds))
						{
							// When our polygons lie outside the bounds of both the left and the right node, then
							// all the polygons can be categorized as being 'outside'
							outside.AddRange(inputPolygons);
						} else
						{
							//Categorize(processedNode, mesh, right,
							//           inputPolygons,
							//           inside, aligned, revAligned, outside);
							categorizationNode = rightNode;
							goto Restart;
						}
					} else
					if (AABB.IsOutside(processedNode.Bounds, relativeRightTrans, rightNode.Bounds))
					{
						//Categorize(processedNode, left, mesh,
						//           inputPolygons,
						//           inside, aligned, revAligned, outside);
						categorizationNode = leftNode;
						goto Restart;
					} else
					{
						LogicalOr(processedNode, processedMesh, categorizationNode, 
									inputPolygons,
									inside, aligned, revAligned, outside,
									false, false);
					}
					break;
				}

				case CSGNodeType.Common:
				{
					// !(!A || !B)
					var relativeLeftTrans	= Vector3.Subtract(processedNode.Translation, leftNode.Translation);
					var relativeRightTrans	= Vector3.Subtract(processedNode.Translation, rightNode.Translation);
					if (AABB.IsOutside(processedNode.Bounds, relativeLeftTrans, leftNode.Bounds) ||
						AABB.IsOutside(processedNode.Bounds, relativeRightTrans, rightNode.Bounds))
					{
						// When our polygons lie outside the bounds of both the left and the right node, then
						// all the polygons can be categorized as being 'outside'
						outside.AddRange(inputPolygons);
					} else
					{
						LogicalOr(processedNode, processedMesh, categorizationNode, 
									inputPolygons,
									outside, revAligned, aligned, inside,
									true, true);
					}
					break;
				}

				case CSGNodeType.Subtraction:
				{
					// !(!A ||  B)
					var relativeLeftTrans	= Vector3.Subtract(processedNode.Translation, leftNode.Translation);
					var relativeRightTrans	= Vector3.Subtract(processedNode.Translation, rightNode.Translation);
					if (AABB.IsOutside(processedNode.Bounds, relativeLeftTrans, leftNode.Bounds))
					{
						// When our polygons lie outside the bounds of both the left node, then
						// all the polygons can be categorized as being 'outside'
						outside.AddRange(inputPolygons);
					} else
					if (AABB.IsOutside(processedNode.Bounds, relativeRightTrans, rightNode.Bounds))
					{
						categorizationNode = leftNode;
						goto Restart;
					} else
					{
						LogicalOr(processedNode, processedMesh, categorizationNode, 
									inputPolygons,
									outside, revAligned, aligned, inside,
									true, false);
					}
					break;
				}
			}
		}
		#endregion

		// Logical OR set operation on polygons
 		//
		// Table showing final output from combination of categorization of left and right node
		//
		//                  | right node
		//                  | inside    aligned     r-aligned   outside
		// -----------------+------------------------------------------
		// left  inside     | I         I           I           I
		// node  aligned    | I         A           I           A
		//       r-aligned  | I         I           R           R
		//       outside    | I         A           R           O
		//
		// I = inside   A = aligned
		// O = outside  R = reverse aligned
		//
		#region LogicalOr
		static void LogicalOr(	CSGNode			processedNode,
								CSGMesh			processedMesh,
								
								CSGNode			categorizationNode,

								List<Polygon>	inputPolygons,

								List<Polygon>	inside,
								List<Polygon>	aligned,
								List<Polygon>	revAligned,
								List<Polygon>	outside,

								bool			inverseLeft,
								bool			inverseRight)
		{
			var leftNode		= categorizationNode.Left;
			var rightNode		= categorizationNode.Right;
			var defaultCapacity	= inputPolygons.Count / 2;

			// ... Allocations are ridiculously cheap in .NET, there is a garbage collection penalty however.
			// CSG can be performed without temporary buffers and recursion by using flags, 
			// which would increase performance and scalability (garbage collection interfers with parallelization).
			// It makes the code a lot harder to read however.
			var leftAligned		= new List<Polygon>(defaultCapacity);
			var leftRevAligned	= new List<Polygon>(defaultCapacity);
			var leftOutside		= new List<Polygon>(defaultCapacity);
			//var leftInside	= new List<Polygon>(defaultCapacity); // everything that's inside the left node 
																		  // is always part of the inside category

			// First categorize polygons in left path ...
			if (inverseLeft)
				Categorize(processedNode, processedMesh, leftNode,
							inputPolygons,
							leftOutside, leftRevAligned, leftAligned, inside);
			else
				Categorize(processedNode, processedMesh, leftNode,
							inputPolygons,
							inside, leftAligned, leftRevAligned, leftOutside);

			// ... Then categorize the polygons in the right path
			// Note that no single polygon will go into more than one of the Categorize methods below
			if (inverseRight)
			{
				if (leftAligned.Count > 0)
				{
					if (inside == aligned)
					{
						inside.AddRange(leftAligned);
					} else
						Categorize(processedNode, processedMesh, rightNode,
									leftAligned,
									aligned, inside, aligned, inside);
				}

				if (leftRevAligned.Count > 0)
				{
					if (inside == revAligned)
					{
						inside.AddRange(leftRevAligned);
					} else
						Categorize(processedNode, processedMesh, rightNode,
									leftRevAligned,
									revAligned, revAligned, inside, inside);
				}

				if (leftOutside.Count > 0)
				{
					Categorize(processedNode, processedMesh, rightNode,
								leftOutside,
								outside, revAligned, aligned, inside);
				}
			} else
			{
				if (leftAligned.Count > 0)
				{
					if (inside == aligned)
					{
						inside.AddRange(leftAligned);
					} else
						Categorize(processedNode, processedMesh, rightNode,
									leftAligned,
									inside, aligned, inside, aligned);
				}

				if (leftRevAligned.Count > 0)
				{
					if (inside == revAligned)
					{
						inside.AddRange(leftRevAligned);
					} else
						Categorize(processedNode, processedMesh, rightNode,
									leftRevAligned,
									inside, inside, revAligned, revAligned);
				}

				if (leftOutside.Count > 0)
				{
					Categorize(processedNode, processedMesh, rightNode,
								leftOutside,
								inside, aligned, revAligned, outside);
				}
			}
		}
		#endregion

		// Create meshes for a given number of nodes and perform CSG on these.
		#region ProcessCSGNodes
		// We cache our base meshes here
		static ConcurrentDictionary<CSGNode, CSGMesh> cachedBaseMeshes = new ConcurrentDictionary<CSGNode, CSGMesh>();
		public static ConcurrentDictionary<CSGNode, CSGMesh> ProcessCSGNodes(CSGNode root, IEnumerable<CSGNode> nodes)
		{
			var meshes = new ConcurrentDictionary<CSGNode, CSGMesh>();
			var buildMesh =
				(Action<CSGNode>)
					delegate(CSGNode node)
					{
						CSGMesh mesh;
						if (!cachedBaseMeshes.TryGetValue(node, out mesh))
						{
							// If the node we're performing csg on is a brush, we simply create the geometry from the planes
							// If the node is a more complicated node, we perform csg on it's child nodes and combine the
							// meshes that are created.
							// Note that right now we cache brushes per node, but we can improve on this by caching on
							// node type instead. Since lots of nodes will have the same geometry in real life and only
							// need to be created once. It won't help much in runtime performance considering they're 
							// cached anyway, but it'll save on memory usage.
							if (node.NodeType != CSGNodeType.Brush)
							{
								var childNodes	= CSGUtility.FindChildBrushes(node);
								var brushMeshes = ProcessCSGNodes(node, childNodes);
								mesh = CSGMesh.Combine(node.Translation, brushMeshes);
							} else
								mesh = CSGMesh.CreateFromPlanes(node.Planes);

							// Cache the mesh
							cachedBaseMeshes[node] = mesh;
						}

						// Clone the cached mesh so we can perform CSG on it.
						var clonedMesh = mesh.Clone();
						node.Bounds.Set(clonedMesh.Bounds);
						meshes[node] = clonedMesh;
					};

			var updateDelegate =
				(Action<KeyValuePair<CSGNode, CSGMesh>>)
					delegate(KeyValuePair<CSGNode, CSGMesh> item)
					{
						var processedNode		= item.Key;
						var processedMesh		= item.Value;

						var inputPolygons		= processedMesh.Polygons;
						var insidePolygons		= new List<Polygon>(inputPolygons.Count);
						var outsidePolygons		= new List<Polygon>(inputPolygons.Count);
						var alignedPolygons		= new List<Polygon>(inputPolygons.Count);
						var reversedPolygons	= new List<Polygon>(inputPolygons.Count);

						CSGCategorization.Categorize(processedNode,
													 processedMesh,

													 root,

													 inputPolygons,	// these are the polygons that are categorized

													 insidePolygons,
													 alignedPolygons,
													 reversedPolygons,
													 outsidePolygons
													);

						// Flag all non aligned polygons as being invisible, and store their categorizations
						// so we can use it if we instance this mesh.
						foreach (var polygon in insidePolygons)
						{
							polygon.Category = PolygonCategory.Inside;
							polygon.Visible = false;
						}

						foreach (var polygon in outsidePolygons)
						{
							polygon.Category = PolygonCategory.Outside;
							polygon.Visible = false;
						}
						
						foreach (var polygon in alignedPolygons)
							polygon.Category = PolygonCategory.Aligned;

						foreach (var polygon in reversedPolygons)
							polygon.Category = PolygonCategory.ReverseAligned;
					};


			//
			// Here we run build the meshes and perform csg on them either in serial or parallel
			//

			/*
			foreach (var node in nodes)
				buildMesh(node);
			CSGUtility.UpdateBounds(root);
			foreach (var item in meshes)
				updateDelegate(item);
			/*/

			Parallel.ForEach(nodes, buildMesh);
			
			CSGUtility.UpdateBounds(root);
			
			Parallel.ForEach(meshes, updateDelegate);
			
			//*/
			return meshes;
		}
		#endregion 
	}
}
