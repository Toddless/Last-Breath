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
        private const int TierCount = 3; // mirrors the three exported tier containers

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
            // Every tier renders, even an empty one: the window instance is reused between
            // abilities (OpenWindow returns the open one), so a skipped tier would keep the
            // previous ability's variants on screen (tracker #64).
            for (int tier = 1; tier <= TierCount; tier++)
                SetTier(tier, options.Where(option => option.Tier == tier).ToList());
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void SetTier(int tier, List<UpgradeOptionView> options)
        {
            var buttons = GetButtonsInTier(tier);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (i >= options.Count)
                {
                    // A button without an option hides: stale text stays invisible and a click
                    // can't send the previous ability's upgrade instance id.
                    button.Visible = false;
                    button.SetUpgradeTaken(false);
                    button.SetUpgradeInstanceId(string.Empty);
                    button.SetDescription(string.Empty);
                    continue;
                }

                var option = options[i];
                button.Visible = true;
                button.SetUpgradeTaken(option.Selected);
                button.SetUpgradeTier(tier);
                button.SetUpgradeInstanceId(option.UpgradeInstanceId);
                button.SetDescription(option.Description);
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
