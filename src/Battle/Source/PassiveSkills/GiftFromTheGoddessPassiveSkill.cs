namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Events;
    using Effects;

    public class GiftFromTheGoddessPassiveSkill(float chance)
        : Skill(id: "Passive_Skill_Gift_From_The_Goddess")
    {
        private readonly List<IEffect> _effects =
        [
            new RegenerationEffect(150, 3, 5),
            new ExecutionEffect(3, 1, 0.30f),
            new LuckyCritChanceEffect(3, 1)
        ];

        public float Chance { get; } = chance;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            var rnd = evnt.Context.Rnd;
            if (!ChanceRoll.Roll(Chance, rnd)) return;

            int number = rnd.RandiRange(0, _effects.Count - 1);
            var effect = _effects[number].Copy();
            effect.Apply(new EffectApplyingContext { Caster = Owner!, Damage = evnt.Context.FinalDamage, Source = InstanceId, Target = Owner! });
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new GiftFromTheGoddessPassiveSkill(Chance);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not GiftFromTheGoddessPassiveSkill gift) return false;

            return Chance > gift.Chance;
        }
    }
}
