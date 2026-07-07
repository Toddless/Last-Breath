namespace Battle.Source.UIElements
{
    using Core.Battle.Abilities;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

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

            SetAbilityParameter(Localization.Localize("Cost"), ability.CostValue.ToString());
            SetAbilityParameter(Localization.Localize("Resource"), Localization.Localize(ability.CostType.ToString()));
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
