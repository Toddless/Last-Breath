namespace Battle.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;

    /// <summary>
    /// The one gate an augment goes through on its way into a slot; a socket window only mirrors what
    /// it answers. Order is the whole of it: the board takes the copy FIRST and the item leaves the bag
    /// only once it has — a refusal then costs nothing, and a seating never leaves the same copy lying
    /// in the bag as well, where it could be seated a second time or sold off the character's own
    /// build.
    /// <para>
    /// A refusal is named rather than counted: the fitting rule's verdict is carried back untouched,
    /// so the window tells the player why the augment stayed out instead of measuring tiers and tags
    /// on its own — two readings of one rule are two answers.
    /// </para>
    /// <para>
    /// A seating that took ends by putting the board back onto the abilities. The gate does not work
    /// out which upgrade that is — the binder reads the whole arrangement, the same way it reads it
    /// after a load or a refunded node — because a gate that dressed the ability itself would be a
    /// second opinion about what the sockets hold.
    /// </para>
    /// </summary>
    public class InstallAugmentRequestHandler(
        IAbilitySocketBoard sockets,
        IInventory inventory,
        IAbilityAugmentBinder augments)
        : IRequestHandler<InstallAugmentRequest, AugmentInstallResult>
    {
        public Task<AugmentInstallResult> HandleRequest(InstallAugmentRequest request)
        {
            if (inventory.GetItem<IAugmentItem>(request.ItemInstanceId) is not { } item)
                return Task.FromResult(new AugmentInstallResult(AugmentInstallOutcome.AugmentNotHeld));

            if (!sockets.Install(request.SocketId, item.Augment))
                return Task.FromResult(Refusal(request.SocketId, item.Augment));

            inventory.RemoveItemByInstanceId(item.InstanceId);
            augments.Bind();
            return Task.FromResult(new AugmentInstallResult(AugmentInstallOutcome.Installed));
        }

        /// <summary>Why the board would not take it. Worked out only after a seating has failed: the
        /// board answers yes or no by itself, and the reason is worth a second reading of the same
        /// rule exactly when there is a player to tell it to.</summary>
        private AugmentInstallResult Refusal(string socketId, AugmentInstance augment)
        {
            if (sockets.Find(socketId) is not { } socket)
                return new AugmentInstallResult(AugmentInstallOutcome.NoSuchSocket);

            return socket.IsEmpty
                ? new AugmentInstallResult(AugmentInstallOutcome.DoesNotFit, sockets.Judge(socketId, augment))
                : new AugmentInstallResult(AugmentInstallOutcome.SocketOccupied);
        }
    }
}
