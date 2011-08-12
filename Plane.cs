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
	[DebuggerDisplay("{A} {B} {C} {D}")]
	public class Plane
	{
		public float A;
		public float B;
		public float C;
		public float D;
		
		public Vector3 Normal       { get { return new Vector3(A, B, C); } set { A = value.X; B = value.Y; C = value.Z; } }
		public Vector3 PointOnPlane { get { return Normal * D; } }
		
		#region Constructors
		public Plane() { }

		public Plane(Plane inPlane)
		{
			A = inPlane.A;
			B = inPlane.B;
			C = inPlane.C;
			D = inPlane.D;
		}

		public Plane(Vector3 inNormal, float inD)
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
		#endregion

		#region Plane Intersection
		public static Vector3 Intersection(Plane inPlane1,
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

			// better to have detectable invalid values than to have reaaaaaaally big values
			if (w > -Constants.NormalEpsilon && w < Constants.NormalEpsilon)
			{
				return new Vector3(float.NaN,
								   float.NaN,
								   float.NaN);
			}
			else
				return new Vector3((float)(x / w),
								   (float)(y / w),
								   (float)(z / w));
		}
		#endregion

		#region Ray Intersection
		public static Vector3 Intersection(Vector3 start, Vector3 end, float sdist, float edist)
		{
			Vector3 vector = end - start;
			float length = edist - sdist;
			float delta = edist / length;

			return end - (delta * vector);
		}

		public Vector3 Intersection(Vector3 start, Vector3 end)
		{
			return Intersection(start, end, Distance(start), Distance(end));
		}
		#endregion

		#region Distance
		public float Distance(float x, float y, float z)
		{
			return
				(
					(A * x) +
					(B * y) +
					(C * z) -
					(D)
				);
		}

		public float Distance(Vector3 vertex)
		{
			return
				(
					(A * vertex.X) +
					(B * vertex.Y) +
					(C * vertex.Z) -
					(D)
				);
		}
		#endregion

		// These methods are designed for clarity and readability, 
		//	if speed is your concern do not use enums and use the floating point values directly!!
		#region OnSide
		static public PlaneSideResult OnSide(float distance, float epsilon)
		{
			if (distance > epsilon) return PlaneSideResult.Outside;
			else if (distance < -epsilon) return PlaneSideResult.Inside;
			else return PlaneSideResult.Intersects;
		}

		public static PlaneSideResult OnSide(float distance)
		{
			if (distance > Constants.DistanceEpsilon) return PlaneSideResult.Outside;
			else if (distance < -Constants.DistanceEpsilon) return PlaneSideResult.Inside;
			else return PlaneSideResult.Intersects;
		}

		public PlaneSideResult OnSide(float x, float y, float z)
		{
			return OnSide(Distance(x, y, z));
		}

		public PlaneSideResult OnSide(Vector3 vertex)
		{
			return OnSide(Distance(vertex));
		}

		public PlaneSideResult OnSide(AABB bounds)
		{
			var x = A >= 0 ? bounds.MinX : bounds.MaxX;
			var y = B >= 0 ? bounds.MinY : bounds.MaxY;
			var z = C >= 0 ? bounds.MinZ : bounds.MaxZ;
			return OnSide(Distance(x, y, z));
		}

		public PlaneSideResult OnSide(AABB bounds, Vector3 translation)
		{
			var backward_x = A <= 0 ? bounds.MinX : bounds.MaxX;
			var backward_y = B <= 0 ? bounds.MinY : bounds.MaxY;
			var backward_z = C <= 0 ? bounds.MinZ : bounds.MaxZ;
			var distance = Distance(backward_x + translation.X, backward_y + translation.Y, backward_z + translation.Z);
			var side = OnSide(distance);
			if (side == PlaneSideResult.Inside)
				return PlaneSideResult.Inside;
			var forward_x = A >= 0 ? bounds.MinX : bounds.MaxX;
			var forward_y = B >= 0 ? bounds.MinY : bounds.MaxY;
			var forward_z = C >= 0 ? bounds.MinZ : bounds.MaxZ;
			distance = Distance(forward_x + translation.X, forward_y + translation.Y, forward_z + translation.Z);
			side = OnSide(distance);
			if (side == PlaneSideResult.Outside)
				return PlaneSideResult.Outside;
			return PlaneSideResult.Intersects;
		}
		#endregion

		#region Plane Negation
		public Plane Negated()
		{
			return new Plane(-A, -B, -C, -D);
		}
		#endregion

		#region Plane Translation
		public void Translate(Vector3 translation)
		{
			// translated offset = plane.Normal.Dotproduct(translation)
			// normal = A,B,C
			D += (A * translation.X) +
				 (B * translation.Y) +
				 (C * translation.Z);
		}

		public static Plane Translated(Plane plane, Vector3 translation)
		{
			return new Plane(plane.A, plane.B, plane.C,
				// translated offset = plane.Normal.Dotproduct(translation)
				// normal = A,B,C
							 plane.D + (plane.A * translation.X) +
									   (plane.B * translation.Y) +
									   (plane.C * translation.Z));
		}

		public static Plane Translated(Plane plane, float translateX, float translateY, float translateZ)
		{
			return new Plane(plane.A, plane.B, plane.C,
				// translated offset = plane.Normal.Dotproduct(translation)
				// normal = A,B,C
							 plane.D + (plane.A * translateX) +
									   (plane.B * translateY) +
									   (plane.C * translateZ));
		}
		#endregion

		#region Plane comparisons
		public override int GetHashCode()
		{
			return A.GetHashCode() ^
					B.GetHashCode() ^
					C.GetHashCode() ^
					D.GetHashCode();
		}

		public bool Equals(Plane other)
		{
			if (Object.ReferenceEquals(this, other))
				return true;
			if (Object.ReferenceEquals(other, null))
				return false;
			return D == other.D &&
					A == other.A &&
					B == other.B &&
					C == other.C;
		}

		public override bool Equals(object obj)
		{
			if (Object.ReferenceEquals(this, obj))
				return true;
			Plane other = obj as Plane;
			if (Object.ReferenceEquals(other, null))
				return false;
			return D == other.D &&
					A == other.A &&
					B == other.B &&
					C == other.C;
		}

		public static bool operator ==(Plane left, Plane right)
		{
			if (Object.ReferenceEquals(left, right))
				return true;
			if (Object.ReferenceEquals(left, null) ||
				Object.ReferenceEquals(right, null))
				return false;
			return left.D == right.D &&
					left.A == right.A &&
					left.B == right.B &&
					left.C == right.C;
		}

		public static bool operator !=(Plane left, Plane right)
		{
			if (Object.ReferenceEquals(left, right))
				return false;
			if (Object.ReferenceEquals(left, null) ||
				Object.ReferenceEquals(right, null))
				return true;
			return left.D != right.D ||
					left.A != right.A ||
					left.B != right.B ||
					left.C != right.C;
		}
		#endregion
	}
}
