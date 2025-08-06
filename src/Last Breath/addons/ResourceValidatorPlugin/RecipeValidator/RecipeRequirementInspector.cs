namespace LastBreath.addons.ResourceValidatorPlugin.RecipeValidator
{
    using Godot;
    using LastBreath.addons.Crafting.Recipies;
    using LastBreath.Addons.Crafting;

    [Tool]
    public partial class RecipeRequirementInspector : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject @object)
        {
            GD.Print($"Get object: {@object.GetType().Name}");
            return @object is RecipeRequirement;
        }


        public override void _ParseBegin(GodotObject @object)
        {
            var req = (RecipeRequirement)@object;
            if (req.Resource is CraftingResource cr &&
                cr.MaterialType?.Category != req.Category)
            {
                var warning = new Label
                {
                    Text = $"The category did ({req.Category}) not match ({cr.MaterialType?.Category})"
                };
                warning.AddThemeColorOverride("font_color", Colors.OrangeRed);
                AddCustomControl(warning);
            }
        }
    }
}
