namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Events.GameEvents;

    /// <summary>Item-grant passive (Ether Flow): a share of every mana gain converts into Barrier.</summary>
    public class ManaToBarrierPassiveSkill(float percent)
        : Skill(id: "Passive_Skill_Mana_To_Barrier")
    {
        public float Percent { get; } = percent;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<ManaRestoredEvent>(OnManaRestored);
        }

        private void OnManaRestored(ManaRestoredEvent evt)
        {
            if (Owner == null) return;
            Owner.CurrentBarrier += evt.Amount * Percent;
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<ManaRestoredEvent>(OnManaRestored);
            Owner = null;
        }

        public override ISkill Copy() => new ManaToBarrierPassiveSkill(Percent);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not ManaToBarrierPassiveSkill other) return false;
            return Percent > other.Percent;
        }
    }
}
