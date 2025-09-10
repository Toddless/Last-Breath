namespace Crafting.Source.UIElements
{
    using System;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class OptionalResource : BaseResource
    {
        private const string UID = "uid://cldhk7d6f2k2p";
        [Export] private Button? _add, _remove;

        [Signal] public delegate void AddPressedEventHandler();

        public event Action<string>? ResourceRemoved;

        public override void _Ready()
        {
            if (_add != null) _add.Pressed += OnAddPressed;
            if (_remove != null) _remove.Pressed += RemoveCraftingResource;
        }

        public override void AddCraftingResource(string resource, Func<string, int> amounHave, int amountNeed = 1)
        {
            if (!string.IsNullOrWhiteSpace(ResourceId)) RemoveCraftingResource();
            base.AddCraftingResource(resource, amounHave, amountNeed);
        }

        public override void RemoveCraftingResource()
        {
            if (!string.IsNullOrWhiteSpace(ResourceId))
            {
                ResourceRemoved?.Invoke(ResourceId);
                base.RemoveCraftingResource();
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void _ExitTree()
        {
            if (Engine.IsEditorHint()) return;
            if (_add != null) _add.Pressed -= OnAddPressed;
            if (_remove != null) _remove.Pressed -= RemoveCraftingResource;
        }

        private void OnAddPressed() => EmitSignal(SignalName.AddPressed);
    }
}
