namespace Battle.Source.Abilities.PassiveSkills
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Skills;

    public class VampireAttackPassiveSkill(string id, float percentToLeach)
        : Skill(id)
    {
        public float PercentToLeach { get; } = percentToLeach;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            float toHeal = evnt.Context.FinalDamage * PercentToLeach;
            Owner?.Heal(toHeal);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new VampireAttackPassiveSkill(Id, PercentToLeach);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not VampireAttackPassiveSkill vampire) return false;

            return vampire.PercentToLeach > PercentToLeach;
        }
    }
}
