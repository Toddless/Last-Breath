namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Events;
    using Core.Modifiers.Context;

    public class CreatorsNaturePassiveSkill(
        float manaRecovery,
        float healthRecovery,
        float recoveryEfficiency) : Skill(id: "Passive_Skill_Creators_Nature")
    {
        private IHealModifier? _healthRecovery;
        private IManaRecoveryModifier? _manaRecovery;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(ManaRecovery)] = ManaRecovery,
                    [nameof(HealthRecovery)] = HealthRecovery,
                    [nameof(RecoveryEfficiency)] = RecoveryEfficiency
                };
                return field;
            }
        }

        public float ManaRecovery { get; } = manaRecovery;
        public float HealthRecovery { get; } = healthRecovery;
        public float RecoveryEfficiency { get; } = recoveryEfficiency;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _healthRecovery = new HealthRecoveryContextModifier(1 + RecoveryEfficiency);
            _manaRecovery = new ManaRecoveryContextModifier(1 + RecoveryEfficiency);
            Owner.ModifierHandler.Add(_manaRecovery);
            Owner.ModifierHandler.Add(_healthRecovery);
            Owner.CombatEvents.Subscribe<DamageTakenEvent>(OnDamageTaken);
        }

        private void OnDamageTaken(DamageTakenEvent obj)
        {
            if (Owner is null || obj.Target.InstanceId != Owner.InstanceId) return;
            Owner.Heal(new HealContext(Owner, Owner) { Amount = Owner.Parameters.MaxHealth * HealthRecovery });
            Owner.RestoreMana(new ManaRecoveryContext(Owner, Owner) { Amount = Owner.Parameters.MaxMana * ManaRecovery });
        }

        public override void Detach(IFightable owner)
        {
            if (Owner is null) return;
            if (_healthRecovery != null) Owner.ModifierHandler.Remove(_healthRecovery);
            if (_manaRecovery != null) Owner.ModifierHandler.Remove(_manaRecovery);
            Owner.CombatEvents.Unsubscribe<DamageTakenEvent>(OnDamageTaken);
        }

        public override ISkill Copy() => new CreatorsNaturePassiveSkill(ManaRecovery, HealthRecovery, RecoveryEfficiency);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not CreatorsNaturePassiveSkill creatorsNaturePassiveSkill) return false;
            // for now i take only one parameter
            return RecoveryEfficiency > creatorsNaturePassiveSkill.RecoveryEfficiency;
        }
    }
}
