namespace Battle.Source.PassiveSkills
{
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>"Сброс оков": the first stun received in a battle is dispelled immediately.
    /// The charge is refreshed by the battle-end republish on the owner's combat bus.</summary>
    public class UnshackledPassiveSkill() : Skill(id: "Passive_Skill_Unshackled")
    {
        private const string StunEffectId = "Effect_Stun";

        private bool _used;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _used = false;
            owner.CombatEvents.Subscribe<StatusEffectAppliedEvent>(OnStatusApplied);
            owner.CombatEvents.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnStatusApplied(StatusEffectAppliedEvent evt)
        {
            if (_used || Owner == null) return;
            if ((evt.StatusEffect & StatusEffects.Stun) == 0) return;

            _used = true;
            // Removing the carriers lifts the status too (RemoveStatusIfLastCarrier)
            foreach (IEffect stun in Owner.Effects.GetBy(effect => effect.IsSame(StunEffectId)).ToList())
                stun.Remove();
        }

        private void OnBattleEnd(BattleEndEvent evt) => _used = false;

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<StatusEffectAppliedEvent>(OnStatusApplied);
            owner.CombatEvents.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            Owner = null;
        }

        public override ISkill Copy() => new UnshackledPassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}
