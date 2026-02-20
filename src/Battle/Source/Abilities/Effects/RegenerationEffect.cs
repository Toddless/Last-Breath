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
        StatusEffects statusEffect = StatusEffects.Regeneration)
        : Effect(id: "Effect_Regeneration", duration, maxStacks, statusEffect)
    {
        public float Amount { get; } = amount;

        public override void TurnEnd()
        {
            Owner?.Heal(Amount);
            base.TurnEnd();
        }

        protected override string FormatDescription()
        {
            float totalRegeneration = Owner?.Effects.GetBy(effect => effect.Id == Id).Cast<RegenerationEffect>().Sum(effect => effect.Amount) ?? Amount;

            return Localization.LocalizeDescriptionFormated(Id, Mathf.RoundToInt(totalRegeneration));
        }

        public override IEffect Clone() => new RegenerationEffect(Amount, Duration, MaxMaxStacks, Status);
    }
}
