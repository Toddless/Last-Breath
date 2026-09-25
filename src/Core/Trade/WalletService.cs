namespace Core.Trade
{
    using System;
    using Session;

    public class WalletService(ITradeConfigProvider configProvider) : IWalletService, ISessionResettable
    {
        public int Gold { get; private set; } = configProvider.Config.StartingGold;

        public event Action<int>? GoldChanged;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Gold) return false;
            if (amount == 0) return true;
            Gold -= amount;
            GoldChanged?.Invoke(Gold);
            return true;
        }

        public void RestoreState(int gold)
        {
            Gold = Math.Max(0, gold);
            GoldChanged?.Invoke(Gold);
        }

        public void ResetSession() => Gold = configProvider.Config.StartingGold; // silent by contract

    }
}
