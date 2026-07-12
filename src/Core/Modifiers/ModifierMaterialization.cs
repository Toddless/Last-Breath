namespace Core.Modifiers
{
    using System;
    using System.Collections.Generic;

    /// <summary>Where a materialized line lands. The item implements this for its rolled ("additional") bucket;
    /// <see cref="CollectingSink"/> accumulates for template parse.</summary>
    public interface IModifierSink
    {
        void AddEntity(IModifierInstance modifier);
        void AddContext(ContextModifierEntry entry);
    }

    /// <summary>Accumulates materialized lines into entity/context buckets — used to build an item's authored
    /// implicits/modifiers from parsed descriptors.</summary>
    public sealed class CollectingSink : IModifierSink
    {
        public List<IModifierInstance> Entities { get; } = [];
        public List<ContextModifierEntry> Contexts { get; } = [];

        public void AddEntity(IModifierInstance modifier) => Entities.Add(modifier);
        public void AddContext(ContextModifierEntry entry) => Contexts.Add(entry);
    }

    public interface IModifierMaterializer
    {
        void Materialize(IModifierDescriptor descriptor, IModifierSink sink, string source);
    }

    /// <summary>Registry-dispatched materialization: each descriptor kind has a handler, new kind = new entry.
    /// Always mints fresh instances, so pool descriptors stay pristine.</summary>
    public sealed class ModifierMaterializer : IModifierMaterializer
    {
        private readonly IReadOnlyDictionary<Type, Action<IModifierDescriptor, IModifierSink, string>> _handlers;

        public ModifierMaterializer()
        {
            _handlers = new Dictionary<Type, Action<IModifierDescriptor, IModifierSink, string>>
            {
                [typeof(ParameterDescriptor)] = (descriptor, sink, source) => MaterializeParameter((ParameterDescriptor)descriptor, sink, source),
                [typeof(ContextDescriptor)] = (descriptor, sink, _) => MaterializeContext((ContextDescriptor)descriptor, sink),
                [typeof(CompositeDescriptor)] = (descriptor, sink, source) => MaterializeComposite((CompositeDescriptor)descriptor, sink, source),
            };
        }

        public void Materialize(IModifierDescriptor descriptor, IModifierSink sink, string source)
        {
            if (_handlers.TryGetValue(descriptor.GetType(), out var handler)) handler(descriptor, sink, source);
            else Tracker.TrackError($"No materializer registered for descriptor {descriptor.GetType().Name}");
        }

        private static void MaterializeParameter(ParameterDescriptor descriptor, IModifierSink sink, string source)
        {
            var modifier = ModifiersCreator.CreateModifierInstance(descriptor.Parameter, descriptor.ValueType, descriptor.Value, source);
            modifier.Scope = descriptor.Scope;
            sink.AddEntity(modifier);
        }

        private static void MaterializeContext(ContextDescriptor descriptor, IModifierSink sink) =>
            sink.AddContext(new ContextModifierEntry(descriptor.Parameter, descriptor.ValueType, descriptor.Value, descriptor.Weight));

        private void MaterializeComposite(CompositeDescriptor descriptor, IModifierSink sink, string source)
        {
            foreach (var part in descriptor.Parts) Materialize(part, sink, source);
        }
    }
}
