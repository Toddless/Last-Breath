namespace Core.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Conditions;
    using Entity.Components;
    using Enums;
    using Interfaces;
    using Items;
    using Items.Grants;

    /// <summary>Where a materialized descriptor lands: lines in the entity/context buckets, rolled grants in
    /// their own. <see cref="CollectingSink"/> accumulates for template parse and for the crafting rolls.</summary>
    public interface IModifierSink
    {
        void AddEntity(IModifierInstance modifier);
        void AddContext(ContextModifierEntry entry);

        /// <summary>A rolled grant (passive/effect): behaviour that is not a line at all. Kept in its own
        /// bucket so a caller that only knows how to place lines cannot silently swallow it.</summary>
        void AddGrant(IItemGrant grant);
    }

    /// <summary>Accumulates materialized lines into entity/context buckets — used to build an item's authored
    /// implicits/modifiers from parsed descriptors.</summary>
    public sealed class CollectingSink : IModifierSink
    {
        public List<IModifierInstance> Entities { get; } = [];
        public List<ContextModifierEntry> Contexts { get; } = [];
        public List<IItemGrant> Grants { get; } = [];

        public void AddEntity(IModifierInstance modifier) => Entities.Add(modifier);
        public void AddContext(ContextModifierEntry entry) => Contexts.Add(entry);
        public void AddGrant(IItemGrant grant) => Grants.Add(grant);
    }

    public interface IModifierMaterializer
    {
        void Materialize(IModifierDescriptor descriptor, IModifierSink sink, string source);
    }

    /// <summary>The one rule for turning the condition id a line carries into the predicate that answers
    /// for it — shared by every path that builds a line from stored data (a rolled pool entry, a restored
    /// save). A line that names no condition simply has none; a line that names one the host cannot build
    /// is refused, so the bonus it was written to gate is never handed out ungated.</summary>
    public static class LineConditions
    {
        public static bool TryBuild(IConditionProvider? catalog, string? conditionId, out ICondition? condition)
        {
            condition = null;
            if (string.IsNullOrWhiteSpace(conditionId)) return true;

            if (catalog == null)
            {
                Tracker.TrackError($"Dropping a line held up by condition '{conditionId}': this host has no condition catalog");
                return false;
            }

            return catalog.TryResolve(conditionId, out condition);
        }

        /// <summary>Why a predicate may not hold up this entry, or null when it may. Only a stat line has
        /// something to be held up: a grant is behaviour that answers for itself, an operation is applied
        /// once, and a pipeline knob is handed on as the entry alone with no room for a predicate beside it.
        /// A composite is ONE player-facing line, so it answers for its whole content — a bundle holding a
        /// part that cannot be held up is refused entirely rather than reaching the item as the half of an
        /// authored line that happens to fit.</summary>
        public static string? WhyCannotBeHeldUp(IModifierDescriptor descriptor) => descriptor switch
        {
            ParameterDescriptor => null,
            CompositeDescriptor composite => composite.Parts.Select(WhyCannotBeHeldUp).FirstOrDefault(refusal => refusal != null),
            ContextDescriptor context => $"pipeline knob '{context.Parameter}' is handed on as the entry alone and would come back applying always",
            GrantDescriptor grant => WhyGrantCannotBeHeldUp(grant.GrantId),
            UpgradeLevelsDescriptor => "a sharpening-levels operation is applied once",
            _ => $"{descriptor.GetType().Name} is not a stat line",
        };

        /// <summary>The answer for everything a grant owns, asked from both sides of it: the pool entry that
        /// rolls the grant, and the lines an item's grant is authored from. Those lines are minted with the
        /// grant and land on the wearer through <see cref="Items.Grants.ModifierGrant.Attach"/> as its own
        /// content — never as entries with a channel of their own for a predicate to travel in.</summary>
        public static string WhyGrantCannotBeHeldUp(string grantId) =>
            $"granted behaviour '{grantId}' answers for itself";
    }

    /// <summary>Kind-dispatched materialization (new descriptor kind = new switch arm). Always mints fresh
    /// instances, so pool descriptors stay pristine. The single point where a value range becomes a concrete
    /// number: ranges roll on the injected RNG, fixed values pass through bit-identical and consume no roll
    /// (seeded sequences must not shift for legacy single-value content). Each line is stamped with its roll
    /// provenance: Affix, the source range, and — for composite parts — a shared GroupId.
    /// <para>It is also where a named condition becomes a predicate. The catalog answers per line, so every
    /// line — each part of a composite included — gets an instance of its own to point at an owner. An entry
    /// naming a condition no catalog holds produces NO line at all: a bonus written to be paid for must not
    /// reach the item as an unconditional one, and an entry with nothing a predicate could hold up (a grant,
    /// an operation, a pipeline knob — a composite hiding one of those included) is refused WHOLE.</para></summary>
    public sealed class ModifierMaterializer(IRandomNumberGenerator rnd, IGrantFactory? grantFactory = null, IConditionProvider? conditions = null)
        : IModifierMaterializer
    {
        // Group affix/id and the root's condition stamped onto every line of one composite roll; empty for
        // atomic descriptors. A composite is one player-facing line, so one predicate gates all of its parts.
        private readonly record struct LineStamp(AffixKind? Affix, string? GroupId, string? Condition);

        public void Materialize(IModifierDescriptor descriptor, IModifierSink sink, string source) =>
            Materialize(descriptor, sink, source, default);

        private void Materialize(IModifierDescriptor descriptor, IModifierSink sink, string source, LineStamp stamp)
        {
            switch (descriptor)
            {
                case ParameterDescriptor parameter: MaterializeParameter(parameter, sink, source, stamp); break;
                case ContextDescriptor context: MaterializeContext(context, sink, stamp); break;
                case CompositeDescriptor composite: MaterializeComposite(composite, sink, source, stamp); break;
                case GrantDescriptor grant: MaterializeGrant(grant, sink, stamp); break;
                default: Tracker.TrackError($"No materializer registered for descriptor {descriptor.GetType().Name}"); break;
            }
        }

        // Grants are minted by the one factory like every other grant (data, save, effect catalog). A host
        // without the factory (sandbox, tool) says so out loud instead of dropping the entry in silence.
        // A grant named under a condition — its own or the one its composite root carries — is refused
        // rather than minted ungated: a behaviour handed out for good is not what "while wounded" bought.
        private void MaterializeGrant(GrantDescriptor descriptor, IModifierSink sink, LineStamp stamp)
        {
            if (Refuse(descriptor, stamp.Condition ?? descriptor.Condition)) return;

            if (grantFactory == null)
            {
                Tracker.TrackError($"Cannot materialize grant '{descriptor.GrantId}': this host has no grant factory");
                return;
            }

            var grant = grantFactory.Create(descriptor.Kind, descriptor.GrantId, [], descriptor.Properties);
            if (grant == null) return; // the factory already reported the unknown kind/id
            sink.AddGrant(grant);
        }

        private void MaterializeParameter(ParameterDescriptor descriptor, IModifierSink sink, string source, LineStamp stamp)
        {
            string? conditionId = stamp.Condition ?? descriptor.Condition;
            if (!LineConditions.TryBuild(conditions, conditionId, out var condition)) return;

            var modifier = ModifiersCreator.CreateModifierInstance(descriptor.Parameter, descriptor.ValueType, descriptor.Value.Roll(rnd), source);
            modifier.Scope = descriptor.Scope;
            if (modifier is SimpleModifier simple)
            {
                simple.Affix = stamp.Affix ?? descriptor.Affix;
                simple.GroupId = stamp.GroupId;
                simple.RolledRange = descriptor.Value.IsFixed ? null : descriptor.Value;
                simple.ConditionId = conditionId;
                simple.Condition = condition;
            }

            sink.AddEntity(modifier);
        }

        // A pipeline line is handed around as the entry alone (item copy, reroll insert, save), so a
        // predicate given to one here would be lost by the first of those paths and the line would come
        // back applying always. Until the entry itself can carry one, such a line is refused out loud.
        private void MaterializeContext(ContextDescriptor descriptor, IModifierSink sink, LineStamp stamp)
        {
            if (Refuse(descriptor, stamp.Condition ?? descriptor.Condition)) return;

            sink.AddContext(new ContextModifierEntry(descriptor.Parameter, descriptor.ValueType, descriptor.Value.Roll(rnd), descriptor.Weight)
            {
                Affix = stamp.Affix ?? descriptor.Affix,
                GroupId = stamp.GroupId,
                RolledRange = descriptor.Value.IsFixed ? null : descriptor.Value,
            });
        }

        // All parts of one composite share a GroupId (they present as a single line) and inherit the ROOT
        // affix and condition — parts carry neither by parse contract. A nested composite keeps the
        // outermost group. A bundle held up by a condition is weighed WHOLE before any part is minted: one
        // line either arrives gated in full or does not arrive.
        private void MaterializeComposite(CompositeDescriptor descriptor, IModifierSink sink, string source, LineStamp stamp)
        {
            if (Refuse(descriptor, stamp.Condition ?? descriptor.Condition)) return;

            var groupStamp = new LineStamp(
                stamp.Affix ?? descriptor.Affix,
                stamp.GroupId ?? Guid.NewGuid().ToString(),
                stamp.Condition ?? descriptor.Condition);
            foreach (var part in descriptor.Parts) Materialize(part, sink, source, groupStamp);
        }

        /// <summary>The fail-closed answer to "this entry names a condition it has nothing to hold up":
        /// nothing is minted and the reason is reported. Data is refused at parse already — this is the
        /// second gate, for descriptors that never went through it (a pool built in code, a tool).</summary>
        private static bool Refuse(IModifierDescriptor descriptor, string? conditionId)
        {
            if (string.IsNullOrWhiteSpace(conditionId)) return false;
            if (LineConditions.WhyCannotBeHeldUp(descriptor) is not { } refusal) return false;

            Tracker.TrackError($"Skipping a modifier held up by condition '{conditionId}': {refusal}");
            return true;
        }
    }
}
