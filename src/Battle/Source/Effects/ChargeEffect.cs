namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Entity;

    /// <summary>
    /// "Charge" (Static Armor): a stacking mark. On the stack that fills the pile ALL stacks are consumed
    /// and <c>onDetonate(target)</c> fires — the detonation payload (damage, barrier restore, splash) lives
    /// with whoever applied the mark, the effect only counts and pops. Built from the canon alone it still
    /// counts and pops, harmlessly: a mark nobody gave a payload to is a mark and nothing more.
    /// </summary>
    public class ChargeEffect(int duration, int maxStacks, Action<IFightable>? onDetonate = null)
        : Effect(id: "Effect_Charge", duration, maxStacks)
    {
        /// <summary>A charge is a debuff: it detonates on the bearer, so a dispel reaches it off the
        /// bearer's own half of the list rather than off a target's buffs.</summary>
        public override bool IsHarmful => true;

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(DetonationStacks)] = DetonationStacks;
                return values;
            }
        }

        /// <summary>The pile detonates when it is FULL, so the threshold is the ceiling and cannot be
        /// anything else. A threshold of its own could be raised past a ceiling the canon holds down, and
        /// the charge would then count forever without ever going off.</summary>
        public int DetonationStacks => MaxStacks;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not detonate anything

            var stacks = Target.Effects.GetBy(effect => effect.IsSame(Id)).ToList();
            if (stacks.Count < DetonationStacks) return;

            IFightable target = Target;
            foreach (IEffect stack in stacks) stack.Remove();
            onDetonate?.Invoke(target);
        }

        public override IEffect Copy() => new ChargeEffect(Duration, MaxStacks, onDetonate);
    }
}
