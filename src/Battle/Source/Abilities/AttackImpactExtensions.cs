namespace Battle.Source.Abilities
{
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;

    public static class AttackImpactExtensions
    {
        /// <summary>Snapshot of a resolved attack as one ability impact for the impact riders. The
        /// attack pipeline is the one road that carries <see cref="ImpactKind.Attack"/>, and the ability
        /// whose series the swing belongs to comes along as the impact's source.
        /// <para>A swing thrown back at whoever swung last is an answer and reports as
        /// <see cref="ImpactKind.Reaction"/> instead, so a record bought for a series of attacks counts
        /// the attacks that series made and not the ones somebody else's aggression provoked.</para></summary>
        public static AbilityImpact ToImpact(this IAttackContext context, IBattleField field, IAbility source) =>
            new(context.Attacker, context.Target, field,
                context.Result is AttackResults.Succeed, context.IsCritical, context.FinalDamage)
            {
                Source = source,
                Kind = context.IsAnswer ? ImpactKind.Reaction : ImpactKind.Attack
            };
    }
}
