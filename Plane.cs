using System;

namespace RealtimeCSG
{
	public struct Plane
	{
		public float A;
		public float B;
		public float C;
		public float D;
		
		public Vector3f Normal       { get { return new Vector3f(A, B, C); } set { A = value.X; B = value.Y; C = value.Z; } }
		public Vector3f PointOnPlane { get { return Normal * D; } }


		public Plane(Plane inPlane)
		{
			A = inPlane.A;
			B = inPlane.B;
			C = inPlane.C;
			D = inPlane.D;
		}
		
		public Plane(Vector3f inNormal, float inD)
		{
			A = inNormal.X;
			B = inNormal.Y;
			C = inNormal.Z;
			D = inD;
		}

		public Plane(float inA, float inB, float inC, float inD)
		{
			A = inA;
			B = inB;
			C = inC;
			D = inD;
		}

		public static Vector3f Intersection(Plane inPlane1,
										    Plane inPlane2,
										    Plane inPlane3)
		{
			// intersection point with 3 planes
			//  {
			//      x = -( c2*b1*d3-c2*b3*d1+b3*c1*d2+c3*b2*d1-b1*c3*d2-c1*b2*d3)/
			//           (-c2*b3*a1+c3*b2*a1-b1*c3*a2-c1*b2*a3+b3*c1*a2+c2*b1*a3), 
			//      y =  ( c3*a2*d1-c3*a1*d2-c2*a3*d1+d2*c1*a3-a2*c1*d3+c2*d3*a1)/
			//           (-c2*b3*a1+c3*b2*a1-b1*c3*a2-c1*b2*a3+b3*c1*a2+c2*b1*a3), 
			//      z = -(-a2*b1*d3+a2*b3*d1-a3*b2*d1+d3*b2*a1-d2*b3*a1+d2*b1*a3)/
			//           (-c2*b3*a1+c3*b2*a1-b1*c3*a2-c1*b2*a3+b3*c1*a2+c2*b1*a3)
			//  }
			
			double bc1 = (inPlane1.B * inPlane3.C) - (inPlane3.B * inPlane1.C);
			double bc2 = (inPlane2.B * inPlane1.C) - (inPlane1.B * inPlane2.C);
			double bc3 = (inPlane3.B * inPlane2.C) - (inPlane2.B * inPlane3.C);

			double ad1 = (inPlane1.A * inPlane3.D) - (inPlane3.A * inPlane1.D);
			double ad2 = (inPlane2.A * inPlane1.D) - (inPlane1.A * inPlane2.D);
			double ad3 = (inPlane3.A * inPlane2.D) - (inPlane2.A * inPlane3.D);

			double x = -((inPlane1.D * bc3) + (inPlane2.D * bc1) + (inPlane3.D * bc2));
			double y = -((inPlane1.C * ad3) + (inPlane2.C * ad1) + (inPlane3.C * ad2));
			double z = +((inPlane1.B * ad3) + (inPlane2.B * ad1) + (inPlane3.B * ad2));
			double w = -((inPlane1.A * bc3) + (inPlane2.A * bc1) + (inPlane3.A * bc2));

			// better to have detectable invalid values then to have reaaaaaaally big values
			if (w > -Constants.NormalEpsilon && w < Constants.NormalEpsilon)
			{
				return new Vector3f(float.NaN,
									float.NaN,
									float.NaN);
			} else
				return new Vector3f((float)(x / w),
									(float)(y / w),
									(float)(z / w));
		}
		
		public static Vector3f Intersection(Vector3f start, Vector3f end, float sdist, float edist)
		{
			Vector3f	vector	= end - start;
			float		length	= edist - sdist;
			float		delta	= edist / length;

			return end - (delta * vector);
		}

		public Vector3f Intersection(Vector3f start, Vector3f end)
		{
			return Intersection(start, end, Distance(start), Distance(end));
		}


		public float Distance(Vector3f vector)
		{
			return
				(
					(A * vector.X) +
					(B * vector.Y) +
					(C * vector.Z) -
					(D)
				);
		}

		static public Side OnSide(float distance, float epsilon)
		{
			if		(distance >  epsilon) return Side.Outside;
			else if (distance < -epsilon) return Side.Inside;
			else return Side.Intersects;
		}

		static public Side OnSide(float distance)
		{
			if		(distance >  Constants.DistanceEpsilon) return Side.Outside;
			else if (distance < -Constants.DistanceEpsilon) return Side.Inside;
			else return Side.Intersects;
		}
	}
}
