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

namespace RealtimeCSG
{
	/*
			  ^
			  |			polygon
	next	  |
	half-edge |
			  |			half-edge
	vertex	  *<====================== 
			  ---------------------->*
					twin-half-edge
	*/
	public sealed class HalfEdge
	{
		public HalfEdge() { }
		public HalfEdge(Vector3f vertex) { Vertex = vertex; }

		public Vector3f		Vertex;
		public Polygon		Polygon;
		public HalfEdge		Twin;
		public HalfEdge		Next;

		public HalfEdge		Insert	(Vector3f newVertex)
		{
			/*
			  original:
			 
						this
			 *<====================== 
			  ---------------------->*
						twin
			 
			 split:
			 			 
				newEdge		thisEdge
			 *<=========*<=========== 
			  --------->*----------->*
				thisTwin	newTwin
			 
			*/
			
			HalfEdge thisEdge = this;
			HalfEdge thisTwin = this.Twin;
			HalfEdge newEdge  = new HalfEdge();
			HalfEdge newTwin  = new HalfEdge();

			newEdge.Polygon = thisEdge.Polygon;
			newTwin.Polygon = thisTwin.Polygon;

			newEdge.Vertex	= thisEdge.Vertex;
			thisEdge.Vertex = newVertex;
			
			newTwin.Vertex	= thisTwin.Vertex;
			thisTwin.Vertex	= newVertex;

			newEdge.Next	= thisEdge.Next;
			thisEdge.Next	= newEdge;
			
			newTwin.Next	= thisTwin.Next;
			thisTwin.Next	= newTwin;

			newEdge.Twin	= thisTwin;
			thisTwin.Twin	= newEdge;

			thisEdge.Twin	= newTwin;
			newTwin.Twin	= thisEdge;

			return newEdge;
		}
	}
}
