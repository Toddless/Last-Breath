namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Data;
    using Core.Enums;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Renders one stance's mastery tree. Fills its fixed threshold slots from a domain request
    /// (all stance abilities with unlock state) and, on an available slot click, requests the detail
    /// view and shows it in the shared upgrade window. Holds no domain objects.
    /// </summary>
    public partial class StanceTree : Control, IRequireServices
    {
        private const string UID = "uid://dxxuu85fbrdj";

        [Export] private AbilitySlot[] _abilitySlots = [];
        [Export] private Stance _stance;
        [Export] private Label? _name;

        private IUiElementsManager? _uiElementsManager;
        private IGameMessageBus? _messageBus;

        public override void _Ready()
        {
            foreach (AbilitySlot abilitySlot in _abilitySlots)
                abilitySlot.AbilitySelected += OnAbilitySelected;
            _name?.Text = $"{_stance}";
        }

        public override void _ExitTree()
        {
            foreach (AbilitySlot slot in _abilitySlots)
                slot.AbilitySelected -= OnAbilitySelected;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiElementsManager = provider.GetService<IUiElementsManager>();
            _messageBus = provider.GetService<IGameMessageBus>();
            FillSlotsAsync();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async void FillSlotsAsync()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_messageBus);
                var views = await _messageBus.SendRequest<GetStanceAbilityRequest, IReadOnlyList<AbilitySlotView>>(
                    new GetStanceAbilityRequest(_stance));

                for (int i = 0; i < _abilitySlots.Length && i < views.Count; i++)
                    _abilitySlots[i].SetView(views[i]);
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to fill ability slots", ex, this);
                GD.Print($"Failed to fill ability slots: {ex.Message}, {ex.StackTrace}");
            }
        }

        private async void OnAbilitySelected(string abilityId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_messageBus);
                ArgumentNullException.ThrowIfNull(_uiElementsManager);
                var view = await _messageBus.SendRequest<GetAbilityUpgradeViewRequest, AbilityUpgradeView>(
                    new GetAbilityUpgradeViewRequest(abilityId));

                if (_uiElementsManager.OpenWindow(typeof(AbilityUpgradeWindow)) is not AbilityUpgradeWindow window) return;
                window.Show(view);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to open upgrade window", e, this);
                GD.Print("Failed to open upgrade window");
            }
        }
    }
}
