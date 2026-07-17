namespace Battle.Source.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>
    /// Total damage immunity: while applied, every damage component of every incoming hit is zeroed
    /// (Absolute priority — the target-side pipeline runs it after all other mutators). The boss
    /// stage transition uses it as the "finish your turn" signal; effectively permanent — the
    /// transformation strips it together with all other effects.
    /// </summary>
    public class DamageImmunityEffect(int duration = 999)
        : Effect(id: "Effect_Damage_Immunity", duration, maxStacks: 1)
    {
        private readonly DamageImmunityModifier _modifier = new();

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            Target.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            Target?.ModifierHandler.Remove(_modifier); // before base nulls the target
            base.Remove();
        }

        public override IEffect Copy() => new DamageImmunityEffect(Duration);

        private sealed class DamageImmunityModifier()
            : ContextModifier(ContextModifierPriority.Absolute, id: "Context_Modifier_Damage_Immunity"), IDamageModifier
        {
            public void Apply(IDamageContext context)
            {
                foreach (DamageType type in context.DamageComponents.Keys.ToArray())
                    context.Set(type, 0f);
            }
        }
    }
}
