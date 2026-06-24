namespace Core.Interfaces.Battle
{
    using System.Threading.Tasks;
    using Enums;

    public interface IBattleContext
    {
        Task<BattleResults> RunBattleAsync();
        void Dispose();
    }
}
