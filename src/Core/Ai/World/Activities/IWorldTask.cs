namespace Core.Ai.World.Activities
{
    /// <summary>
    /// An activity with a completion condition — the unit of a routine (<see cref="CycleActivity"/>).
    /// Completion survives interruptions like any activity progress; <see cref="Restart"/> clears it
    /// when the cycle comes back around.
    /// </summary>
    public interface IWorldTask : IWorldActivity
    {
        bool IsCompleted { get; }

        /// <summary>Clears the completion for the task's next lap in a cycle.</summary>
        void Restart();
    }
}
