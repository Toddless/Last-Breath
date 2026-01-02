namespace Battle.Source.UIElements
{
    using Godot;
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.UI;
    using Core.Interfaces.Abilities;

    public partial class AbilityDescription : Control, IInitializable
    {
        private const string UID = "uid://bt653n8hnbkxf";
        [Export] private RichTextLabel? _description;
        [Export] private VBoxContainer? _abilityParametersContainer;
        [Export] private Label? _name;


        public void SetupFullAbilityDescription(IAbility ability)
        {
            SetAbilityName(ability.DisplayName);
            SetDescription(ability.Description);

            SetAbilityParameter(Localization.Localize("Type"), Localization.Localize(ability.AbilityType.ToString()));
            SetAbilityParameter(Localization.Localize("Cost"), ability.CostValue.ToString());
            SetAbilityParameter(Localization.Localize("Resource"), Localization.Localize(ability.CostType.ToString()));
            SetAbilityParameter(Localization.Localize("Cooldown"), ability.Cooldown.ToString());
            if (ability.AbilityType is AbilityType.Target)
                SetAbilityParameter(Localization.Localize("MaxTargets"), ability.MaxTargets.ToString());
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void SetDescription(string description) => _description?.Text = description;

        private void SetAbilityName(string name) => _name?.Text = name;

        private void SetAbilityParameter(string parameter, string value)
        {
            var statLine = StatLine.Initialize().Instantiate<StatLine>();
            statLine.SetStatLineText(parameter, value);
            _abilityParametersContainer?.AddChild(statLine);
        }
    }
}
