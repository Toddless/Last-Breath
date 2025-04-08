namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class ResourceManager
    {
        // TODO: Add modifiers calculation for resource recovery
        private IResource _currentResource;
        // maybe i should use stance instead of resurceType?
        private readonly Dictionary<Stance, IResource> _resources = new()
        {
            { Stance.Intelligence, new Mana()},
            { Stance.Dexterity , new ComboPoints()},
            { Stance.Strength, new Fury()}
        };

        public ResourceManager(Stance type)
        {
            _currentResource = SetCurrentResource(type);
        }

        public IResource CurrentResource => _currentResource;
        // TODO: How i will recover current resource?
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

        public bool IsResourceHasRightType(ResourceType type) => _currentResource.Type == type;

        private static IResource CreateResource(Stance type) => type switch
        {
            Stance.Dexterity => new ComboPoints(),
            Stance.Strength => new Fury(),
            Stance.Intelligence => new Mana(),
            _ => new Mana()  // TODO: Log 
        };
    }
}
