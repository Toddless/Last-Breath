namespace Core.Interfaces.Battle
{
    using System.Threading.Tasks;

    public interface IBattleContext
    {
        Task RunBattleAsync();
        void Dispose();
    }
}
