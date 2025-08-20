namespace Crafting.Source.UIElements
{
    using System;
    using Core.Interfaces.Crafting;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class OptionalResource : Node
    {
        private const string UID = "uid://cldhk7d6f2k2p";
        private ICraftingResource? _resource;
        private int _amountHave;
        [Export] private Button? _add, _remove;
        [Export] private HBoxContainer? _container;
        private Action<string, int>? _resourceConsumed;
        [Signal] public delegate void AddPressedEventHandler();
        [Signal] public delegate void RemovePressedEventHandler();

        public event Action<ICraftingResource>? ResourceAdded, ResourceRemoved;

        public override void _Ready()
        {
            _add!.Pressed += () => EmitSignal(SignalName.AddPressed);
            _remove!.Pressed += RemoveCraftingResource;
        }

        public void AddCraftingResource(ICraftingResource resource, int amountHave, int amountNeed = 1)
        {
            // only one resource can be added
            if (_resource != null) RemoveCraftingResource();
            _resource = resource;
            var templ = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
            if (_resource.Icon != null) templ.SetIcon(_resource.Icon);
            templ.SetText(_resource.DisplayName, amountHave, amountNeed);
            _resourceConsumed += (displayName, amountHave) =>
            {
                templ.SetText(displayName, amountHave, amountNeed);
            };
            _container?.AddChild(templ);
            ResourceAdded?.Invoke(_resource);
            _amountHave = amountHave;
        }

        public void ConsumeResource()
        {
            if (_resource != null)
            {
                _amountHave--;
                _resourceConsumed?.Invoke(_resource.DisplayName, _amountHave);
            }
        }

        public bool CanClear() => _resource != null && _amountHave < 1;

        public void RemoveCraftingResource()
        {
            if (_resource != null)
            {
                foreach (var child in _container?.GetChildren() ?? [])
                    child.QueueFree();
                ResourceRemoved?.Invoke(_resource);
                _resourceConsumed = null;
                _resource = null;
                _amountHave = 0;
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
