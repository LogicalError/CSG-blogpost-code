using System;

namespace RealtimeCSG
{
	public sealed class Vector3f
	{
		public float X;
		public float Y;
		public float Z;

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
	}
}
