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
	public sealed class Vector3f
	{
		public float X;
		public float Y;
		public float Z;

		public bool IsValid()
		{
			return (!float.IsNaN(X) && !float.IsInfinity(X) &&
					!float.IsNaN(Y) && !float.IsInfinity(Y) &&
					!float.IsNaN(Z) && !float.IsInfinity(Z));
		}

		public Vector3f(Vector3f inVector)
		{
			X = inVector.X;
			Y = inVector.Y;
			Z = inVector.Z;
		}

		public Vector3f(float inX, float inY, float inZ)
		{
			X = inX; 
			Y = inY; 
			Z = inZ;
		}

		public void Multiply(float scalar)
		{
			this.X *= scalar;
			this.Y *= scalar;
			this.Z *= scalar;
		}

		public static Vector3f operator *(float scalar, Vector3f vector)
		{
			return new Vector3f((scalar * vector.X),
								(scalar * vector.Y),
								(scalar * vector.Z));
		}

		public static Vector3f operator *(Vector3f vector, float scalar)
		{
			return new Vector3f((vector.X * scalar),
								(vector.Y * scalar),
								(vector.Z * scalar));
		}

		public static Vector3f operator -(Vector3f left, Vector3f right)
		{
			return new Vector3f((left.X - right.X),
								(left.Y - right.Y),
								(left.Z - right.Z));
		}

		public static Vector3f operator +(Vector3f left, Vector3f right)
		{
			return new Vector3f((left.X + right.X),
								(left.Y + right.Y),
								(left.Z + right.Z));
		}

		public float Dotproduct(Vector3f right)
		{
			return (X * right.X) +
				   (Y * right.Y) +
				   (Z * right.Z);
		}
		
		public Vector3f CrossProduct(Vector3f right)
		{
			return new Vector3f( (Y * right.Z) - (Z * right.Y),
								 (Z * right.X) - (X * right.Z),
								 (X * right.Y) - (Y * right.X));
		}
	}
}
