using System;
using System.Collections.Generic;
using System.Linq;

namespace Elevator89.BuildPresetter.FolderHierarchy
{
	internal static class Hierarchy
	{
		public static HierarchyAsset BuildFrom(IEnumerable<string> assetPaths)
		{
			using (IEnumerator<string> enumerator = assetPaths.GetEnumerator())
			{
				if (!enumerator.MoveNext())
					throw new ArgumentException();

				HierarchyAsset hierarchy = BuildFrom(enumerator.Current);

				while (enumerator.MoveNext())
				{
					Merge(hierarchy, BuildFrom(enumerator.Current));
				}

				return hierarchy;
			}
		}

		public static HierarchyAsset BuildFrom(string assetPath)
		{
			HierarchyAsset asset = null;

			while (true)
			{
				int lastSlashIndex = assetPath.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase);

				List<HierarchyAsset> children = new List<HierarchyAsset>();
				if (asset != null)
					children.Add(asset);

				if (lastSlashIndex == -1)
					return new HierarchyAsset(assetPath.GetHashCode(), assetPath, children); // Root item, assetPath is just its name

				string assetName = assetPath.Substring(lastSlashIndex + 1);

				asset = new HierarchyAsset(assetPath.GetHashCode(), assetName, children);
				assetPath = assetPath.Substring(0, lastSlashIndex);
			}
		}

		public static void Merge(HierarchyAsset destination, HierarchyAsset source)
		{
			if (destination.Name != source.Name)
				throw new ArgumentException();

			foreach (HierarchyAsset sourceChild in source.Children)
			{
				HierarchyAsset destinationChild = destination.Children.FirstOrDefault(c => c.Name == sourceChild.Name);
				if (destinationChild != null)
				{
					Merge(destinationChild, sourceChild);
					break;
				}
				else
				{
					destination.Children.Add(sourceChild);
				}
			}
		}
	}
}
