namespace Core.Session
{
    /// <summary>
    /// A service holding per-session state (facts, quests, inventory, reputation, masteries...).
    /// ResetSession returns it to the fresh-process state: singletons outlive scene changes, so a
    /// second "New game" in one process would otherwise inherit the previous playthrough.
    /// The reset is SILENT — no gameplay notifications; it runs from the main menu, outside any
    /// world scene, and the fresh scene reads the state in its own _Ready.
    /// </summary>
    public interface ISessionResettable
    {
        void ResetSession();
    }
}
