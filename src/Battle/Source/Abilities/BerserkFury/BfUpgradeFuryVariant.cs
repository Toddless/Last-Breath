namespace Battle.Source.Abilities.BerserkFury
{
    using System;
    using Core.Battle.Abilities;

    /// <summary>L3 upgrades: replace the plain Fury with a variant (Burning / Primal / Healing).</summary>
    public class BfUpgradeFuryVariant(string id, string[] tags, int tier, Func<int, float, IEffect> factory)
        : AbilityUpgrade<BerserkFury>(id, tags, tier)
    {
        private Func<int, float, IEffect>? _previous;

        public override void ApplyUpgrade(BerserkFury ability)
        {
            _previous = ability.FuryFactory;
            ability.FuryFactory = factory;
        }

        public override void RemoveUpgrade(BerserkFury ability)
        {
            if (_previous == null) return;
            ability.FuryFactory = _previous;
            _previous = null;
        }

        public override IAbilityUpgrade Copy() => new BfUpgradeFuryVariant(Id, Tags, Tier, factory);
    }
}
