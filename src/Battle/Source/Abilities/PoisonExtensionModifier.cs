namespace Battle.Source.Abilities
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    public class PoisonExtensionModifier(int additionalDuration) : IPostAttackModifier
    {
        public Task Apply(IAttackContext context, AttackMetadata metadata)
        {
            if (context.Result is not AttackResults.Succeed) return Task.CompletedTask;

            foreach (IEffect effect in context.Target.Effects.GetBy(effect => effect.Status is StatusEffects.Poison))
                effect.Duration += additionalDuration;
            return Task.CompletedTask;
        }
    }
}
