#if TOOLS
namespace LastBreath.addons.Tools.TagGenerator
{
    using System.Linq;
    using Godot;

    [Tool]
    public partial class TagInspectorPlugin : EditorInspectorPlugin
    {
        private readonly string _defaultDir = "res://addons/Tools/TagGenerator/";

        public override bool _CanHandle(GodotObject @object) => true;

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
        {
            var propName = name.Split('.').FirstOrDefault() ?? name;
            if (propName == "Tags")
            {
                var editor = new TagEditorProperty();
                editor.SetRegistryDir(_defaultDir);
                AddPropertyEditor(name, editor);
                return true;
            }

            return false;
        }
    }
}
#endif
