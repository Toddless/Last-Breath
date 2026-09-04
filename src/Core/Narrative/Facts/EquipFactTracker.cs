namespace Core.Narrative.Facts
{
    using Entity;
    using Entity.Components;
    using Enums;
    using Items;
    using Save;
    using Services;

    /// <summary>
    /// Writes what the player has ever worn into the facts registry: a flag per kind of gear and a
    /// counter of every piece put on. Never taken back — a helmet removed does not unlearn wearing
    /// one, which is what a quest asking for a first piece of gear means.
    /// </summary>
    /// <remarks>Only the player is remembered, and structurally so: the tracker follows the accessor's
    /// paperdoll and no other, so an NPC that grew equipment could not write a fact by accident.</remarks>
    public class EquipFactTracker
    {
        private readonly IWorldFactsService _facts;
        private readonly ILoadScope _loadScope;

        private IEquipmentComponent? _equipment;

        public EquipFactTracker(IPlayerAccessor playerAccessor, IWorldFactsService facts, ILoadScope loadScope)
        {
            _facts = facts;
            _loadScope = loadScope;
            playerAccessor.PlayerChanged += Follow;
            if (playerAccessor.Player is { } player) Follow(player);
        }

        /// <summary>The player is a scene node registering itself, so the tracker may be built on either
        /// side of his arrival; both roads lead here, and a replaced player takes the ear with him.</summary>
        private void Follow(IPlayer player)
        {
            _equipment?.EquipmentChanged -= OnEquipmentChanged;
            _equipment = player.Equipment;
            _equipment?.EquipmentChanged += OnEquipmentChanged;
        }

        private void OnEquipmentChanged(EquipmentPiece slot, IEquipItem? item)
        {
            if (_loadScope.IsLoading) return; // a restored paperdoll re-equips everything the file held
            if (item == null) return; // taking a piece off is not a thing the world stops remembering

            _facts.SetFact(FactKeys.ItemEquipped(item.EquipmentPiece));
            _facts.Add(FactKeys.ItemEquippedAny());
        }
    }
}
