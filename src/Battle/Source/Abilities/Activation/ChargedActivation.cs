namespace Battle.Source.Abilities.Activation
{
    using System;

    /// <summary>
    /// Charge bookkeeping of a hold-to-charge ability (<see cref="Core.Battle.Abilities.IChargedAbility"/>):
    /// the UI writes <see cref="PendingStage"/> before the commit, <see cref="ConsumeStage"/> takes it
    /// once per cast, downgraded to the highest stage the owner can actually afford.
    /// </summary>
    public class ChargedActivation(int maxStage, Func<int, bool> canAffordStage)
    {
        public int MaxStage { get; } = maxStage;

        /// <summary>Stage picked in the UI; consumed by the next cast.</summary>
        public int PendingStage { get; set; }

        public int MaxAffordableStage
        {
            get
            {
                for (int stage = MaxStage; stage > 1; stage--)
                    if (canAffordStage(stage))
                        return stage;
                return 1;
            }
        }

        public int ConsumeStage()
        {
            int stage = Math.Clamp(PendingStage == 0 ? 1 : PendingStage, 1, MaxStage);
            PendingStage = 0;
            return Math.Min(stage, MaxAffordableStage);
        }
    }
}
