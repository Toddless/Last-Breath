namespace Battle.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.Rules;
    using Core.Trade;

    /// <summary>
    /// The one gate of a paid respec: the tree's verdict first, then the price, then the wallet
    /// check-then-spend, then the nodes. All-or-nothing in both currencies — a half-emptied purse beside
    /// a half-undone plan is the one outcome nothing downstream could repair.
    /// <para>The order lives here and nowhere else. The tree and the wallet know nothing about each
    /// other, so something has to bring them together, and a screen that ran the order itself would be a
    /// second reading of it. What a screen needs before the click — the price and the verdict — are pure
    /// questions it can ask directly, so nothing here has to be split out to answer them.</para>
    /// <para>Wired by the project that owns a wallet. A host without one never sends this request, and
    /// the handler is built on dispatch rather than at startup.</para>
    /// </summary>
    public class RespecPassiveNodesRequestHandler(
        IPassiveTreeService tree,
        IPassiveRespecPricing pricing,
        IMartialArtMastery mastery,
        IWalletService wallet)
        : IRequestHandler<RespecPassiveNodesRequest, RespecResult>
    {
        public Task<RespecResult> HandleRequest(RespecPassiveNodesRequest request)
        {
            // An impossible respec costs nothing: the refusal is asked for before a coin moves, the same
            // way a purchase that cannot happen is never charged for.
            AllocationResult verdict = tree.CheckRefundSet(request.NodeIds);
            if (verdict != AllocationResult.Success) return Refused(verdict);

            int price = pricing.PriceOf(request.NodeIds.Count, mastery.EarnedLevel);
            if (!wallet.TrySpend(price)) return Task.FromResult(new RespecResult(verdict, NotEnoughGold: true, GoldSpent: 0));

            AllocationResult refund = tree.RefundSet(request.NodeIds);
            if (refund != AllocationResult.Success)
            {
                // The allocation moved between the verdict and the spend. The respec never happened, so
                // neither did the payment.
                wallet.Add(price);
                return Refused(refund);
            }

            return Task.FromResult(new RespecResult(refund, NotEnoughGold: false, price));
        }

        private static Task<RespecResult> Refused(AllocationResult verdict) =>
            Task.FromResult(new RespecResult(verdict, NotEnoughGold: false, GoldSpent: 0));
    }
}
