namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>"Accelerator": a landed critical attack reduces the cooldown of one random
    /// cooling-down ability by <c>amount</c>.</summary>
    public class AcceleratorPassiveSkill(int amount = 1) : Skill(id: "Passive_Skill_Accelerator")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(Amount)] = Amount,
                };
                return field;
            }
        }

        public int Amount { get; } = amount;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            var context = evt.Context;
            if (Owner == null || context.Result is not AttackResults.Succeed) return;
            if (context is { IsCritical: false, ForceCriticalAttack: false }) return;

            List<IAbility> coolingDown = Owner.AbilityBook.AllAbilities.Where(ability => ability.CooldownLeft > 0).ToList();
            if (coolingDown.Count == 0) return;

            IAbility lucky = coolingDown[context.Rnd.RandiRange(0, coolingDown.Count - 1)];
            lucky.CooldownLeft = System.Math.Max(0, lucky.CooldownLeft - Amount);
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new AcceleratorPassiveSkill(Amount);

        public override bool IsStronger(ISkill skill) =>
            skill is AcceleratorPassiveSkill accelerator && accelerator.Amount > Amount;
    }
}
