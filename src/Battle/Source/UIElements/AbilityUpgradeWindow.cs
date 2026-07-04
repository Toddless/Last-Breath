namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.UI;
    using Godot;

    public partial class AbilityUpgradeWindow : Control, IWindow
    {
        private const string UID = "uid://qkc76uh24l18";

        private IAbility? _ability;
        [Export] private AbilityUpgrades? _abilityUpgrades;
        [Export] private Label? _abilityName, _cost, _cooldown;
        [Export] private TextureRect? _abilityIcon;
        [Export] private RichTextLabel? _abilityDescription;
        [Export] private Button? _close;

        public bool IsAlreadyVisible => IsInsideTree() && IsVisible();

        public override void _Ready()
        {
            _abilityUpgrades?.AbilityUpgradeSelected += OnUpgradeSelected;
        }

        public void InjectServices(IGameServiceProvider provider)
        {

        }


        public void ShowAbility(IAbility ability)
        {
            foreach ((int tier, List<IAbilityUpgrade> upgrades) in ability.Upgrades)
                _abilityUpgrades?.SetUpgradesInTier(tier, upgrades);
            _ability = ability;
            _abilityName?.Text = _ability.DisplayName;
            _cooldown?.Text = $"Cooldown: {Mathf.RoundToInt(ability.Cooldown)}s";
            _cost?.Text = $"Cost: {Mathf.RoundToInt(ability.CostValue)} {ability.CostType}";
            _abilityDescription?.Text = _ability.Description;
        }

        private void OnUpgradeSelected(string upgradeInstanceId, int tier)
        {
            _ability?.SelectUpgrade(tier, upgradeInstanceId);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Close() => QueueFree();
    }
}
