using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nez.ImGuiTools;

public sealed class MainMenuNode(string name, StringComparer comparer, MethodInfo method = null)
{
	public string Name { get; } = name;
	public Dictionary<string, MainMenuNode> Children { get; } = new(comparer);
	public bool HasChildren => Children.Count > 0;
	public MethodInfo Method { get; set; } = method;
}

public static class MainMenuTree
{
	public static MainMenuNode BuildTree(IEnumerable<MethodInfo> methods)
	{
		var comparer = StringComparer.OrdinalIgnoreCase;
		var root = new MainMenuNode("<root>", comparer);

		foreach (var method in methods)
		{
			var attr = method.GetCustomAttribute<MainMenuActionAttribute>()!;
			var segments = attr.ActionPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			var current = root;

			for (int i = 0; i < segments.Length; i++)
			{
				var segment = segments[i];
				if (!current.Children.TryGetValue(segment, out var child))
				{
					child = new MainMenuNode(segment, comparer);
					current.Children.Add(segment, child);
				}

				current = child;
				if (i == segments.Length - 1)
				{
					current.Method = method;
				}
			}
		}

		return root;
	}
}
