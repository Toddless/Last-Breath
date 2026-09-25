namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abilities;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.Rules;
    using Core.PassiveTree.Summary;
    using Core.Trade;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The passive wheel: the tree, a strip of numbers and buttons above it, and two panels that open
    /// over it — the totals the allocation adds up to, and the guard that catches an unapplied plan on
    /// its way out.
    ///
    /// <para>It is the SECOND of two screens, not a bigger version of the first: the mastery window is
    /// the sheet of abilities the character already owns, this one is where they are bought and where
    /// their slots are opened. The socket sheet stays over there; here the slots are drawn as small rings
    /// around the ability that owns them, so there is nothing to read down a column and nothing to select
    /// a node for.</para>
    ///
    /// <para>This window is the only owner of the plan. The draft is CREATED here, disposed with the
    /// scene and never registered in the container — which is the whole safety of a marked node: nothing
    /// downstream of the allocation can reach it, so it gives no bonus, opens no slot, costs no point and
    /// is written to no file until a button says so. It is also the only thing that sends a respec: the
    /// canvas marks, the strip reports presses, and the order in which the tree and the purse settle
    /// lives behind the bus.</para>
    ///
    /// <para>Nothing here counts nodes, edges, slots or points: the wheel is built by walking the
    /// document, the slots come from the board, and the point total comes from mastery. The tree file is
    /// rewritten daily and none of its numbers live in this file.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveWheelWindow : Control, IWindow
    {
        private const string UID = "uid://bwh3pk2vqx7la";

        /// <summary>The engine action Esc is bound to. Caught by name rather than by key code so a
        /// rebound Esc still reaches the guard.</summary>
        private const string CancelAction = "ui_cancel";

        [Export] private PassiveWheelCanvas? _canvas;
        [Export] private PassiveWheelTopBar? _topBar;
        [Export] private PassiveSummaryPanel? _summary;

        /// <summary>One line, said synchronously where the cursor is: why the last gesture was refused,
        /// or what it did besides what was asked. The asynchronous half of a respec or an augment install
        /// is said here too, and as a toast beside it, because the window may be gone by the time the bus
        /// answers.</summary>
        [Export] private Label? _hint;

        [Export] private PassiveCloseGuard? _closeGuard;

        private IPassiveTreeService? _tree;
        private IAbilitySocketBoard? _board;
        private IGameMessageBus? _bus;
        private IWalletService? _wallet;
        private ModifierFormatter? _modifiers;
        private ContextModifierFormatter? _knobs;

        private PassiveTreeDraft? _draft;
        private PassiveRespecQuotes? _quotes;
        private AugmentSeating? _seating;

        private bool _summaryOpen;

        /// <summary>Esc must not walk away from work the player has not been asked about. The guard puts
        /// the question on screen and the manager is told the key was handled; this is the second half of
        /// the same rule, for whatever road reaches the manager without passing through the key.</summary>
        public bool IsDismissable => _draft == null || _draft.IsEmpty;

        public override void _Ready()
        {
            _topBar?.RespecToggled += OnRespecToggled;
            _topBar?.SummaryToggled += OnSummaryToggled;
            _topBar?.ApplyPressed += () => Apply(closeOnSuccess: false);
            _topBar?.CancelPressed += DropPlan;
            _topBar?.FramePressed += () => _canvas?.FrameAll();
            _topBar?.ClosePressed += RequestClose;

            _closeGuard?.Applied += () => Apply(closeOnSuccess: true);
            _closeGuard?.Discarded += DiscardAndClose;
            _closeGuard?.Stayed += () => _closeGuard.Visible = false;

            _canvas?.StatusChanged += ShowHint;
        }

        public override void _ExitTree()
        {
            _tree?.AllocationChanged -= Refresh;
            _board?.Changed -= Refresh;
            _tree?.AllocationChanged -= RefreshAbilityCards;
            _board?.Changed -= RefreshAbilityCards;
            _wallet?.GoldChanged -= OnGoldChanged;
            _draft?.Changed -= Refresh;

            // Anything the courier put on the Overlay layer belongs to this window's lifetime: the layer
            // outlives it, and a list left standing over the world still seats augments.
            _seating?.ClosePicker();
            _draft?.Dispose();
            _draft = null;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            ReportMissingParts();

            // Optional throughout: the battle sandbox composes no passive tree and no purse, and a wheel
            // that took the scene down there would be worse than one showing an empty state.
            _tree = provider.Optional<IPassiveTreeService>();
            _board = provider.Optional<IAbilitySocketBoard>();
            _bus = provider.Optional<IGameMessageBus>();
            _wallet = provider.Optional<IWalletService>();
            _modifiers = provider.Optional<ModifierFormatter>();
            _knobs = provider.Optional<ContextModifierFormatter>();

            _quotes = new PassiveRespecQuotes(
                provider.Optional<IPassiveRespecPricing>(), provider.Optional<IMartialArtMastery>(), _wallet);

            if (_tree != null)
            {
                _draft = new PassiveTreeDraft(_tree);
                _draft.Changed += Refresh;
                _tree.AllocationChanged += Refresh;
            }

            if (_board != null) _board.Changed += Refresh;
            if (_wallet != null) _wallet.GoldChanged += OnGoldChanged;

            // The two events after which an ability can DO something else: a node bought or given back, and
            // an augment seated or pulled out. The plan is deliberately not among them — a mark gives no
            // bonus, so nothing it touches changes a card.
            if (_tree != null) _tree.AllocationChanged += RefreshAbilityCards;
            if (_board != null) _board.Changed += RefreshAbilityCards;

            _summary?.UseFormatters(_modifiers, _knobs);

            _canvas?.InjectServices(provider);
            _canvas?.UseDraft(_draft);
            _canvas?.UseQuotes(_quotes);

            // The wheel seats augments through the same courier the socket sheet uses, with its own line
            // to print refusals on. A second copy of that road would be a second reading of one rule.
            _seating = new AugmentSeating(
                _bus, provider.Optional<IAugmentInstallGate>(), ShowHint, provider.Optional<IUiElementsManager>());
            _canvas?.UseSocketHost(_seating);

            RefreshAbilityCards();
            Refresh();
        }

        /// <summary>
        /// The ability cards the node popups print, fetched ahead of the cursor and handed to the canvas.
        /// The sheet's own answer is what they are built from — the wheel prints the ability the socket
        /// screen prints, down to the numbers the seated augments put in it — and it is asked for the whole
        /// catalog at once rather than per hover, because a popup opens where the pointer already is and
        /// has nowhere to wait for the bus.
        /// <para>The UNOWNED are asked for too, and that is the point of the screen: the player spends the
        /// wheel looking at nodes he has not bought, and the card is what tells him whether to.</para>
        /// </summary>
        private async void RefreshAbilityCards()
        {
            try
            {
                if (_bus == null || _canvas == null) return;

                IReadOnlyList<AbilitySocketRowView> rows =
                    await _bus.SendRequest<GetAbilitySocketRowsRequest, IReadOnlyList<AbilitySocketRowView>>(
                        new GetAbilitySocketRowsRequest(IncludeUnowned: true));

                if (!IsInstanceValid(_canvas)) return;

                Dictionary<string, AbilityCard> cards = new(StringComparer.Ordinal);
                foreach (AbilitySocketRowView row in rows) cards[row.AbilityId] = AbilityText.Card(row);

                _canvas.UseAbilityCards(cards);
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to read the ability cards of the passive wheel", exception, this);
            }
        }

        // Close = death (QueueFree), the IWindow contract: RemoveChild left a live, still-tracked node
        // parentless, so a following Esc called Close() again on it and GetParent() was null.
        public void Close() => QueueFree();

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Esc is a layer walk everywhere else in the game; here it is deliberately caught first
        /// while a plan is standing, because the manager frees the node right after
        /// <see cref="Close"/> and a veto from inside it is impossible.</summary>
        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (!@event.IsActionPressed(CancelAction) || _draft == null || _draft.IsEmpty) return;
            if (_closeGuard is not { Visible: false }) return;

            AskBeforeClosing();
            GetViewport().SetInputAsHandled();
        }

        /// <summary>A missing part would simply make the wheel refuse things without a word.</summary>
        private void ReportMissingParts()
        {
            if (_canvas == null) Tracker.TrackNotFound("Passive wheel window has no canvas", this);
            if (_topBar == null) Tracker.TrackNotFound("Passive wheel window has no top bar", this);
            if (_closeGuard == null) Tracker.TrackNotFound("Passive wheel window has no close guard", this);
        }

        /// <summary>The mode is the draft's, and switching it clears the plan — so the player is told,
        /// rather than watching a dozen marks disappear.</summary>
        private void OnRespecToggled(bool pressed)
        {
            if (_draft == null) return;

            bool hadMarks = !_draft.IsEmpty;
            _draft.Mode = pressed ? DraftMode.Refund : DraftMode.Take;
            if (hadMarks) ShowHint(Localization.Localize(PassiveWheelText.ModeCleared));
        }

        private void OnSummaryToggled(bool pressed)
        {
            _summaryOpen = pressed;
            Refresh();
        }

        private void OnGoldChanged(int gold) => Refresh();

        private void DropPlan()
        {
            _draft?.Clear();
            ShowHint(string.Empty);
        }

        /// <summary>
        /// The numbers that move with the tree, with the plan, and with the purse. One pass for all
        /// three: they are three readings of the same screen, and a cheaper road for one of them would be
        /// a second opinion about what the player is looking at.
        /// </summary>
        private void Refresh()
        {
            if (_topBar == null) return;

            bool givesBack = _draft?.Mode == DraftMode.Refund;
            int planned = PlannedCount();

            _topBar.ShowPoints(_tree?.AvailablePoints ?? 0, _tree?.SpentPoints ?? 0, _tree?.TotalPoints ?? 0,
                planned, givesBack);
            _topBar.ShowNoPoints(_tree is { TotalPoints: 0 });
            _topBar.ShowWaiting(WaitingAugments());
            _topBar.ShowCancel(_draft is { IsEmpty: false });
            _topBar.SetRespecEnabled(_tree is { SpentPoints: > 0 });
            _topBar.SetRespecPressed(givesBack);

            RefreshApply(planned, givesBack);
            RefreshSummary(planned);
        }

        /// <summary>The button says what it would do and what it would cost, and when it cannot it says
        /// why on the hint line — a dead button with nothing beside it is silence where the interface owes
        /// a sentence.</summary>
        private void RefreshApply(int planned, bool givesBack)
        {
            if (_topBar == null) return;

            if (planned == 0)
            {
                _topBar.ShowApply(Localization.Localize(PassiveWheelText.ApplyEmpty), enabled: false);
                _topBar.ShowPrice(string.Empty, affordable: true);
                return;
            }

            if (!givesBack)
            {
                bool affordablePoints = planned <= (_tree?.AvailablePoints ?? 0);
                _topBar.ShowApply(
                    Localization.Render(PassiveWheelText.ApplyTake,
                        new Dictionary<string, object?> { [PassiveWheelText.CountValue] = planned, [PassiveWheelText.PointsValue] = planned }), affordablePoints);

                _topBar.ShowPrice(string.Empty, affordable: true);
                return;
            }

            RespecQuote quote = _quotes?.Quote(planned) ?? default;
            AllocationResult verdict = _tree?.CheckRefundSet(_draft?.PendingRefunds ?? []) ?? AllocationResult.UnknownNode;
            bool priced = _quotes is { CanCharge: true };
            bool allowed = verdict == AllocationResult.Success && priced && quote.Affordable;

            _topBar.ShowApply(
                Localization.Render(PassiveWheelText.ApplyRefund,
                    new Dictionary<string, object?> { [PassiveWheelText.CountValue] = planned, [PassiveWheelText.GoldValue] = quote.Gold }), allowed);

            _topBar.ShowPrice(priced
                ? Localization.Render(PassiveWheelText.RefundPrice,
                    new Dictionary<string, object?> { [PassiveWheelText.GoldValue] = quote.Gold })
                : string.Empty, quote.Affordable);

            if (!allowed) ShowHint(RefusalOf(verdict));
        }

        /// <summary>Why the confirmation is shut. The tree's own verdict wins where the tree is what
        /// refused; anything else is the purse, and a build with no purse at all is honestly a purse that
        /// cannot cover it.</summary>
        private static string RefusalOf(AllocationResult verdict) => Localization.Localize(
            verdict == AllocationResult.Success
                ? PassiveWheelText.NotEnoughGold
                : PassiveTreeRefusalText.RefundKey(verdict));

        /// <summary>The totals, built only while the panel is open: it is a walk over every taken node and
        /// there is no reason to pay for it behind a closed panel.</summary>
        private void RefreshSummary(int planned)
        {
            if (_summary == null) return;

            _summary.Visible = _summaryOpen;
            if (!_summaryOpen || _tree == null) return;

            TreeSummary held = PassiveTreeSummary.BuildUnconditional(_tree.Tree, _tree.TakenNodes, NoBaseline.Instance);
            TreeSummary projected = _draft == null
                ? held
                : PassiveTreeSummary.BuildUnconditional(_tree.Tree, _draft.Projected, NoBaseline.Instance);

            _summary.Show(held, projected, planned);
        }

        private int PlannedCount() => _draft == null
            ? 0
            : _draft.Mode == DraftMode.Refund
                ? _draft.PendingRefunds.Count
                : _draft.PendingTakes.Count;

        private int WaitingAugments() =>
            _board?.Sockets.Count(socket => !socket.IsOpen && !socket.IsEmpty) ?? 0;

        /// <summary>The one road from a plan to the character. Buying is settled here and now; giving back
        /// spends gold as well as points and goes to the gate that settles both, which answers
        /// asynchronously — so the window may be gone by the time it does, and the answer is said as a
        /// toast beside the hint line.</summary>
        private void Apply(bool closeOnSuccess)
        {
            if (_draft == null) return;

            if (_draft.Mode == DraftMode.Refund)
            {
                SendRespec(_draft.PendingRefunds, closeOnSuccess);
                return;
            }

            IReadOnlyList<string> buying = _draft.PendingTakes;
            AllocationResult result = _draft.ApplyTakes();
            if (result != AllocationResult.Success)
            {
                if (_tree != null && buying.Count > 0)
                    ShowHint(Localization.Localize(PassiveTreeRefusalText.TakeKey(_tree.Tree, buying[0], result)));

                return;
            }

            ShowHint(string.Empty);
            if (closeOnSuccess) Close();
        }

        private async void SendRespec(IReadOnlyList<string> nodeIds, bool closeOnSuccess)
        {
            try
            {
                if (_bus == null || nodeIds.Count == 0) return;

                RespecResult result = await _bus.SendRequest<RespecPassiveNodesRequest, RespecResult>(
                    new RespecPassiveNodesRequest(nodeIds));

                string key = result.Refunded
                    ? PassiveWheelText.RespecDone
                    : result.NotEnoughGold
                        ? PassiveWheelText.NotEnoughGold
                        : PassiveTreeRefusalText.RefundKey(result.Verdict);

                _ = _bus.PublishMessageAsync(new SendNotificationMessageMessage(key));
                if (!IsInsideTree()) return;

                ShowHint(Localization.Localize(key));
                if (result.Refunded && closeOnSuccess) Close();
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to give passive nodes back", exception, this);
                GD.Print($"Failed to give passive nodes back: {exception.Message}");
            }
        }

        /// <summary>The close button and Esc take the same road, so a plan is caught whichever one the
        /// player reaches for.</summary>
        private void RequestClose()
        {
            if (_draft == null || _draft.IsEmpty)
            {
                Close();
                return;
            }

            AskBeforeClosing();
        }

        private void AskBeforeClosing()
        {
            if (_draft == null || _closeGuard == null)
            {
                Close();
                return;
            }

            bool givesBack = _draft.Mode == DraftMode.Refund;
            int planned = PlannedCount();
            RespecQuote quote = _quotes?.Quote(planned) ?? default;
            bool priced = _quotes is { CanCharge: true };
            bool canApply = givesBack
                ? priced && quote.Affordable && _tree?.CheckRefundSet(_draft.PendingRefunds) == AllocationResult.Success
                : planned <= (_tree?.AvailablePoints ?? 0);

            string message = Localization.Render(givesBack ? PassiveWheelText.CloseRefund : PassiveWheelText.CloseTake,
                new Dictionary<string, object?> { [PassiveWheelText.CountValue] = planned, [PassiveWheelText.PointsValue] = planned, [PassiveWheelText.GoldValue] = quote.Gold });

            _closeGuard.Ask(message, canApply,
                canApply ? null : Localization.Localize(PassiveWheelText.NotEnoughGold));
        }

        private void DiscardAndClose()
        {
            _draft?.Clear();
            Close();
        }

        /// <summary>Guarded against a DEAD node and not merely a missing one: every road here is an
        /// answer arriving later than the gesture that asked for it — a bus reply, or a pick made in a
        /// popup that outlives the window — and by then the wheel may have been closed and freed.</summary>
        private void ShowHint(string text)
        {
            if (_hint == null || !IsInstanceValid(_hint)) return;

            _hint.Text = text;
            _hint.Visible = text.Length > 0;
        }
    }
}
