namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Views;
    using Godot;

    /// <summary>
    /// The three tiers of upgrade radio-buttons. Fed pure view data (no domain object); reports the
    /// chosen upgrade up by instance id + tier. One selection per tier is enforced by the ButtonGroups.
    /// </summary>
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

        public void SetOptions(IReadOnlyList<UpgradeOptionView> options)
        {
            foreach (var tier in options.GroupBy(option => option.Tier))
                SetTier(tier.Key, tier.ToList());
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void SetTier(int tier, List<UpgradeOptionView> options)
        {
            var buttons = GetButtonsInTier(tier);
            for (int i = 0; i < buttons.Length && i < options.Count; i++)
            {
                var button = buttons[i];
                var option = options[i];
                button.Text = option.DisplayName;
                button.SetUpgradeTaken(option.Selected);
                button.SetUpgradeTier(tier);
                button.SetUpgradeInstanceId(option.UpgradeInstanceId);
            }
        }

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

        private void OnUpgradeSelected(string upgradeInstanceId, int tier) => AbilityUpgradeSelected?.Invoke(upgradeInstanceId, tier);

        private UpgradeButton[] GetButtonsInTier(int tier) => tier switch
        {
            1 => _tierOne?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            2 => _tierTwo?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            3 => _tierThree?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            _ => []
        };
    }
}
