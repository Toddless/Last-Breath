namespace Core.Modifiers.Context
{
    using System;
    using System.Linq;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Reduces only the damage-over-turn components (bleed, poison, burning) of hits the owner TAKES.
    /// Separate from the by-cause reduction on purpose: a DoT tick and a one-shot effect hit are different
    /// things to the player even though both arrive as <see cref="DamageCause.Effect"/>.</summary>
    public class DotDamageTakenReductionContextModifier(IFightable owner, Func<float> reduce)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Dot_Damage_Taken_Reduction"), IDamageModifier
    {
        private const DamageType DotTypes = DamageType.Bleed | DamageType.Poison | DamageType.Burning;

        public void Apply(IDamageContext context)
        {
            if (context.Source.IsSame(owner.InstanceId)) return;

            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                if ((type & DotTypes) != 0)
                    context.Set(type, damage * (1 - reduce()));
        }
    }
}
