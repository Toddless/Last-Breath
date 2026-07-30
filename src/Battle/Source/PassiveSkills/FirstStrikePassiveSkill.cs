namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Events;

    /// <summary>"First Strike": the first attack after each of the owner's turn starts deals
    /// <c>bonus</c> (0.45 = +45%) more damage.</summary>
    public class FirstStrikePassiveSkill(float bonus) : Skill(id: "Passive_Skill_First_Strike")
    {
        private bool _ready = true;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(Bonus)] = Bonus
                };
                return field;
            }
        }

        public float Bonus { get; } = bonus;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _ready = true;
            owner.CombatEvents.Subscribe<TurnStartEvent>(OnTurnStart);
            owner.CombatEvents.Subscribe<BeforeAttackEvent>(OnBeforeAttack);
        }

        private void OnTurnStart(TurnStartEvent evt) => _ready = true;

        private void OnBeforeAttack(BeforeAttackEvent evt)
        {
            if (!_ready) return;
            _ready = false;
            evt.Context.ScaleDamage(1 + Bonus);
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<TurnStartEvent>(OnTurnStart);
            owner.CombatEvents.Unsubscribe<BeforeAttackEvent>(OnBeforeAttack);
            Owner = null;
        }

        public override ISkill Copy() => new FirstStrikePassiveSkill(Bonus);

        public override bool IsStronger(ISkill skill) =>
            skill is FirstStrikePassiveSkill first && Bonus > first.Bonus;
    }
}
