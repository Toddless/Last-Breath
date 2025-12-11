namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class ConfusionEffect(string id, int duration)
        : Effect(id, duration, stacks: 1, statusEffect: StatusEffects.Confused)
    {
        public override IEffect Clone() => throw new System.NotImplementedException();
    }
}
