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
            { Stance.Intelligence, new Mana()},
            { Stance.Dexterity , new ComboPoints()},
            { Stance.Strength, new Fury()}
        };

        public ResourceComponent(Stance type, ModifierManager modifierManager)
        {
            _currentResource = SetCurrentResource(type);
            _modifierManager = modifierManager;
        }

        public IResource CurrentResource => _currentResource;

        public IResource SetCurrentResource(Stance type)
        {
            if (!_resources.TryGetValue(type, out var resource))
            {
                // TODO: Log
                resource = CreateResource(type);
                _resources[type] = resource;
            }
            return resource;
        }

        public void RecoverCurrentResource()
        {
            _currentResource.RecoveryAmount = Mathf.Max(0, _modifierManager.CalculateFloatValue(_currentResource.GetBaseRecovery(), _currentResource.Parameter));
            _currentResource.Recover();
        }

        public bool IsResourceHasRightType(ResourceType type) => _currentResource.Type == type;

        private static IResource CreateResource(Stance type) => type switch
        {
            Stance.Dexterity => new ComboPoints(),
            Stance.Strength => new Fury(),
            _ => new Mana() // default should be mana
        };
    }
}
