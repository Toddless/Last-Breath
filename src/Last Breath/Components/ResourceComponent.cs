namespace Playground.Components
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class ResourceComponent
    {
        private IResource _currentResource;
        private readonly ModifierManager _modifierManager;
        // maybe i should use stance instead of resurceType?
        private readonly Dictionary<Stance, IResource> _resources = new()
        {
            // TODO: for enemy i need only one stance, all 3 Resources make no sense
            { Stance.Intelligence, new Mana()},
            { Stance.Dexterity , new ComboPoints()},
            { Stance.Strength, new Fury()}
        };

        public IResource CurrentResource => _currentResource;

        public ResourceComponent(Stance type, ModifierManager modifierManager)
        {
            _currentResource = CreateResource(type);
            _modifierManager = modifierManager;
            _modifierManager.ParameterModifiersChanged += OnParameterChanges;
        }


        public void SetCurrentResource(Stance type)
        {
            if (!_resources.TryGetValue(type, out var resource))
            {
                // TODO: Log
                resource = CreateResource(type);
                _resources[type] = resource;
            }
            _currentResource = resource;
        }

        public ResourceType GetCurrentResource() => _currentResource.Type;

        public bool IsResourceHasRightType(ResourceType type) => _currentResource.Type == type;

        private void OnParameterChanges(Parameter parameter)
        {
            if (parameter != Parameter.Resource) return;
            CurrentResource.RecoveryAmount = Mathf.Max(0, Calculations.CalculateFloatValue(CurrentResource.GetBaseRecovery(), _modifierManager.GetCombinedModifiers(Parameter.Resource)));
        }

        private static IResource CreateResource(Stance type) => type switch
        {
            Stance.Dexterity => new ComboPoints(),
            Stance.Strength => new Fury(),
            _ => new Mana() // default should be mana
        };
    }
}
