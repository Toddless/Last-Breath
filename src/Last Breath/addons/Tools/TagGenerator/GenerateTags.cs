namespace LastBreath.addons.Tools.TagGenerator
{
	using System.Collections.Generic;
	using System.Linq;
	using Godot;

	[Tool]
	public partial class GenerateTags : EditorScript
	{
		private const string InputPath = "res://addons/Tools/TagGenerator/AllTags.json";
		private const string OutputPath = "res://addons/Tools/TagGenerator/TagRegistry.tres";

		public override void _Run()
		{
			if (!FileAccess.FileExists(InputPath))
			{
				GD.PrintErr($"Json file not found: {InputPath}");
				return;
			}

			using var fa = FileAccess.Open(InputPath, FileAccess.ModeFlags.Read);
			if (fa == null)
			{
				GD.PrintErr("Failed to open Json file.");
				return;
			}

			var jsonText = fa.GetAsText();
			var json = new Json();
			var parse = json.Parse(jsonText);
			if (parse != Error.Ok)
			{
				GD.PrintErr($"Json not found");
				return;
			}

			if (json.Data.VariantType != Variant.Type.Dictionary)
			{
				GD.PrintErr("Unexpected Json root type (expected object/dictionary).");
				return;
			}

			var defs = new Godot.Collections.Dictionary<string, TagDefinition>();

			var hierarchies = new List<string>();

			void Recurse(object node, List<string> path)
			{
				if (node is Variant godotNode)
				{
					if (godotNode.VariantType == Variant.Type.Dictionary)
					{
						var dict = godotNode.AsGodotDictionary();
						foreach (var keyObj in dict.Keys)
						{
							var key = keyObj.AsString() ?? string.Empty;
							if (string.IsNullOrWhiteSpace(key)) continue;

							var newPath = new List<string>(path) { key };
							AddSegmentAsTag(key);

							var val = dict[key];
							Recurse(val, newPath);
						}
					}
					else if (godotNode.VariantType == Variant.Type.Array)
					{
						var arr = godotNode.AsStringArray();
						foreach (var item in arr)
						{
							var leaf = item.ToString() ?? string.Empty;
							if (string.IsNullOrWhiteSpace(leaf)) continue;

							var fullPathSegments = new List<string>(path) { leaf };
							foreach (var seg in fullPathSegments)
								AddSegmentAsTag(seg);

							var hierarchyPath = string.Join("/", fullPathSegments);
							hierarchies.Add(hierarchyPath);
						}
					}
					else
					{
						// unexpected - ignore
					}
				}
			}

			void AddSegmentAsTag(string display)
			{
				var id = TagUtils.Normalize(display);
				if (string.IsNullOrEmpty(id)) return;
				if (!defs.ContainsKey(id))
				{
					var td = new TagDefinition
					{
						Id = id,
						DisplayName = display
					};
					defs[id] = td;
				}
				else
				{
					if (string.IsNullOrEmpty(defs[id].DisplayName) && !string.IsNullOrEmpty(display))
						defs[id].DisplayName = display;
				}
			}

			var rootDict = json.Data.AsGodotDictionary();
			foreach (var topKey in rootDict.Keys)
			{
				var topKeyString = topKey.AsString() ?? string.Empty;
				var topVal = rootDict[topKeyString];
				var startPath = new List<string> { topKeyString };
				AddSegmentAsTag(topKeyString);
				Recurse(topVal, startPath);
			}

			hierarchies = [.. hierarchies.Distinct()];

			var reg = new TagRegistry
			{
				AllTags = [.. defs.Values],
				Hierarchies = [.. hierarchies],
			};

			var err = ResourceSaver.Save(reg, OutputPath);
			if (err != Error.Ok)
			{
				GD.PrintErr($"Failed to save TagRegistry to {OutputPath}: {err}");
			}
			else
			{
				GD.Print($"Tag registry generated: {OutputPath}. Tags: {defs.Count}, Hierarchies: {hierarchies.Count}");
			}
		}
	}
}
