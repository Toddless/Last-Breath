#if TOOLS
namespace LastBreath.Addons.Tools.TagGenerator
{
    using Godot;

    [Tool]
    public partial class TagEditorPlugin : EditorPlugin
    {
        private TagInspectorPlugin? _inspectorPlugin;

        public override void _EnterTree()
        {
            _inspectorPlugin = new TagInspectorPlugin();
            AddInspectorPlugin(_inspectorPlugin);
            GD.Print("[TagEditorPlugin] inspector plugin added");
        }

        public override void _ExitTree()
        {
            if (_inspectorPlugin != null)
            {
                RemoveInspectorPlugin(_inspectorPlugin);
                _inspectorPlugin = null;
                GD.Print("[TagEditorPlugin] inspector plugin removed");
            }
        }
    }
}
#endif
