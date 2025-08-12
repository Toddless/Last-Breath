#if TOOLS
namespace LastBreath.addons.Tools.TagGenerator
{
    using System.Linq;
    using Godot;

    [Tool]
    public partial class TagInspectorPlugin : EditorInspectorPlugin
    {
        private const string PathToFile = "res://addons/Tools/TagGenerator/TagRegistry.tres";

        public override bool _CanHandle(GodotObject @object) => @object.IsClass(nameof(Resource));

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
        {
            var propName = name.Split('.').FirstOrDefault() ?? name;
            if (propName == "Tags")
            {
                var editor = new TagEditorProperty();
                editor.SetRegistryDir(PathToFile);
                AddPropertyEditor(name, editor);
                return true;
            }

            return false;
        }
    }
}
#endif
