namespace LastBreath.addons.Tools.TagGenerator
{
	using Godot;
	using System.Linq;
	using Core.Constants;

	[Tool]
	public partial class GenerateTags : EditorScript
	{
		public override void _Run()
		{
			var fields = typeof(TagConstants)
				.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				.Where(x => x.FieldType == typeof(string));

			var tags = fields
				.Select(x => x.GetValue(string.Empty))
				.Cast<string>()
				.Where(x => !string.IsNullOrEmpty(x))
				.Distinct()
				.OrderBy(s => s)
				.ToArray();

			var reg = ResourceLoader.Load<TagRegistry>("res://addons/Tools/TagGenerator/tag_registry.tres");
			if (tags != null)
				reg.AllTags = tags;
            
			var error = ResourceSaver.Save(reg);

			if (error != Error.Ok)
				GD.Print($"Failed to save registry: {error}");
		}
	}
}
