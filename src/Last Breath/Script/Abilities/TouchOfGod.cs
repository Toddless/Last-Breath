namespace Playground.Script.Abilities
{
    using Playground.Script.Abilities.Effects;

    public class TouchOfGod(ICharacter owner)
        : AbilityBase(owner,
            cooldown: 6,
            cost: 5,
            type: Enums.ResourceType.Fury,
            activateOnlyOnCaster: true)
    {
        protected override AbilityEffectConfig ConfigureEffects() => new()
        {
            SelfTarget = [new RegenerationEffect(), new GoliathEffect(stacks: 5)]
        };
    }
}
