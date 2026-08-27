namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers;

    /// <summary>"Сила духа": a blow landed on the bearer may hand him back a share of his WHOLE mana pool,
    /// and the mana he regains every turn is cut in return.
    /// <para>The price is a line on <see cref="EntityParameter.ManaRecovery"/>, so it reaches the flat
    /// per-turn regeneration and nothing else: a leech, a refund and this keystone's own return are shares
    /// of something other than that parameter and pass it by.</para>
    /// <para>Only a blow pays: an attack of the pipeline and a hit an ability deals itself (a projectile, a
    /// chain jump, a splash all reach the bearer as one ability hit) each arrive once and are rolled for
    /// once. A damage-over-turn tick is not a blow and is refused — a poison would otherwise pay the
    /// keystone every turn it burns.</para>
    /// <para>The roll is taken before the pool is read, so how much mana the bearer has never decides
    /// whether the stream advances: a pool converted away (Агностик) leaves the keystone paying nothing
    /// while everyone else's rolls stay where they were.</para></summary>
    public class StrengthOfSpiritPassiveSkill : Skill
    {
        public const string PassiveId = "Passive_Skill_Strength_Of_Spirit";

        private readonly IModifierInstance _penalty;

        protected override IReadOnlyDictionary<string, object?> DescriptionValues { get; }

        /// <summary>How often a blow pays (0.35 = 35% of them).</summary>
        public float Chance { get; }

        /// <summary>What a paying blow is worth, as a share of the maximum mana (0.05 = 5%).</summary>
        public float PercentOfMaxMana { get; }

        /// <summary>What the bargain costs, as a multiplicative share of the per-turn mana recovery
        /// (−0.25 = −25%).</summary>
        public float RecoveryPenalty { get; }

        /// <summary>The same price as a CARD states it — a magnitude, because the sentence around it already
        /// carries the direction ("25% less"). Rendering the signed share there would read "−25% less", which
        /// is the cost written twice and pointing both ways.</summary>
        public float RecoveryCut => -RecoveryPenalty;

        public StrengthOfSpiritPassiveSkill(float chance, float percentOfMaxMana, float recoveryPenalty) : base(PassiveId)
        {
            Chance = chance;
            PercentOfMaxMana = percentOfMaxMana;
            RecoveryPenalty = recoveryPenalty;
            _penalty = new SimpleModifier(EntityParameter.ManaRecovery, ModifierValueType.Multiplicative, recoveryPenalty, PassiveId);
            DescriptionValues = new Dictionary<string, object?>
            {
                [nameof(Chance)] = chance,
                [nameof(PercentOfMaxMana)] = percentOfMaxMana,
                [nameof(RecoveryPenalty)] = recoveryPenalty,
                [nameof(RecoveryCut)] = -recoveryPenalty
            };
        }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _penalty.ApplyTo(owner);
            owner.CombatEvents.Subscribe<DamageTakenEvent>(OnDamageTaken);
        }

        /// <summary>Leaves nothing behind: the subscription comes off the bearer's bus and the price comes
        /// off his modifier list, so a refunded keystone stops both halves of the bargain at once.</summary>
        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<DamageTakenEvent>(OnDamageTaken);
            _penalty.RemoveFrom(owner);
            Owner = null;
        }

        public override ISkill Copy() => new StrengthOfSpiritPassiveSkill(Chance, PercentOfMaxMana, RecoveryPenalty);

        /// <summary>A registration is judged by the whole bargain: what a blow is expected to return ranks
        /// first, and where two of them return the same, the one charging less for it wins. Ranking on the
        /// chance alone would hand the slot to a copy that pays more often and for less.</summary>
        public override bool IsStronger(ISkill skill) =>
            skill is StrengthOfSpiritPassiveSkill other
            && (ExpectedReturn > other.ExpectedReturn
                || (ExpectedReturn.Equals(other.ExpectedReturn) && RecoveryPenalty > other.RecoveryPenalty));

        /// <summary>Mana per blow, as a share of the pool: the two halves of the bonus are worth only what
        /// they are worth together.</summary>
        private float ExpectedReturn => Chance * PercentOfMaxMana;

        private void OnDamageTaken(DamageTakenEvent evnt)
        {
            if (Owner is null || !evnt.Target.IsSame(Owner.InstanceId)) return;
            if (!IsBlow(evnt.Context.Cause)) return;
            if (!ChanceRoll.Roll(Chance, CombatRandom.Rolls)) return;

            Owner.RestoreMana(new ManaRecoveryContext(Owner, Owner) { Amount = Owner.Parameters.MaxMana * PercentOfMaxMana });
        }

        /// <summary>Whether the bearer was STRUCK: an attack, or a hit an ability dealt him directly.
        /// A tick, a passive's answer, an item and the environment are damage without a blow behind them.</summary>
        private static bool IsBlow(DamageCause cause) => cause is DamageCause.Attack or DamageCause.Ability;
    }
}
