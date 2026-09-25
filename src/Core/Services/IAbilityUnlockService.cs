namespace Core.Services
{
    public interface IAbilityUnlockService
    {
        void Dispose();

        /// <summary>Learns every ability whose threshold the player has reached but hasn't learned yet.</summary>
        void Reconcile(bool notify);
    }
}
