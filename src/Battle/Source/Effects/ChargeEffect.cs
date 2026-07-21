namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Entity;

    /// <summary>
    /// "Charge" (Static Armor): a stacking mark. When the target reaches <c>detonationStacks</c> stacks,
    /// ALL stacks are consumed and <c>onDetonate(target)</c> fires — the detonation payload (damage,
    /// barrier restore, splash) lives with whoever applied the mark, the effect only counts and pops.
    /// </summary>
    public class ChargeEffect(int duration, int maxStacks, int detonationStacks, Action<IFightable>? onDetonate)
        : Effect(id: "Effect_Charge", duration, maxStacks)
    {
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(DetonationStacks)] = DetonationStacks;
                return values;
            }
        }

        public int DetonationStacks => detonationStacks;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not detonate anything

            var stacks = Target.Effects.GetBy(effect => effect.IsSame(Id)).ToList();
            if (stacks.Count < detonationStacks) return;

            IFightable target = Target;
            foreach (IEffect stack in stacks) stack.Remove();
            onDetonate?.Invoke(target);
        }

        public override IEffect Copy() => new ChargeEffect(Duration, MaxStacks, detonationStacks, onDetonate);
    }
}
