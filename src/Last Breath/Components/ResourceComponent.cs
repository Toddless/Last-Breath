namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class ResourceComponent
    {
        private IResource _currentResource;
        private readonly ModifierManager _modifierManager;
        // maybe i should use stance instead of resurceType?
        private readonly Dictionary<Stance, IResource> _resources = [];

        public IResource CurrentResource => _currentResource;

        public event Action<float>? CurrentResourceValueChanges;

        public event Action<ResourceType, float, float>? CurrentResourceTypeChanges;
        public ResourceComponent(Stance type, ModifierManager modifierManager)
        {
            _currentResource = InitialResource(type);
            _modifierManager = modifierManager;
            _modifierManager.ParameterModifiersChanged += OnParameterChanges;
            SetEvents();
        }

        public void SetCurrentResource(Stance type)
        {
            if (!_resources.TryGetValue(type, out var resource))
            {
                // TODO: Log
                resource = CreateResource(type);
                _resources[type] = resource;
            }
            _currentResource.CurrentChanges -= OnCurrentChanges;
            _currentResource = resource;
            _currentResource.CurrentChanges += OnCurrentChanges;
            CurrentResourceTypeChanges?.Invoke(_currentResource.Type, _currentResource.Current, _currentResource.MaximumAmount);
        }

        public void HandleResourceRecoveryEvent(RecoveryEventContext context) => _currentResource.HandleRecoveryEvent(context);

        public ResourceType GetCurrentResource() => _currentResource.Type;

        public bool IsResourceHasRightType(ResourceType type) => _currentResource.Type == type;

        private void OnParameterChanges(Parameter parameter)
        {
            if (parameter != Parameter.Resource) return;
            CurrentResource.RecoveryAmount = Mathf.Max(0, Calculations.CalculateFloatValue(CurrentResource.GetBaseRecovery(), _modifierManager.GetCombinedModifiers(Parameter.Resource)));
        }

        private void SetEvents()
        {
            _modifierManager.ParameterModifiersChanged += OnParameterChanges;
            _currentResource.CurrentChanges += OnCurrentChanges;
        }

        private IResource InitialResource(Stance type)
        {
            var resource = CreateResource(type);
            if (_resources.ContainsValue(resource))
            {
                _resources[type] = resource;
            }
            return resource;
        }


        private void OnCurrentChanges(float obj) => CurrentResourceValueChanges?.Invoke(obj);

        private static IResource CreateResource(Stance type) => type switch
        {
            Stance.Dexterity => new ComboPoints(),
            Stance.Strength => new Fury(),
            _ => new Mana() // default should be mana
        };
    }
}
