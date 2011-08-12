using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RealtimeCSG
{
	// Integer AABB to avoid needing to do epsilon calculations when comparing them
	[DebuggerDisplay("({MinX} {MinY} {MinZ}) ({MaxX} {MaxY} {MaxZ})")]
	public sealed class AABB
	{
		public int MinX = int.MaxValue;
		public int MaxX = int.MinValue;

		public int MinY = int.MaxValue;
		public int MaxY = int.MinValue;

		public int MinZ = int.MaxValue;
		public int MaxZ = int.MinValue;

		public int Width	{ get { return MaxX - MinX; } }
		public int Height	{ get { return MaxY - MinY; } }
		public int Depth	{ get { return MaxZ - MinZ; } }
		public int X		{ get { return (MaxX + MinX) / 2; } }
		public int Y		{ get { return (MaxY + MinY) / 2; } }
		public int Z		{ get { return (MaxZ + MinZ) / 2; } }

		public AABB() { } 

		public AABB(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
		{
			Add(minX, minY, minZ);
			Add(maxX, maxY, maxZ);
		}

		public AABB(AABB other)
		{
			Clear();
			Set(other);
		}


		public bool IsEmpty()
		{
			return (MinX >= MaxX ||
					MinY >= MaxY ||
					MinZ >= MaxZ);
		}
		
		public void Clear()
		{
			MinX = int.MaxValue;
			MaxX = int.MinValue;

			MinY = int.MaxValue;
			MaxY = int.MinValue;

			MinZ = int.MaxValue;
			MaxZ = int.MinValue;
		}

		public void Add(Vector3 inCoordinate)
		{
			Add(inCoordinate.X, inCoordinate.Y, inCoordinate.Z);
		}

		public void Add(float inX, float inY, float inZ)
		{
			Debug.Assert(!float.IsInfinity(inX) && !float.IsNaN(inX));
			Debug.Assert(!float.IsInfinity(inY) && !float.IsNaN(inY));
			Debug.Assert(!float.IsInfinity(inZ) && !float.IsNaN(inZ));

			MinX = Math.Min(MinX, (int)Math.Floor(inX));
			MinY = Math.Min(MinY, (int)Math.Floor(inY));
			MinZ = Math.Min(MinZ, (int)Math.Floor(inZ));

			MaxX = Math.Max(MaxX, (int)Math.Ceiling(inX));
			MaxY = Math.Max(MaxY, (int)Math.Ceiling(inY));
			MaxZ = Math.Max(MaxZ, (int)Math.Ceiling(inZ));
		}

		public void Add(AABB bounds)
		{
			MinX = Math.Min(MinX, bounds.MinX);
			MinY = Math.Min(MinY, bounds.MinY);
			MinZ = Math.Min(MinZ, bounds.MinZ);

			MaxX = Math.Max(MaxX, bounds.MaxX);
			MaxY = Math.Max(MaxY, bounds.MaxY);
			MaxZ = Math.Max(MaxZ, bounds.MaxZ);
		}

		public void Set(AABB bounds)
		{
			this.MinX = bounds.MinX;
			this.MinY = bounds.MinY;
			this.MinZ = bounds.MinZ;

			this.MaxX = bounds.MaxX;
			this.MaxY = bounds.MaxY;
			this.MaxZ = bounds.MaxZ;
		}

		public void Translate(int X, int Y, int Z)
		{
			this.MinX = this.MinX + X;
			this.MinY = this.MinY + Y;
			this.MinZ = this.MinZ + Z;

			this.MaxX = this.MaxX + X;
			this.MaxY = this.MaxY + Y;
			this.MaxZ = this.MaxZ + Z;
		}

		public void Translate(Vector3 translation)
		{
			this.MinX = (int)Math.Floor(this.MinX + translation.X);
			this.MinY = (int)Math.Floor(this.MinY + translation.Y);
			this.MinZ = (int)Math.Floor(this.MinZ + translation.Z);

			this.MaxX = (int)Math.Ceiling(this.MaxX + translation.X);
			this.MaxY = (int)Math.Ceiling(this.MaxY + translation.Y);
			this.MaxZ = (int)Math.Ceiling(this.MaxZ + translation.Z);
		}

		public AABB Translated(Vector3 translation)
		{
			return new AABB(this.MinX + translation.X,
							this.MinY + translation.Y,
							this.MinZ + translation.Z,

							this.MaxX + translation.X,
							this.MaxY + translation.Y,
							this.MaxZ + translation.Z);

		}

		public void Set(AABB other, Vector3 translation)
		{
			this.MinX = (int)Math.Floor(other.MinX + translation.X);
			this.MinY = (int)Math.Floor(other.MinY + translation.Y);
			this.MinZ = (int)Math.Floor(other.MinZ + translation.Z);

			this.MaxX = (int)Math.Ceiling(other.MaxX + translation.X);
			this.MaxY = (int)Math.Ceiling(other.MaxY + translation.Y);
			this.MaxZ = (int)Math.Ceiling(other.MaxZ + translation.Z);
		}

		public bool			IsOutside(AABB other)
		{
			return	(this.MaxX - other.MinX) < 0 || (this.MinX - other.MaxX) > 0 ||
					(this.MaxY - other.MinY) < 0 || (this.MinY - other.MaxY) > 0 ||
					(this.MaxZ - other.MinZ) < 0 || (this.MinZ - other.MaxZ) > 0
						;
		}

		public static bool	IsOutside(AABB left, AABB right)
		{
			return	(left.MaxX - right.MinX) < 0 || (left.MinX - right.MaxX) > 0 ||
					(left.MaxY - right.MinY) < 0 || (left.MinY - right.MaxY) > 0 ||
					(left.MaxZ - right.MinZ) < 0 || (left.MinZ - right.MaxZ) > 0
						;
		}

		public static bool IsOutside(AABB left, Vector3 translation, AABB right)
		{
			return  ((left.MaxX + translation.X) - right.MinX) < 0 || ((left.MinX + translation.X) - right.MaxX) > 0 ||
					((left.MaxY + translation.Y) - right.MinY) < 0 || ((left.MinY + translation.Y) - right.MaxY) > 0 ||
					((left.MaxZ + translation.Z) - right.MinZ) < 0 || ((left.MinZ + translation.Z) - right.MaxZ) > 0
						;
		}
	}
}
