namespace Core.Modifiers
{
    using System;
    using System.Collections.Generic;
    using Entity.Components;
    using Enums;

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

    /// <summary>Kind-dispatched materialization (new descriptor kind = new switch arm). Always mints fresh
    /// instances, so pool descriptors stay pristine. The single point where a value range becomes a concrete
    /// number: ranges roll on the injected RNG, fixed values pass through bit-identical and consume no roll
    /// (seeded sequences must not shift for legacy single-value content). Each line is stamped with its roll
    /// provenance: Affix, the source range, and — for composite parts — a shared GroupId.</summary>
    public sealed class ModifierMaterializer(IRandomNumberGenerator rnd) : IModifierMaterializer
    {
        // Group affix/id stamped onto every line of one composite roll; empty for atomic descriptors.
        private readonly record struct LineStamp(AffixKind? Affix, string? GroupId);

        public void Materialize(IModifierDescriptor descriptor, IModifierSink sink, string source) =>
            Materialize(descriptor, sink, source, default);

        private void Materialize(IModifierDescriptor descriptor, IModifierSink sink, string source, LineStamp stamp)
        {
            switch (descriptor)
            {
                case ParameterDescriptor parameter: MaterializeParameter(parameter, sink, source, stamp); break;
                case ContextDescriptor context: MaterializeContext(context, sink, stamp); break;
                case CompositeDescriptor composite: MaterializeComposite(composite, sink, source, stamp); break;
                default: Tracker.TrackError($"No materializer registered for descriptor {descriptor.GetType().Name}"); break;
            }
        }

        private void MaterializeParameter(ParameterDescriptor descriptor, IModifierSink sink, string source, LineStamp stamp)
        {
            var modifier = ModifiersCreator.CreateModifierInstance(descriptor.Parameter, descriptor.ValueType, descriptor.Value.Roll(rnd), source);
            modifier.Scope = descriptor.Scope;
            if (modifier is SimpleModifier simple)
            {
                simple.Affix = stamp.Affix ?? descriptor.Affix;
                simple.GroupId = stamp.GroupId;
                simple.RolledRange = descriptor.Value.IsFixed ? null : descriptor.Value;
            }

            sink.AddEntity(modifier);
        }

        private void MaterializeContext(ContextDescriptor descriptor, IModifierSink sink, LineStamp stamp) =>
            sink.AddContext(new ContextModifierEntry(descriptor.Parameter, descriptor.ValueType, descriptor.Value.Roll(rnd), descriptor.Weight)
            {
                Affix = stamp.Affix ?? descriptor.Affix,
                GroupId = stamp.GroupId,
                RolledRange = descriptor.Value.IsFixed ? null : descriptor.Value,
            });

        // All parts of one composite share a GroupId (they present as a single line) and inherit the ROOT
        // affix — parts carry none by parse contract. A nested composite keeps the outermost group.
        private void MaterializeComposite(CompositeDescriptor descriptor, IModifierSink sink, string source, LineStamp stamp)
        {
            var groupStamp = new LineStamp(stamp.Affix ?? descriptor.Affix, stamp.GroupId ?? Guid.NewGuid().ToString());
            foreach (var part in descriptor.Parts) Materialize(part, sink, source, groupStamp);
        }
    }
}
