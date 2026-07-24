namespace Core.Trade
{
    using System;

    /// <summary>
    /// The player's gold as a wallet COUNTER (owner decision 2026-07-24) — not a bag item: it takes
    /// no slot, saves as one number and is spendable from any window (trade, future services).
    /// </summary>
    public interface IWalletService
    {
        int Gold { get; }

        /// <summary>Fires with the new balance on every change (UI binds here).</summary>
        event Action<int>? GoldChanged;

        void Add(int amount);

        /// <summary>All-or-nothing: false leaves the balance untouched.</summary>
        bool TrySpend(int amount);

        /// <summary>Save-load plumbing only; gameplay changes go through Add/TrySpend.</summary>
        void RestoreState(int gold);
    }
}
