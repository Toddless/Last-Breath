namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abilities;
    using Core;
    using Core.Battle.Abilities;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The road from a cell — or from a pip on the wheel — to the install gate and back. One courier for
    /// every screen that seats augments, so a second screen is a second instance and never a second
    /// reading of the order.
    ///
    /// <para>The one place it touches a service directly instead of the bus is the drag check: the engine
    /// asks whether a drop is allowed synchronously and the request bus answers asynchronously, so the
    /// two cannot be joined. That is a READ — every operation still goes through the bus — and it reads
    /// the very gate the operation will go through, which is the only arrangement in which the cursor
    /// cannot promise something the bus then refuses.</para>
    ///
    /// <para>A refusal is said twice on purpose: on the line the owner gave it, where the player is
    /// looking, and as a toast, because the screen may have been closed by the time the bus answers.</para>
    /// </summary>
    /// <param name="showReason">Where a refusal is printed. The owner's own line — the panel has one
    /// under its rows, the wheel has one under its canvas — so the same courier serves both without
    /// knowing what either looks like.</param>
    /// <param name="windows">Where the picker of an empty slot is opened. Optional: a composition
    /// without one seats augments by drag alone, which is what the sandbox does.</param>
    public sealed class AugmentSeating(
        IGameMessageBus? bus,
        IAugmentInstallGate? gate,
        Action<string> showReason,
        IUiElementsManager? windows = null)
        : IAugmentCellHost
    {
        /// <summary>The list currently up, so whoever opened this courier can take it down with itself.
        /// The picker lives in the Overlay layer and outlives the window that asked for it — a screen
        /// closed by its cross would otherwise leave a list hanging over the world, still able to seat
        /// an augment into a slot nobody is looking at.</summary>
        private IPickerPopup? _picker;

        /// <summary>A composition with no gate holds no slots at all, so every address is honestly one it
        /// does not have.</summary>
        public AugmentInstallResult Judge(string socketAddress, string itemInstanceId) =>
            gate?.Judge(socketAddress, itemInstanceId) ?? new AugmentInstallResult(AugmentInstallOutcome.NoSuchSocket);

        public void Pick(string socketAddress) => Offer(socketAddress);

        /// <summary>Takes down the list this courier put up, if it is still there. Called by the owner
        /// when it dies; a picker that closed itself on a pick is already gone and this costs nothing.</summary>
        public void ClosePicker()
        {
            if (_picker is Node node && GodotObject.IsInstanceValid(node)) _picker.Close();
            _picker = null;
        }

        public void Install(string socketAddress, string itemInstanceId) => SendInstall(socketAddress, itemInstanceId);

        public void Extract(string socketAddress) => SendExtract(socketAddress);

        public void ShowReason(string text) => showReason(text);

        /// <summary>The rows as the picker draws them: the copy's own name in its rarity's colour, and
        /// its tier and what it does under the pointer — the same card the tray shows and the same one
        /// the slot will show once the copy is in it. The list arrives in the order the gate offered it
        /// and is not touched — a window that re-sorted would be answering a question the domain has
        /// already answered.</summary>
        private static IReadOnlyList<PickerEntry> Rows(IReadOnlyList<AugmentTrayTileView> candidates) =>
        [
            .. candidates.Select(candidate => Row(candidate, AugmentText.Card(candidate)))
        ];

        private static PickerEntry Row(AugmentTrayTileView candidate, AugmentCard card) =>
            new(candidate.InstanceId, card.Name, candidate.Icon, card.Body, card.RarityColor);

        /// <summary>
        /// Shows what the bag holds for that slot and seats whatever is chosen. WHICH copies those are
        /// is the gate's answer, asked for through the bus like every other read: the picker renders a
        /// list, it does not assemble one.
        /// <para>
        /// An empty answer is said on the line instead of opened as an empty popup — the player asked a
        /// question and gets it answered where he is looking. The pick itself goes back through the
        /// install request, so a bag emptied by another window between the offer and the click is
        /// refused by the gate exactly as a stale drag is.
        /// </para>
        /// </summary>
        private async void Offer(string socketAddress)
        {
            try
            {
                if (bus == null || windows == null) return;

                var candidates = await bus.SendRequest<GetAugmentCandidatesRequest, IReadOnlyList<AugmentTrayTileView>>(
                    new GetAugmentCandidatesRequest(socketAddress));

                if (candidates.Count == 0)
                {
                    ShowReason(Localization.Localize(AugmentText.NothingFits));
                    return;
                }

                if (windows.ShowPopup(typeof(IPickerPopup)) is not IPickerPopup picker) return;

                ShowReason(string.Empty);
                _picker = picker;
                picker.Present(
                    Localization.Localize(AugmentText.PickTitle),
                    Rows(candidates),
                    instanceId => Install(socketAddress, instanceId));
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to offer the augments a slot would take", exception);
                GD.Print($"Failed to offer the augments a slot would take: {exception.Message}");
            }
        }

        /// <summary>The bus is the authority: the preview under the cursor was advisory, and between it
        /// and the drop the bag could be emptied or a node refunded by another window. A refusal is
        /// reported from the ANSWER; a success needs no redraw of its own, because the board says so.</summary>
        private async void SendInstall(string socketAddress, string itemInstanceId)
        {
            try
            {
                if (bus == null) return;

                var result = await bus.SendRequest<InstallAugmentRequest, AugmentInstallResult>(
                    new InstallAugmentRequest(socketAddress, itemInstanceId));

                if (!result.Installed) Announce(AugmentRefusalText.KeyFor(result));
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to install an augment", exception);
                GD.Print($"Failed to install an augment: {exception.Message}");
            }
        }

        private async void SendExtract(string socketAddress)
        {
            try
            {
                if (bus == null) return;

                var result = await bus.SendRequest<ExtractAugmentRequest, AugmentExtractResult>(
                    new ExtractAugmentRequest(socketAddress));

                if (result != AugmentExtractResult.Extracted) Announce(AugmentRefusalText.KeyFor(result));
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to extract an augment", exception);
                GD.Print($"Failed to extract an augment: {exception.Message}");
            }
        }

        private void Announce(string localizationKey)
        {
            ShowReason(Localization.Localize(localizationKey));
            _ = bus?.PublishMessageAsync(new SendNotificationMessageMessage(localizationKey));
        }
    }
}
