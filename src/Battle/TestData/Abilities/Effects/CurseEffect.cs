namespace Battle.TestData.Abilities.Effects
{
    using Core.Interfaces.Abilities;

    public class CurseEffect : Effect
    {
        public override IEffect Clone() => new CurseEffect
        {
            Id = Id, Duration = Duration, Stacks = Stacks, StatusEffect = StatusEffect
        };
    }
}
