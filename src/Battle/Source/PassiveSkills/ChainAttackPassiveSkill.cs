namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;

    public class ChainAttackPassiveSkill()
        : Skill(id: "Passive_Skill_Chain_Attack")
    {
        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent @event)
        {
            if (Owner == null) return;
            // Обязательно прерываем серию при уклонении.
            // Неизбежные атаки только через модификацию контекста атаки
            if (@event.Context.Rnd.Randf() > Owner.Parameters.AdditionalHit || @event.Context.Result is not AttackResults.Succeed) return;
            @event.Context.CreateReaction(Owner, @event.Context.Target,
                Owner.Parameters.Damage * @event.Context.Rnd.RandfRange(0.9f, 1.1f)).Schedule();
        }

        public override void Detach(IFightable owner) => owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);

        public override ISkill Copy() => new ChainAttackPassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}
