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
using System.Diagnostics;

namespace RealtimeCSG
{
	public sealed class Polygon
	{
		public HalfEdge		First;
		public Plane		Plane;
	
		// Note 1: This method should probably be turned into a static method instead
		//			(especially since the currently split polygon might end up being empty)
		// Note 2: This method is not optimized! Code is simplified for clarity!
		//			example: Plane.Distance / Plane.OnSide should be inlined manually
		public Polygon		Split(Plane cutPlane)
		{
			if (First == null)
				return null;

			HalfEdge	prev			= First;
			HalfEdge	current			= prev.Next;
			HalfEdge	next			= current.Next;
			HalfEdge	last			= next;
			HalfEdge	enterEdge		= null;
			HalfEdge	exitEdge		= null;
			
			Vector3f	prevVertex		= prev.Vertex;
			Vector3f	currentVertex	= current.Vertex;
			
			float		distance0		= cutPlane.Distance(prev.Vertex);
			float		distance1		= cutPlane.Distance(current.Vertex);
			
			Side		side0			= Plane.OnSide(distance0);
			Side		side1			= Plane.OnSide(distance1);

			do
			{
				prevVertex		= prev.Vertex;
				currentVertex	= current.Vertex;

				float	distance2	= cutPlane.Distance(next.Vertex);
				Side	side2		= Plane.OnSide(distance2);
				
/*			
				do we need to handle these?

					  outside
				 0       1
				 --------*....... intersect
						  \
						   2
					  inside
									 					 
					  outside
						   2
						  /
				 --------*....... intersect 
				 0       1
					  inside
						 
*/
				if (side0 != side1)
				{
					if (side0 == Side.Inside)			// inside - outside/intersect - ??
					{
						if (side1 != Side.Intersects)	// inside - outside - ??
						{
							/*
							edge01 exits:
						
								  outside
									 1
									 *
							 ......./........ intersect
								   /   
								  0     
								  inside
							*/
							// unoptimized code:
							Vector3f	vertex		= Plane.Intersection(prevVertex, currentVertex, distance0, distance1);
							HalfEdge	newEdge		= current.Insert(vertex);
							
							exitEdge = current;
							prev	 = prev.Next;
							current  = prev.Next;
							next	 = current.Next;

							distance0 = 0;
							side0 = Side.Intersects;

							if (enterEdge != null)
								break;
						} else
						if (side2 == Side.Outside)		// inside - intersect - outside
						{
							/*					
							find exit edge:
						
								  outside
							           2
									1 /
							 ........*....... intersect
									/ 
								   0   
								   inside
						
							 */
							exitEdge = current;
							if (enterEdge != null)
								break;
						} else							// inside - intersect - intersect/inside
						{
							/*					
							return null:
						
								  outside
									 1
							 ........*....... intersect
									/ \
								   0   2
								   inside
						
								  outside
									 1      2
							 ........*------- intersect
									/ 
								   0
								  inside
						 
							 */
							side0 = Side.Inside;
							enterEdge = exitEdge = null;
							break;
						}
					} else
					if (side0 == Side.Outside)			// outside - inside/intersect - ??
					{
						if (side1 != Side.Intersects)	// outside - inside - ??
						{
							/*
							edge01 enters:
						
								  outside
								  0	 
								   \ 
							 .......\........ intersect
								     *  
								     1   
								  inside
							*/
							Vector3f	vertex	= Plane.Intersection(prevVertex, currentVertex, distance0, distance1);
							HalfEdge	newEdge = current.Insert(vertex);

							enterEdge = current;
							prev	  = prev.Next;
							current   = prev.Next;
							next	  = current.Next;

							distance0 = 0;
							side0 = Side.Intersects;

							if (exitEdge != null)
								break;
						} else
						if (side2 == Side.Inside)		// outside - intersect - inside
						{
							/*					
							find enter edge:
						
								  outside
							       0    
									\ 1
							 ........*....... intersect
									  \
								       2  
								   inside
						
							 */
							enterEdge = current;
							if (exitEdge != null)
								break;
						} else							// outside - intersect - intersect/outside
						{							
							/*						 
							return full face:
				
								  outside
								   0   2
									\ /
							 ........*....... intersect
									 1
								  inside
					 					 					 
								  outside
								   0
									\ 
							 ........*------- intersect
									 1      2
								  inside

							*/
							side0 = Side.Outside;
							enterEdge = exitEdge = null;
							break;
						}
					}
				}

				prev = current;
				current = next;
				next = next.Next;
				distance0 = distance1;
				distance1 = distance2;
				side0 = side1;
				side1 = side2;
			} while (next != last);

			// We should never have only one edge crossing the plane ..
			Debug.Assert((enterEdge == null) == (exitEdge == null));
			
			if (enterEdge != null && exitEdge != null)
			{
				/*
				  enter   .
				          .
					=====>*----->
				          .
				  outside . inside
				          .
					<-----*<=====
				          .
				          .  exit
				*/
				Polygon	 newPolygon	 = new Polygon();
				HalfEdge outsideEdge = new HalfEdge();
				HalfEdge insideEdge  = new HalfEdge();
				

				outsideEdge.Twin	= insideEdge;
				insideEdge.Twin		= outsideEdge;

				insideEdge.Polygon  = this;
				outsideEdge.Polygon = newPolygon;

				outsideEdge.Vertex	= exitEdge.Vertex;
				insideEdge.Vertex	= enterEdge.Vertex;

				outsideEdge.Next	= exitEdge.Next;
				insideEdge.Next		= enterEdge.Next;

				exitEdge.Next		= insideEdge;
				enterEdge.Next		= outsideEdge;

				newPolygon.First	= outsideEdge;
				this.First			= insideEdge;

				HalfEdge iterator = newPolygon.First;
				do
				{
					iterator.Polygon = newPolygon;
					iterator = iterator.Next;
				} while (iterator != newPolygon.First);
				return newPolygon;
			} else
			{
				if (side0 != Side.Outside)
					return null;

				Polygon newPolygon = new Polygon();
				newPolygon.First = First;
				HalfEdge iterator = First;
				do
				{
					iterator.Polygon = newPolygon;
					iterator = iterator.Next;
				} while (iterator != First);
				
				this.First = null;
				return newPolygon;
			}
		}
	}
}
