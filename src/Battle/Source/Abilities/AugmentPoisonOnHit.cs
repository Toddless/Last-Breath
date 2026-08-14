namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>
    /// A landing impact poisons what it touched. The sentence belongs to no ability — an attack of a
    /// series, a shard of a volley and a jump of a chain are all impacts, and the rider works on each
    /// of them alike — so the class names none.
    ///
    /// It installs the rider and the two numbers it stands on — the shared
    /// <see cref="AbilityParameter.PoisonDuration"/> and the rider's own potency — as parameters of the
    /// ability. The
    /// second half is what makes the augment composable — a parameter is a thing other augments can
    /// decorate, so "poison lasts a turn longer" lands on the poison THIS augment applies without a
    /// line of code knowing the two were bought together. Numbers held privately by the rider would be
    /// numbers nothing else could reach.
    ///
    /// The keys are given back when the augment leaves, and only the ones this copy actually put there:
    /// an ability that names a poison duration of its own keeps it, the rider reads that one instead,
    /// and taking the augment off must not strip a number the ability was born with.
    /// </summary>
    public class AugmentPoisonOnHit(string id, string[] tags, int tier, float poisonDuration, float poisonPotency)
        : AbilityAugmentImpactRider(id, tags, tier, new PoisonOnHitRider())
    {
        private readonly List<string> _lent = [];

        public override void ApplyUpgrade(Ability ability)
        {
            base.ApplyUpgrade(ability);
            Lend(ability, AbilityParameter.PoisonDuration, poisonDuration);
            Lend(ability, PoisonOnHitRider.Parameters.PoisonPotency, poisonPotency);
        }

        public override void RemoveUpgrade(Ability ability)
        {
            base.RemoveUpgrade(ability);
            foreach (string parameter in _lent) ability.UnregisterParameter(parameter);
            _lent.Clear();
        }

        public override IAbilityAugment Copy() => new AugmentPoisonOnHit(Id, Tags, Tier, poisonDuration, poisonPotency);

        private void Lend(Ability ability, string parameter, float value)
        {
            if (ability.TryRegisterParameter(parameter, value)) _lent.Add(parameter);
        }
    }
}
