namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using Battle.Source.Abilities.Targeting;
    using Core.Battle.Abilities;
    using Core.Localization;

    /// <summary>
    /// Whom a cast lands on, in words — the TARGET line of the ability card. Lives in the battle module
    /// because it reads the concrete targeting strategies, which the core interface deliberately does
    /// not describe: the interface says how many and whether the player clicks, and neither says whose
    /// side. A strategy the wording does not know reads as nothing at all, and the card then hides the
    /// line rather than guessing.
    /// </summary>
    public static class AbilityTargetText
    {
        public const string SelfKey = "UI_AbilityTargetSelf";
        public const string EnemyKey = "UI_AbilityTargetEnemy";
        public const string AllyKey = "UI_AbilityTargetAlly";
        public const string EnemiesKey = "UI_AbilityTargetEnemies";
        public const string AlliesKey = "UI_AbilityTargetAllies";
        public const string RandomEnemyKey = "UI_AbilityTargetRandomEnemy";
        public const string RandomAllyKey = "UI_AbilityTargetRandomAlly";
        public const string RandomEnemiesKey = "UI_AbilityTargetRandomEnemies";
        public const string RandomAlliesKey = "UI_AbilityTargetRandomAllies";

        /// <summary>The placeholder the counted templates put their number in.</summary>
        public const string ValuePlaceholder = "Value";

        /// <summary>The line, or nothing for a strategy the wording does not know (an authored NPC
        /// trick, a stub in a walk) — absence hides the line instead of mislabeling it.</summary>
        public static string LineOf(ITargetingStrategy? targeting) => targeting switch
        {
            SelfTargeting => Localization.Localize(SelfKey),
            SingleTargetTargeting single => One(single.Relation),
            // A "few" of one is a single pick: the counted template would say "up to 1", which is a
            // sentence about a rule the cast does not have.
            FewTargetsTargeting { MaxTargets: <= 1 } few => One(few.Relation),
            FewTargetsTargeting few => Counted(few.Relation, few.MaxTargets, EnemiesKey, AlliesKey),
            RandomTargetsTargeting { MaxTargets: <= 1 } random =>
                Localization.Localize(random.Relation == TargetRelation.Enemies ? RandomEnemyKey : RandomAllyKey),
            RandomTargetsTargeting random => Counted(random.Relation, random.MaxTargets, RandomEnemiesKey, RandomAlliesKey),
            _ => string.Empty,
        };

        private static string One(TargetRelation relation) =>
            Localization.Localize(relation == TargetRelation.Enemies ? EnemyKey : AllyKey);

        private static string Counted(TargetRelation relation, int count, string enemiesKey, string alliesKey) =>
            Localization.Render(relation == TargetRelation.Enemies ? enemiesKey : alliesKey,
                new Dictionary<string, object?> { [ValuePlaceholder] = count });
    }
}
