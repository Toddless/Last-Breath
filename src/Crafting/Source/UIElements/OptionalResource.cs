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
        [Export] private Button? _add, _remove;
        [Export] private HBoxContainer? _container;
        [Signal] public delegate void AddPressedEventHandler();
        [Signal] public delegate void RemovePressedEventHandler();

        public event Action<ICraftingResource>? ResourceAdded, ResourceRemoved;

        public override void _Ready()
        {
            _add!.Pressed += () => EmitSignal(SignalName.AddPressed);
            _remove!.Pressed += RemoveCraftingResource;
        }

        public void AddCraftingResource(ICraftingResource resource)
        {
            // only one resource can be added
            if (_resource != null) RemoveCraftingResource();
            _resource = resource;
            var templ = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
            templ.SetIcon(_resource.Icon);
            templ.SetText(_resource.DisplayName, 1, 1);
            _container?.AddChild(templ);
            ResourceAdded?.Invoke(_resource);
        }


        public void RemoveCraftingResource()
        {
            if (_resource != null)
            {
                foreach (var child in _container?.GetChildren() ?? [])
                    child.QueueFree();
                ResourceRemoved?.Invoke(_resource);
                _resource = null;
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
