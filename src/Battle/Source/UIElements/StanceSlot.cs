namespace Battle.Source.UIElements
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.UI;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;

    [GlobalClass]
    public partial class StanceSlot : TextureButton, IInitializable
    {
        private const string UID = "uid://0hxs4hjfeym6";

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
