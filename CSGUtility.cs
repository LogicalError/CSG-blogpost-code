using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealtimeCSG
{
	public static class CSGUtility
	{
		#region FindChildNodes
		public static IEnumerable<CSGNode> FindChildNodes(CSGNode node)
		{
			yield return node;
			if (node.NodeType != CSGNodeType.Brush)
			{
				foreach (var child in FindChildNodes(node.Left))
					yield return child;
				foreach (var child in FindChildNodes(node.Right))
					yield return child;
			}
		}
		#endregion

		#region FindChildBrushes
		public static IEnumerable<CSGNode> FindChildBrushes(CSGNode node)
		{
			if (node.NodeType != CSGNodeType.Brush)
			{
				foreach (var brush in FindChildBrushes(node.Left))
					yield return brush;
				foreach (var brush in FindChildBrushes(node.Right))
					yield return brush;
				yield break;
			}
			else
				yield return node;
		}

		public static IEnumerable<CSGNode> FindChildBrushes(CSGTree tree)
		{
			return FindChildBrushes(tree.RootNode);
		}
		#endregion

		#region UpdateChildTransformations
		public static void UpdateChildTransformations(CSGNode node, Vector3 parentTranslation)
		{
			node.Translation = Vector3.Add(parentTranslation, node.LocalTranslation);
			if (node.NodeType == CSGNodeType.Brush)
				return;
			UpdateChildTransformations(node.Left, node.Translation);
			UpdateChildTransformations(node.Right, node.Translation);
		}
		public static void UpdateChildTransformations(CSGNode node)
		{
			if (node.NodeType == CSGNodeType.Brush)
				return;
			UpdateChildTransformations(node.Left, node.Translation);
			UpdateChildTransformations(node.Right, node.Translation);
		}
		#endregion

		#region UpdateBounds
		public static void UpdateBounds(CSGNode node)
		{
			if (node.NodeType != CSGNodeType.Brush)
			{
				var leftNode = node.Left;
				var rightNode = node.Right;
				UpdateBounds(leftNode);
				UpdateBounds(rightNode);

				node.Bounds.Clear();
				node.Bounds.Add(leftNode.Bounds.Translated(Vector3.Subtract(leftNode.Translation, node.Translation)));
				node.Bounds.Add(rightNode.Bounds.Translated(Vector3.Subtract(rightNode.Translation, node.Translation)));
			}
		}
		#endregion
	}
}
