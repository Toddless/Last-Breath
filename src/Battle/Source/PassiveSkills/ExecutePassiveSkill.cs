namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Godot;

    public class ExecutePassiveSkill(float threshold = 0.3f)
        : Skill(id: "Passive_Skill_Execute")
    {
        private const float MinPossibleThreshold = 0.05f;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?> { [nameof(Threshold)] = Threshold };
                return field;
            }
        }

        public float Threshold { get; } = threshold;

        public override void Attach(IFightable owner)
        {
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evnt)
        {
            if (evnt.Context.Result is not AttackResults.Succeed) return;
            var context = evnt.Context;
            if (CanExecute(context.Target)) context.Target.Kill();
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override ISkill Copy() => new ExecutePassiveSkill(Threshold);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not ExecutePassiveSkill execute) return false;
            return Threshold > execute.Threshold;
        }

        private bool CanExecute(IFightable fightable)
        {
            if (fightable is IFightableNpc npc)
                return npc.CurrentHealth / npc.Parameters.MaxHealth <= GetThreshold(npc.EntityType);
            return fightable.CurrentHealth / fightable.Parameters.MaxHealth <= Threshold;
        }

        private float GetThreshold(EntityType type) => type switch
        {
            EntityType.Regular => Threshold,
            EntityType.Special => Mathf.Max(MinPossibleThreshold, Threshold * 0.9f),
            EntityType.Elit => Mathf.Max(MinPossibleThreshold, Threshold * 0.7f),
            EntityType.Unique => Mathf.Max(MinPossibleThreshold, Threshold * 0.5f),
            EntityType.Boss => Mathf.Max(MinPossibleThreshold, Threshold * 0.35f),
            EntityType.Archon => Mathf.Max(MinPossibleThreshold, threshold * 0.15f),
            _ => Threshold,
        };
    }
}
