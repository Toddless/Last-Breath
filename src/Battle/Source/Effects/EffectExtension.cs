namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public static class EffectExtension
    {
        public static async Task ApplyStacks(this IEffect effect, EffectApplyingContext ctx, int stacks)
        {
            for (int i = 0; i < stacks; i++) await effect.Copy().Apply(ctx);
        }
    }
}
