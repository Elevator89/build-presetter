using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Elevator89.BuildPresetter.FolderHierarchy;

namespace Elevator89.BuildPresetter
{
	internal class HierarchyTreeViewItem : TreeViewItem
	{
		public HierarchyAsset HierarchyAsset { get; set; }

		public HierarchyTreeViewItem(HierarchyAsset hierarchyAsset, int depth) : base(hierarchyAsset.Id, depth, hierarchyAsset.Name)
		{
			HierarchyAsset = hierarchyAsset;
		}
	}

	internal class StreamingAssetsTreeView : TreeView
	{
		private HierarchyAsset _hierarchy = new HierarchyAsset(-1, "Default");

		public StreamingAssetsTreeView(TreeViewState state) : base(state)
		{
			Reload();
		}

		public void SetStreamingAssetsHierarchy(HierarchyAsset hierarchy)
		{
			_hierarchy = hierarchy;
			Reload();
		}

		protected override TreeViewItem BuildRoot()
		{
			return new HierarchyTreeViewItem(_hierarchy, -1);
		}

		protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
		{
			IList<TreeViewItem> rows = GetRows() ?? new List<TreeViewItem>(200);
			rows.Clear();

			AddChildrenRecursive(_hierarchy, 0, rows);

			SetupParentsAndChildrenFromDepths(root, rows);
			return rows;
		}

		private void AddChildrenRecursive(HierarchyAsset hierarchy, int depth, IList<TreeViewItem> rows)
		{
			foreach (HierarchyAsset child in hierarchy.Children)
			{
				HierarchyTreeViewItem viewItem = new HierarchyTreeViewItem(child, depth);
				rows.Add(viewItem);

				if (child.Children.Count > 0)
				{
					if (IsExpanded(child.Id))
						AddChildrenRecursive(child, depth + 1, rows);
					else
						viewItem.children = CreateChildListForCollapsedParent();
				}
			}
		}

		protected override IList<int> GetDescendantsThatHaveChildren(int id)
		{
			List<int> descendantsWithChildrenIds = new List<int>();

			HierarchyAsset startFrom = FindById(id, _hierarchy);
			if (startFrom != null)
				FillDescendantsWithChildren(startFrom, descendantsWithChildrenIds);

			return descendantsWithChildrenIds;
		}

		private void FillDescendantsWithChildren(HierarchyAsset hierarchy, List<int> descendantsWithChildrenIds)
		{
			if (hierarchy.Children.Count > 0)
				descendantsWithChildrenIds.Add(hierarchy.Id);

			for (int i = 0; i < hierarchy.Children.Count; ++i)
				FillDescendantsWithChildren(hierarchy.Children[i], descendantsWithChildrenIds);
		}

		private HierarchyAsset FindById(int id, HierarchyAsset hierarchy)
		{
			if (hierarchy.Id == id)
				return hierarchy;

			for (int i = 0; i < hierarchy.Children.Count; ++i)
			{
				HierarchyAsset foundChild = FindById(id, hierarchy.Children[i]);
				if (foundChild != null)
					return foundChild;
			}
			return null;
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			Event evt = Event.current;
			extraSpaceBeforeIconAndLabel = 18f;

			HierarchyTreeViewItem hierarchyViewItem = (HierarchyTreeViewItem)args.item;
			HierarchyAsset hierarchyAsset = hierarchyViewItem.HierarchyAsset;

			Rect toggleRect = args.rowRect;
			toggleRect.x += GetContentIndent(args.item);
			toggleRect.width = 16f;

			// Ensure row is selected before using the toggle (usability)
			if (evt.type == EventType.MouseDown && toggleRect.Contains(evt.mousePosition))
				SelectionClick(args.item, false);

			EditorGUI.BeginChangeCheck();
			bool isIncluded = EditorGUI.Toggle(toggleRect, hierarchyAsset.IsIncluded);
			if (EditorGUI.EndChangeCheck())
				hierarchyAsset.IsIncluded = isIncluded;

			base.RowGUI(args); // Text
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			Selection.instanceIDs = selectedIds.ToArray();
		}
	}
}
