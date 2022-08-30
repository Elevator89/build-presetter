using System.Collections.Generic;

namespace Elevator89.BuildPresetter.FolderHierarchy
{
	public class HierarchyAsset
	{
		public readonly int Id;
		public readonly string Name;
		public readonly List<HierarchyAsset> Children = new List<HierarchyAsset>();
		public bool IsIncluded = false;

		public HierarchyAsset(int id, string name) : this(id, name, new List<HierarchyAsset>())
		{ }

		public HierarchyAsset(int id, string name, List<HierarchyAsset> children)
		{
			Id = id;
			Name = name;
			Children = children;
		}
	}
}
