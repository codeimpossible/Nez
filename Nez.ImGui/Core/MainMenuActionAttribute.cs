using System;
using System.Linq;

namespace Nez.ImGuiTools;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class MainMenuActionAttribute(string path, int priority = 0) : Attribute
{
	private static readonly string[] MenuItemSeparators = ["/", "\\"];

	public string ActionPath { get; set; } = NormalizeMenuItemName(path);
	public int Priority { get; set; } = priority;
	public string ParentMenu { get; set; } = GetTopLevelMenuName(path);

	private static string GetTopLevelMenuName(string rawName) => GetMenuPathSegments(rawName)[0];

	private static string[] GetMenuPathSegments(string rawName) =>
		rawName .Split(MenuItemSeparators, StringSplitOptions.None)
			.Select(token => token.Trim())
			.ToArray();

	private static string NormalizeMenuItemName(string rawName) =>
		string
			.Join(MenuItemSeparators[0], rawName.Split(MenuItemSeparators, StringSplitOptions.None)
				.Select(token => token.Trim())
				.ToArray());
}
