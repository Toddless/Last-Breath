namespace Battle.Source.PassiveSkills
{
    using Core;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

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
            if (@event.Context.Result is not AttackResults.Succeed) return;
            if (!ChanceRoll.Roll(Owner.Parameters.AdditionalHit, @event.Context.Rnd, Owner.Parameters.GetChanceLuck(EntityParameter.AdditionalHitChance))) return;
            @event.Context.CreateReaction(Owner, @event.Context.Target,
                Owner.Parameters.PhysicalDamage * @event.Context.Rnd.RandfRange(0.9f, 1.1f)).Schedule();
        }

        public override void Detach(IFightable owner) => owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);

        public override ISkill Copy() => new ChainAttackPassiveSkill();

        /// <summary>Parameterless: the series chance and damage come from the owner's parameters,
        /// not from the skill, so no instance can be stronger than another.</summary>
        public override bool IsStronger(ISkill skill) => false;
    }
}
