namespace LastBreath.addons.ResourceValidatorPlugin
{
    using Godot;
    using LastBreath.addons.ResourceValidatorPlugin.RecipeValidator;

    [Tool]
    public partial class ResourceValidatorPlugin : EditorPlugin
    {
        private RecipeRequirementInspector? _inspector;

        public override void _EnterTree()
        {
            _inspector = new RecipeRequirementInspector();
            AddInspectorPlugin(_inspector);
        }

        public override void _ExitTree() => RemoveInspectorPlugin(_inspector);
    }
}
