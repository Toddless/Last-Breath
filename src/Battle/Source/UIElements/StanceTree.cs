namespace Battle.Source.UIElements
{
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.UI;
    using Godot;

    public partial class StanceTree : Control, IRequireServices
    {
        private const string UID = "uid://dxxuu85fbrdj";
        private IUiElementsManager? _uiElementsManager;
        [Export] private AbilitySlot[] _abilitySlots = [];
        [Export] private Stance _stance;

        public override void _Ready()
        {
            foreach (AbilitySlot abilitySlot in _abilitySlots)
                abilitySlot.AbilitySelected += OnAbilitySelected;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiElementsManager = provider.GetService<IUiElementsManager>();
            var player = provider.GetService<IPlayerAccessor>();
            int count = 0;
            foreach (IAbility ability in player.Player?.AbilityBook.GetAbilities(_stance) ?? [])
            {
                var slot = _abilitySlots[count];
                slot.SetAbility(ability);
                count++;
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async void OnAbilitySelected(IAbility ability)
        {
            if (_uiElementsManager?.OpenWindow(typeof(AbilityUpgradeWindow)) is AbilityUpgradeWindow window)
            {
                await ToSignal(window, Node.SignalName.Ready);
                window.ShowAbility(ability);
            }
        }
    }
}
