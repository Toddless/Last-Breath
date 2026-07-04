namespace Battle.Source.UIElements
{
    using System;
    using Godot;

    [GlobalClass]
    public partial class UpgradeButton : Button
    {
        private string _upgradeInstanceId = string.Empty;
        private int _upgradeTier;

        public event Action<string, int>? UpgradeSelected;

        public override void _Ready()
        {
            Toggled += OnToggled;
        }

        public void SetUpgradeTier(int upgradeTier) => _upgradeTier = upgradeTier;
        public void SetUpgradeInstanceId(string upgradeInstanceId) => _upgradeInstanceId = upgradeInstanceId;
        public void SetUpgradeTaken(bool isTaken) => SetPressedNoSignal(isTaken);

        private void OnToggled(bool toggledOn)
        {
            if (toggledOn) UpgradeSelected?.Invoke(_upgradeInstanceId, _upgradeTier);
        }
    }
}
