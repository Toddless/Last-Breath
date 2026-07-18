namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    /// <summary>"Decomposition": every landed attack puts a stack of armor reduction on the target
    /// (<c>reduceBy</c> per stack, capped by <c>maxStacks</c> — 5%×5 = the −25% ceiling).</summary>
    public class DecompositionPassiveSkill(int duration, int maxStacks, float reduceBy)
        : Skill(id: "Passive_Skill_Decomposition")
    {
        public float ReduceBy { get; } = reduceBy;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Owner == null || evt.Context.Result is not AttackResults.Succeed) return;
            _ = new ArmorReductionEffect(duration, maxStacks, ReduceBy)
                .Apply(new EffectApplyingContext { Caster = Owner, Target = evt.Context.Target, Source = InstanceId });
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new DecompositionPassiveSkill(duration, maxStacks, ReduceBy);

        public override bool IsStronger(ISkill skill) =>
            skill is DecompositionPassiveSkill decomposition && decomposition.ReduceBy > ReduceBy;
    }
}
