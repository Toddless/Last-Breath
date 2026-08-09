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
        /// whose series the swing belongs to comes along as the impact's source.</summary>
        public static AbilityImpact ToImpact(this IAttackContext context, IBattleField field, IAbility source) =>
            new(context.Attacker, context.Target, field,
                context.Result is AttackResults.Succeed, context.IsCritical, context.FinalDamage)
            {
                Source = source,
                Kind = ImpactKind.Attack
            };
    }
}
