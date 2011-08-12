using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RealtimeCSG
{
	[DebuggerDisplay("{Operator}")]
	public sealed class CSGNode
	{
		public CSGNode(string id, CSGNodeType branchOperator) { this.NodeType = branchOperator; this.Left = null; this.Right = null; }
		public CSGNode(string id, CSGNodeType branchOperator, CSGNode left, CSGNode right) { this.NodeType = branchOperator; this.Left = left; this.Right = right; }
		public CSGNode(string id, IEnumerable<Plane> planes) { this.NodeType = CSGNodeType.Brush; Planes = planes.ToArray(); }

		public readonly AABB Bounds = new AABB();
		public readonly CSGNodeType NodeType;

		public CSGNode Left;
		public CSGNode Right;
		public CSGNode Parent;

		public Vector3 LocalTranslation;
		public Vector3 Translation;
		public Plane[] Planes;
	}
}
