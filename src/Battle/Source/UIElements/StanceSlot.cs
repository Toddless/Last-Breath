namespace Battle.Source.UIElements
{
    using Core.Enums;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Views.UI;
    using Godot;

    public partial class StanceSlot : TextureButton, IInitializable
    {
        private const string UID = "uid://bke05jg0bjgy1";

        private IBattleEventBus? _battleEventBus;
        public Stance Stance { get; private set; }

        public override void _Ready() => Toggled += OnToggle;

        // Stance passives at this point already activated, no need to raise OnToggle
        public void InitializeStance() => ButtonPressed = true;

        public void SetBattleEventBus(IBattleEventBus battleEventBus) => _battleEventBus = battleEventBus;

        public void SetStance(Stance stance) => Stance = stance;

        public void RemoveBattleEventBus() => _battleEventBus = null;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnToggle(bool toggledOn)
        {
            if (toggledOn) _battleEventBus?.Publish<PlayerChangesStanceEvent>(new(Stance));
        }
    }
}
