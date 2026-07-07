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
        private Stance _stance;

        public override void _Ready() => Toggled += OnToggle;

        public void SetBattleEventBus(IBattleEventBus battleEventBus) => _battleEventBus = battleEventBus;

        public void SetStance(Stance stance) => _stance = stance;

        public void RemoveBattleEventBus() => _battleEventBus = null;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnToggle(bool toggledOn)
        {
            if (toggledOn) _battleEventBus?.Publish<PlayerChangesStanceEvent>(new(_stance));
        }
    }
}
