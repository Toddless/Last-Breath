namespace Crafting.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity.Components;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Results;

    /// <summary>
    /// The drain the other four commands do not give: three augments of one class go in and one of the
    /// same class comes out, drawn from the records the three did not name and rolled from scratch.
    /// Nothing carries over — not the numbers, not the record — so handing a good copy in is a loss,
    /// which is what keeps this a place to put augments of no use rather than a second road to a
    /// strong one.
    /// <para>
    /// The gate is here, as with the other four: the window of a later step only mirrors what this
    /// answers. Order is the whole of the safety — every reason to refuse is worked out first, then
    /// the result is drawn and the BAG TAKES IT, and only once it has do the three copies leave. An
    /// augment's numbers are drawn once and nothing in the game draws them again, so three of them
    /// must never be spent on a result that cannot land; a refusal anywhere along the line leaves the
    /// bag untouched and throws away only the draw.
    /// </para>
    /// </summary>
    /// <param name="catalog">What the ids mean. The tier and the rarity a conversion measures are
    /// written in the record and nowhere else, which is also what makes the result answerable for a
    /// record nobody owns a copy of.</param>
    /// <param name="minter">The one door a copy is born through, so the result is rolled by the same
    /// rules as an augment found in the world.</param>
    public class ConvertAugmentsRequestHandler(
        IInventory inventory,
        IAbilityAugmentCatalog catalog,
        IAugmentItemMinter minter,
        IRandomNumberGenerator rnd)
        : IRequestHandler<ConvertAugmentsRequest, AugmentConversionResult>
    {
        /// <summary>How many copies buy one.</summary>
        private const int OfferedCount = 3;

        public Task<AugmentConversionResult> HandleRequest(ConvertAugmentsRequest request)
        {
            if (!IsAHandful(request.ItemInstanceIds)) return Refused(AugmentConversionOutcome.NotThreeAugments);

            List<IAugmentItem> held = [.. request.ItemInstanceIds.Select(inventory.GetItem<IAugmentItem>).OfType<IAugmentItem>()];
            if (held.Count != OfferedCount) return Refused(AugmentConversionOutcome.AugmentNotHeld);

            List<AbilityUpgradeData> declared = [.. held.Select(item => catalog.Find(item.Id)).OfType<AbilityUpgradeData>()];
            if (declared.Count != OfferedCount) return Refused(AugmentConversionOutcome.UndeclaredAugment);

            if (declared.Any(record => record.Tier != declared[0].Tier))
                return Refused(AugmentConversionOutcome.MixedTiers);

            if (declared.Any(record => record.Rarity != declared[0].Rarity))
                return Refused(AugmentConversionOutcome.MixedRarities);

            if (Drawn(declared) is not { } produced) return Refused(AugmentConversionOutcome.NothingToGiveBack);
            if (!inventory.TryAddItem(produced)) return Refused(AugmentConversionOutcome.NoBagRoom);

            foreach (IAugmentItem spent in held) inventory.RemoveItemByInstanceId(spent.InstanceId);
            return Task.FromResult(new AugmentConversionResult(AugmentConversionOutcome.Converted, produced.InstanceId));
        }

        /// <summary>Three ids, naming three different copies. A repeated id is one copy paying twice
        /// over, and a longer list is a request that cannot be read at all — the conversion eats
        /// exactly what it is handed, so it must be handed exactly what it eats.</summary>
        private static bool IsAHandful(IReadOnlyList<string> offered) =>
            offered.Count == OfferedCount && offered.Distinct(StringComparer.Ordinal).Count() == OfferedCount;

        private static Task<AugmentConversionResult> Refused(AugmentConversionOutcome outcome) =>
            Task.FromResult(new AugmentConversionResult(outcome));

        /// <summary>A fresh copy of one record the conversion may give back, taken evenly among them.
        /// Null when there is no such record — the class the three belong to holds nothing else, and a
        /// conversion with nothing to draw must not be started.</summary>
        private IAugmentItem? Drawn(IReadOnlyList<AbilityUpgradeData> offered)
        {
            List<AbilityUpgradeData> candidates = [.. Alternatives(offered)];
            return candidates.Count == 0 ? null : minter.Mint(candidates[rnd.RandIntRange(0, candidates.Count - 1)].Id);
        }

        /// <summary>Every record of the offered class except the ones offered: the same tier and the
        /// same rarity, so the trade is even, minus the three handed over, so the player is never sold
        /// back what he just gave up.</summary>
        private IEnumerable<AbilityUpgradeData> Alternatives(IReadOnlyList<AbilityUpgradeData> offered)
        {
            HashSet<string> given = [.. offered.Select(record => record.Id)];
            return catalog.All.Where(record =>
                record.Tier == offered[0].Tier
                && record.Rarity == offered[0].Rarity
                && !given.Contains(record.Id));
        }
    }
}
