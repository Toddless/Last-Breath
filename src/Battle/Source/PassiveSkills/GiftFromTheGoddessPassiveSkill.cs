namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Abilities.Effects;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class GiftFromTheGoddessPassiveSkill(float chance)
        : Skill(id: "Passive_Skill_Gift_From_The_Goddess")
    {
        private readonly List<IEffect> _effects =
        [
            new RegenerationEffect(150, 3, 5),
            new DamageOverTurnEffect(3, 3, default, StatusEffects.Bleed),
            new DamageOverTurnEffect(3, 3, default, StatusEffects.Poison),
            new DamageOverTurnEffect(3, 3, default, StatusEffects.Burning),
            new ExecutionEffect(3, 3, 0.15f),
            new LuckyCritChanceEffect(3)
        ];

        public float Chance { get; } = chance;

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            var rnd = obj.Context.Rnd;
            if ((rnd.RandFloat() > Chance)) return;

            int number = rnd.RandIntRange(0, _effects.Count - 1);
            var effect = _effects[number].Clone();
            effect.Apply(new EffectApplyingContext { Caster = Owner!, Damage = obj.Context.FinalDamage, Source = InstanceId, Target = Owner! });
        }

        public override void Detach(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new GiftFromTheGoddessPassiveSkill(Chance);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not GiftFromTheGoddessPassiveSkill gift) return false;

            return gift.Chance > Chance;
        }
    }
}
