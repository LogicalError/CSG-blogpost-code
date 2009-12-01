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
