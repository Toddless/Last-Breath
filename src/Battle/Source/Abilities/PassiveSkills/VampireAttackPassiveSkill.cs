namespace Battle.Source.Abilities.PassiveSkills
{
    using CombatEvents;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Godot;

    public class VampireAttackPassiveSkill(string id, float percentToLeach)
        : Skill(id)
    {
        public float PercentToLeach { get; } = percentToLeach;

        public override void Attach(IEntity owner)
        {
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            float toHeal = evnt.Context.FinalDamage * PercentToLeach;
            evnt.Source.Heal(toHeal);
            GD.Print($"Was leached: {toHeal}");
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override ISkill Copy() => new VampireAttackPassiveSkill(Id, PercentToLeach);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not VampireAttackPassiveSkill vampire) return false;

            return vampire.PercentToLeach > PercentToLeach;
        }
    }
}
