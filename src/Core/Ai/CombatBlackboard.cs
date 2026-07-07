namespace Core.Ai
{
    using System.Collections.Generic;
    using Components;
    using Entity;

    /// <summary>
    /// Per-turn snapshot of the fight the planner reasons about. Refreshed after every cast
    /// because a cast may kill, heal or unlock anything. Reading live state is fine here —
    /// this is resolution-time logic, not presentation.
    /// </summary>
    public class CombatBlackboard(IFightable self, IBehaviorProfile profile, ICombatEnvironment environment, IRandomNumberGenerator rnd)
    {
        public IFightable Self { get; } = self;
        public IBehaviorProfile Profile { get; } = profile;
        public ICombatEnvironment Environment { get; } = environment;
        public IRandomNumberGenerator Rnd { get; } = rnd;

        public IReadOnlyList<IFightable> Enemies { get; private set; } = [];
        public IReadOnlyList<IFightable> Allies { get; private set; } = [];
        public int CastsThisTurn { get; set; }

        public bool HasLivingEnemies => Enemies.Count > 0;

        public void Refresh()
        {
            Enemies = Environment.Field.GetEnemies(Self);
            Allies = Environment.Field.GetAllies(Self);
        }

        public static float HealthRatio(IFightable fightable)
        {
            float max = fightable.Parameters.MaxHealth;
            return max <= 0 ? 0f : fightable.CurrentHealth / max;
        }
    }
}
