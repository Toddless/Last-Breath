namespace Core.Modifiers.Conditions
{
    using System.Linq;
    using Battle.Abilities;
    using Data;
    using Entity;
    using Newtonsoft.Json.Linq;

    /// <summary>Which effects on the owner an <see cref="EffectCondition"/> counts.</summary>
    public enum EffectScope : byte
    {
        /// <summary>Everything the owner carries; negated with a count of one it reads "nothing on me".</summary>
        Any,

        /// <summary>Effects that work for the owner.</summary>
        Buff,

        /// <summary>Effects that work against the owner.</summary>
        Debuff,

        /// <summary>The extra absorption layer, however it was granted.</summary>
        Shield,

        /// <summary>Stacks of one named effect — every stack is its own instance.</summary>
        Stacks,
    }

    /// <summary>
    /// "While I carry at least N of these". A single counting predicate serves the whole family: a
    /// shield holding, N stacks of one effect, N debuffs, N buffs, and — inverted — an owner carrying
    /// nothing at all.
    /// </summary>
    public sealed class EffectCondition(EffectScope scope, string effectId, int count) : OwnerCondition
    {
        protected override bool Evaluate(bool wasMet) => Owner != null && Count(Owner) >= count;

        protected override void Subscribe(IFightable owner)
        {
            owner.Effects.EffectAdded += OnEffectsChanged;
            owner.Effects.EffectRemoved += OnEffectsChanged;
        }

        protected override void Unsubscribe(IFightable owner)
        {
            owner.Effects.EffectAdded -= OnEffectsChanged;
            owner.Effects.EffectRemoved -= OnEffectsChanged;
        }

        private int Count(IFightable owner) => scope switch
        {
            EffectScope.Any => owner.Effects.Effects.Count,
            EffectScope.Buff => owner.Effects.Effects.Count(effect => !effect.IsHarmful),
            EffectScope.Debuff => owner.Effects.Effects.Count(effect => effect.IsHarmful),
            EffectScope.Shield => owner.Effects.Effects.Count(effect => effect is IShieldEffect),
            EffectScope.Stacks => owner.Effects.Effects.Count(effect => effect.Id == effectId),
            _ => 0,
        };

        private void OnEffectsChanged(IEffect effect) => Reevaluate();
    }

    public class EffectConditionFactory : IConditionFactory
    {
        private const int DefaultCount = 1;

        public string Type => ConditionTypes.Effect;

        public OwnerCondition? Create(JObject json)
        {
            var scope = EnumParser.ParseEnum<EffectScope>(json.Value<string>(ConditionFields.Scope) ?? string.Empty);
            string id = json.Value<string>(ConditionFields.Id) ?? string.Empty;
            if (scope == EffectScope.Stacks && id.Length == 0)
            {
                Tracker.TrackError($"Skipping condition '{Type}': scope '{scope}' counts stacks of one effect and needs an '{ConditionFields.Id}'");
                return null;
            }

            int count = json.Value<int?>(ConditionFields.Count) ?? DefaultCount;
            if (count >= DefaultCount) return new EffectCondition(scope, id, count);

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Count}' is {count} — a count below one is met by everyone, invert the entry instead");
            return null;
        }
    }
}
