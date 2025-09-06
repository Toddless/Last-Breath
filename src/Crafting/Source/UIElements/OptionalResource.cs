namespace Crafting.Source.UIElements
{
    using System;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class OptionalResource : Node
    {
        private const string UID = "uid://cldhk7d6f2k2p";
        private string? _resource;
        private int _amountHave;
        [Export] private Button? _add, _remove;
        [Export] private HBoxContainer? _container;
        private Action<string, int>? _resourceConsumed;

        [Signal] public delegate void AddPressedEventHandler();

        public event Action<string>? ResourceRemoved;

        public override void _Ready()
        {
            if (_add != null) _add.Pressed += OnAddPressed;
            if (_remove != null) _remove.Pressed += RemoveCraftingResource;
        }

        public void AddCraftingResource(string resource, int amountHave, int amountNeed = 1)
        {
            // only one resource can be added
            if (_resource != null) RemoveCraftingResource();
            _resource = resource;
            var templ = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
            var resourceIcon = CraftingResourceProvider.Instance?.GetResourceIcon(resource);
            if (resourceIcon != null) templ.SetIcon(resourceIcon);
            templ.SetText(CraftingResourceProvider.Instance?.GetResourceName(resource) ?? string.Empty, amountHave, amountNeed);
            _resourceConsumed += (displayName, amountHave) =>
            {
                templ.SetText(displayName, amountHave, amountNeed);
            };
            _container?.AddChild(templ);
            _amountHave = amountHave;
        }

        public void ConsumeResource()
        {
            if (_resource != null)
            {
                _amountHave--;
                var displayName = CraftingResourceProvider.Instance?.GetResourceName(_resource) ?? string.Empty;
                _resourceConsumed?.Invoke(displayName, _amountHave);
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

        public override void _ExitTree()
        {
            if (Engine.IsEditorHint()) return;
            if (_add != null) _add.Pressed -= OnAddPressed;
            if (_remove != null) _remove.Pressed -= RemoveCraftingResource;
        }

        private void OnAddPressed() => EmitSignal(SignalName.AddPressed);
    }
}
