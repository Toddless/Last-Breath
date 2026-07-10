namespace LastBreath.UI
{
    using System;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    public partial class CharacterWindow : Control, IWindow
    {
        private const string UID = "uid://b7ndt5b1q2dif";

        [Export] private VBoxContainer? _ranks;
        private IFactionRelationService? _relations;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void InjectServices(IGameServiceProvider provider)
        {
            _relations = provider.GetService<IFactionRelationService>();
            _relations.PlayerReputationChanged += OnReputationChanged;
            RenderReputation();
        }

        public void Close() => GetParent().RemoveChild(this);

        public override void _ExitTree()
        {
            if (_relations != null) _relations.PlayerReputationChanged -= OnReputationChanged;
        }

        private void OnReputationChanged(ReputationChangedArgs change) => RenderReputation();

        private void RenderReputation()
        {
            if (_ranks == null || _relations == null) return;

            foreach (var child in _ranks.GetChildren())
                child.QueueFree();

            foreach (Fractions faction in Enum.GetValues<Fractions>())
            {
                if (!_relations.HasReputation(faction)) continue;

                var row = new Label
                {
                    Text = $"{Localization.Localize($"Fraction_{faction}")}: " +
                           $"{Localization.Localize($"RelationLevel_{_relations.GetPlayerRelation(faction)}")} " +
                           $"({_relations.GetReputation(faction)})",
                };
                _ranks.AddChild(row);
            }
        }
    }
}
