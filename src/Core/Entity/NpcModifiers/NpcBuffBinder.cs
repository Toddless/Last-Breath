namespace Core.Entity.NpcModifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.NpcBuffsData;
    using Entity;
    using Enums;
    using Items;
    using Items.Grants;

    /// <summary>
    /// The consumer NpcBuffId was written for. A modifier says WHICH buff it carries; this turns that into
    /// power the bearer actually has: parameter modifiers stamped with the modifier's instance id (so they
    /// leave exactly when it does) and grants minted through the same <see cref="IGrantFactory"/> gear uses,
    /// so a granted passive reaches the NPC down the road already proven by items.
    /// <para>Everything runs through <see cref="Rebuild"/> rather than at attach time: a scaling modifier
    /// raises TotalScale of every peer as it arrives, and a value bound before it landed would keep the
    /// scale it happened to see.</para>
    /// <para>Every bound line is worth base × totalScale and nothing else. The bearer's level never
    /// multiplies a line here: the catalog speaks in Increase percentages, which self-scale with the
    /// level-grown base parameters, so a level factor on top would count the growth twice. Grants stay
    /// whole for the same magnitude reason TotalScale never touches them (see <see cref="BindGrant"/>).</para>
    /// </summary>
    public sealed class NpcBuffBinder(INpcBuffProvider buffs, IGrantFactory? grants = null, Action<string>? report = null) : INpcBuffBinder
    {
        private const float ScaleEpsilon = 0.0001f;

        private readonly Dictionary<string, Binding> _bound = [];

        /// <summary>Rebinding is a full Detach+Attach, so a granted passive holding state of its own would
        /// have it RESET here (stack counters, subscriptions). Unreachable on the spawn path as it stands —
        /// the whole batch settles before anything is bound, and nothing adds a scaling modifier to a
        /// fighting NPC — but anything that does so mid-battle later has to look at this line first.</summary>
        public void Rebuild(IFightable owner, IReadOnlyList<INpcModifier> modifiers)
        {
            var live = modifiers.ToDictionary(modifier => modifier.InstanceId);

            foreach (string instanceId in _bound.Keys.ToList())
            {
                bool stillHere = live.TryGetValue(instanceId, out INpcModifier? modifier);
                if (stillHere && Math.Abs(_bound[instanceId].Scale - modifier!.TotalScale) < ScaleEpsilon) continue;
                Unbind(owner, instanceId);
            }

            foreach (INpcModifier modifier in modifiers.Where(modifier => !_bound.ContainsKey(modifier.InstanceId)))
                Bind(owner, modifier);
        }

        private void Bind(IFightable owner, INpcModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.NpcBuffId)) return; // a modifier that promises the bearer nothing

            if (!buffs.HasBuff(modifier.NpcBuffId))
            {
                // The silent death this pass exists to end: an id pointing at nothing used to read exactly
                // like a buff with no lines, and the NPC just stayed weak.
                Report($"NPC buff '{modifier.NpcBuffId}' named by modifier '{modifier.Id}'");
                return;
            }

            float scale = modifier.TotalScale;
            var binding = new Binding(scale);

            foreach (var line in buffs.CreateModifiers(modifier.NpcBuffId, modifier.InstanceId))
            {
                line.Value *= scale;
                owner.ParameterModifiers.AddModifier(line);
            }

            foreach (NpcBuffGrantData grant in buffs.GetGrants(modifier.NpcBuffId))
                BindGrant(owner, modifier, grant, binding);

            _bound[modifier.InstanceId] = binding;
        }

        // Granted passives are handed over WHOLE. This is a DELIBERATE break with the item convention, where
        // a grant does scale (WithScaledValues, the ascension's +15%), and the reason is magnitude: that
        // factor is 1.15, while TotalScale here reaches 6.1 with the scaling modifiers stacked. Their
        // properties are thresholds and durations, so 6.1 does not make Execute stronger — 0.3 × 6.1 puts
        // its threshold above full health and every hit becomes a kill. Scaling these needs its own curve,
        // not the budget multiplier.
        private void BindGrant(IFightable owner, INpcModifier modifier, NpcBuffGrantData grant, Binding binding)
        {
            if (string.IsNullOrEmpty(grant.Id))
            {
                Report($"grant without an id in the buff of modifier '{modifier.Id}'");
                return;
            }

            if (!Enum.TryParse(grant.Kind, ignoreCase: true, out GrantKind kind))
            {
                Report($"grant kind '{grant.Kind}' of '{grant.Id}' in the buff of modifier '{modifier.Id}'");
                return;
            }

            IItemGrant? minted = grants?.Create(kind, grant.Id, [], grant.Properties);
            if (minted == null) return; // no factory in this composition, or a kind it refuses — it reports itself

            minted.Attach(owner);
            binding.Grants.Add(minted);
        }

        private void Unbind(IFightable owner, string instanceId)
        {
            if (!_bound.Remove(instanceId, out Binding? binding)) return;

            owner.ParameterModifiers.RemoveModifierBySource(instanceId);
            foreach (IItemGrant grant in binding.Grants)
                grant.Detach(owner);
        }

        private void Report(string message)
        {
            if (report != null)
            {
                report(message);
                return;
            }

            Tracker.TrackNotFound(message, this);
        }

        private sealed class Binding(float scale)
        {
            public float Scale { get; } = scale;

            public List<IItemGrant> Grants { get; } = [];
        }
    }
}
