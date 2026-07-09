namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>Item-grant passive (Armor of Power): every burning stack the owner applies drops
    /// a meteor on the target — flat fire damage, no extra effects (yet). Hooks the caster-side
    /// effect-application pipeline, so ANY burning source counts (weapon passives, abilities);
    /// bonus stacks skip the pipeline — one meteor per application act.</summary>
    public class MeteorPassiveSkill(float damage) : Skill(id: "Passive_Skill_Meteor")
    {
        private MeteorOnBurningModifier? _modifier;

        public float Damage { get; } = damage;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _modifier = new MeteorOnBurningModifier(owner, Damage);
            owner.ModifierHandler.Add(_modifier);
        }

        public override void Detach(IFightable owner)
        {
            if (_modifier != null) owner.ModifierHandler.Remove(_modifier);
            _modifier = null;
            Owner = null;
        }

        public override ISkill Copy() => new MeteorPassiveSkill(Damage);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not MeteorPassiveSkill other) return false;
            return Damage > other.Damage;
        }

        private sealed class MeteorOnBurningModifier(IFightable owner, float damage)
            : ContextModifier(priority: Priority.Weak, id: "Context_Modifier_Meteor_On_Burning"), IEffectApplicationModifier
        {
            public void Apply(IEffectApplicationContext context)
            {
                if ((context.Effect.Status & StatusEffects.Burning) == 0) return;
                if (!context.Target.IsAlive || context.Target.IsSame(owner.InstanceId)) return;

                var meteor = new DamageContext { Source = owner, Cause = DamageCause.Passive };
                meteor.Add(DamageType.Fire, damage);
                _ = context.Target.TakeDamage(meteor);
            }
        }
    }
}
