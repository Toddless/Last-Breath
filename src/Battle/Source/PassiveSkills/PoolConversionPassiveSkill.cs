namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>A keystone that pours one pool into another: <see cref="To"/> gains the whole of
    /// <see cref="From"/> and <see cref="From"/> itself is emptied. The class carries no numbers at all —
    /// the pair of parameters IS the passive, so a conversion is a registration rather than a class of its
    /// own.
    /// <para>The pool is measured unconverted (<see cref="PoolConversionModifier"/>), so the passive never
    /// counts what it has itself given or taken away, and it is re-measured for as long as it is worn: sell
    /// the ring that carried the evasion and the armor bought with it shrinks the same turn.</para>
    /// <para>Everything else reading the emptied parameter reads a zero, deliberately — a pool that was
    /// converted is gone, so a line paid per point of evasion is worth nothing beside a conversion of
    /// evasion.</para></summary>
    public class PoolConversionPassiveSkill : Skill
    {
        private readonly PoolConversionModifier _gain;
        private readonly PoolConversionModifier _drain;

        public PoolConversionPassiveSkill(string id, EntityParameter from, EntityParameter to) : base(id)
        {
            From = from;
            To = to;
            _gain = PoolConversionModifier.Gain(from, to, id);
            _drain = PoolConversionModifier.Drain(from, id);
        }

        /// <summary>The pool given up.</summary>
        public EntityParameter From { get; }

        /// <summary>The pool it becomes.</summary>
        public EntityParameter To { get; }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _gain.ApplyTo(owner);
            _drain.ApplyTo(owner);
        }

        /// <summary>Leaves nothing behind: both halves come off the owner's list and stop watching him, so
        /// the two pools are exactly what they were before the keystone was taken.</summary>
        public override void Detach(IFightable owner)
        {
            _gain.RemoveFrom(owner);
            _drain.RemoveFrom(owner);
            Owner = null;
        }

        public override ISkill Copy() => new PoolConversionPassiveSkill(Id, From, To);

        /// <summary>A conversion moves the whole pool either way, so two registrations of one id are worth
        /// the same and the later one takes the slot.</summary>
        public override bool IsStronger(ISkill skill) => false;
    }
}
