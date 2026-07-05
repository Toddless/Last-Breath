namespace Core.Interfaces.Abilities
{
    /// <summary>
    /// Ability whose activation is charged by holding the button: the player picks a stage (1..MaxStage),
    /// the UI writes it to <see cref="PendingStage"/> before the commit, Execute consumes and resets it.
    /// </summary>
    public interface IChargedAbility
    {
        int MaxStage { get; }

        /// <summary>Highest stage currently affordable (resource validation) — the UI stops charging here.</summary>
        int MaxAffordableStage { get; }

        /// <summary>Stage picked in the UI; consumed by the next Execute.</summary>
        int PendingStage { get; set; }
    }
}
