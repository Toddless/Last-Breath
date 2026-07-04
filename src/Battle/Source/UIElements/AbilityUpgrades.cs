namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Abilities;
    using Godot;
    using Utilities;

    public partial class AbilityUpgrades : Control
    {
        private const string UID = "uid://cfjcrk0kkryi8";
        [Export] private BoxContainer? _tierOne, _tierTwo, _tierThree;

        public event Action<string, int>? AbilityUpgradeSelected;

        public override void _Ready()
        {
            CreateButtonGroup(_tierOne);
            CreateButtonGroup(_tierTwo);
            CreateButtonGroup(_tierThree);
        }

        public void SetUpgradesInTier(int tier, List<IAbilityUpgrade> upgrades)
        {
            var buttons = GetButtonsInTier(tier);
            int count = 0;
            foreach (var upgrade in upgrades)
            {
                var button = buttons[count];
                button.Text = Localization.Localize(upgrade.Id);
                button.SetUpgradeTaken(upgrade.Learned);
                button.SetUpgradeTier(tier);
                button.SetUpgradeInstanceId(upgrade.InstanceId);
                count++;
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void CreateButtonGroup(BoxContainer? container)
        {
            if (container == null) return;

            var buttonGroup = new ButtonGroup();
            buttonGroup.AllowUnpress = false;
            foreach (UpgradeButton button in container.GetChildren().OfType<UpgradeButton>())
            {
                button.ButtonGroup = buttonGroup;
                button.UpgradeSelected += OnUpgradeSelected;
            }
        }

        private void OnUpgradeSelected(string upgradeInstanceId, int tier)
        {
            AbilityUpgradeSelected?.Invoke(upgradeInstanceId, tier);
        }

        private UpgradeButton[] GetButtonsInTier(int tier) => tier switch
        {
            1 => _tierOne?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            2 => _tierTwo?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            3 => _tierThree?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            _ => []
        };
    }
}
