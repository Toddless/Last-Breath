namespace Battle.Source.Abilities.Effects
{
    using Godot;
    using Utilities;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class RegenerationEffect(
        float amount,
        int duration,
        int maxStacks,
        bool isPercent = false,
        StatusEffects statusEffect = StatusEffects.Regeneration)
        : Effect(id: "Effect_Regeneration", duration, maxStacks, statusEffect)
    {
        public bool IsPercent => isPercent;
        public float Amount { get; } = amount;

        public override void TurnEnd()
        {
            Target?.Heal(!isPercent ? new HealContext(Target, Target) { Amount = Amount } : new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * Amount });
            base.TurnEnd();
        }

        protected override string FormatDescription()
        {
            float totalRegeneration = Target?.Effects.GetBy(effect => effect.Id == Id).Cast<RegenerationEffect>().Sum(effect => effect.Amount) ?? Amount;

            return Localization.LocalizeDescriptionFormated(Id, Mathf.RoundToInt(totalRegeneration));
        }

        public override IEffect Copy() => new RegenerationEffect(Amount, Duration, MaxStacks, IsPercent, Status);
    }
}
