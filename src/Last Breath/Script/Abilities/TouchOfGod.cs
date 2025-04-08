namespace Playground.Script.Abilities
{
    using Playground.Script.Abilities.Effects;

    public class TouchOfGod(ICharacter owner)
        : AbilityBase(owner,
            cooldown: 6,
            cost: 5)
    {
        protected override AbilityEffectConfig ConfigureEffects() => new()
        {
            SelfTarget = [new RegenerationEffect(default, default, true), new GoliathEffect(duration: default, stacks: 5)]
        };
    }
}
