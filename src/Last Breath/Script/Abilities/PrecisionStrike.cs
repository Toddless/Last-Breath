namespace Playground.Script.Abilities
{
    using Playground.Script.Abilities.Effects;

    public class PrecisionStrike(ICharacter owner)
        : AbilityBase(owner,
            cooldown: 3,
            cost: 2,
            type: Enums.ResourceType.Combopoints,
            activateOnlyOnCaster: false)
    {
        protected override AbilityEffectConfig ConfigureEffects() => new()
        {
            SelfTarget = [new PrecisionEffect()],
            TargetEffects = [new ClumsinessEffect()]
        };
    }
}
