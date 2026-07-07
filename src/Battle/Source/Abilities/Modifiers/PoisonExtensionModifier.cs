namespace Battle.Source.Abilities.Modifiers
{
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Enums;

    public class PoisonExtensionModifier(int additionalDuration) : IPostAttackModifier
    {
        public Task Apply(IAttackContext context)
        {
            if (context.Result is not AttackResults.Succeed) return Task.CompletedTask;

            foreach (IEffect effect in context.Target.Effects.GetBy(effect => effect.Status is StatusEffects.Poison))
                effect.Duration += additionalDuration;
            return Task.CompletedTask;
        }
    }
}
