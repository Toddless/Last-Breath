namespace Core.Session
{
    /// <summary>Registry of session-stateful services; "New game" resets them all before the world scene loads.</summary>
    public interface ISessionResetService
    {
        void Register(ISessionResettable participant);

        /// <summary>Resets every participant to the fresh-process state (in registration order,
        /// inside a load scope so stray gameplay notifications stay muted).</summary>
        void ResetSession();
    }
}
