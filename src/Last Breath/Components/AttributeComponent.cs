namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Playground.Components.Interfaces;

    public class AttributeComponent
    {
        private readonly Dictionary<Type, IAttribute> _attributes = [];

        public T? GetAttribute<T>() where T : class, IAttribute => _attributes.TryGetValue(typeof(T), out var attribute) ? attribute as T : null;

        public void AddAttribute<T>(T attribute) where T : IAttribute
        {
            _attributes[typeof(T)] = attribute;
            attribute.UpdateModifiers();
        }
    }
}
