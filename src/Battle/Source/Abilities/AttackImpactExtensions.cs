namespace Battle.Source.Abilities
{
    using Core.Battle;
    using Core.Data;
    using Core.Enums;

    public static class AttackImpactExtensions
    {
        /// <summary>Snapshot of a resolved attack as one ability impact for the impact riders.</summary>
        public static AbilityImpact ToImpact(this IAttackContext context, IBattleField field) =>
            new(context.Attacker, context.Target, field,
                context.Result is AttackResults.Succeed, context.IsCritical, context.FinalDamage);
    }
}
