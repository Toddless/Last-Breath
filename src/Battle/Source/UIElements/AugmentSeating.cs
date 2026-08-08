namespace Battle.Source.UIElements
{
    using System;
    using Abilities;
    using Core;
    using Core.Battle.Abilities;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
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
    public sealed class AugmentSeating(IGameMessageBus? bus, IAugmentInstallGate? gate, Action<string> showReason)
        : IAugmentCellHost
    {
        /// <summary>Localisation keys of the extraction refusals, by the answer's own name.</summary>
        private const string ExtractRefusalPrefix = "UI_Augment_Extract_";

        /// <summary>A composition with no gate holds no slots at all, so every address is honestly one it
        /// does not have.</summary>
        public AugmentInstallResult Judge(string socketAddress, string itemInstanceId) =>
            gate?.Judge(socketAddress, itemInstanceId) ?? new AugmentInstallResult(AugmentInstallOutcome.NoSuchSocket);

        public void Install(string socketAddress, string itemInstanceId) => SendInstall(socketAddress, itemInstanceId);

        public void Extract(string socketAddress) => SendExtract(socketAddress);

        public void ShowReason(string text) => showReason(text);

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

                if (result != AugmentExtractResult.Extracted) Announce($"{ExtractRefusalPrefix}{result}");
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
