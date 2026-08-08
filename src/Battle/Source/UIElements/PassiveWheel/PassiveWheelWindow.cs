namespace Battle.Source.UIElements.PassiveWheel
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Localization;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.View;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The passive wheel: the surface on the left, and on the right what the selected node says and the
    /// augment sockets of the ability it names.
    ///
    /// It is the SECOND of two screens, not a bigger version of the first: the mastery window is the
    /// sheet of abilities the character already owns, this one is where they are bought and where their
    /// slots are opened. The socket sheet between them is the same element in both — embedded here as
    /// an instance and told to show one ability instead of a stance, which is the whole of what it
    /// needed to be reusable.
    ///
    /// Nothing here counts nodes, edges, slots or points: the wheel is built by walking the document,
    /// the slots come from the board, and the point total comes from mastery. The tree file is rewritten
    /// daily and none of its numbers live in this file.
    /// </summary>
    [GlobalClass]
    public partial class PassiveWheelWindow : Control, IWindow
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://bwh3pk2vqx7la";

        [Export] private PassiveWheelCanvas? _canvas;
        [Export] private PassiveNodeCard? _card;

        /// <summary>Holds the socket sheet and the carried augments. Hidden whole when the selected
        /// node names no ability: a socket panel under "+10 Strength" is noise, and the panel's own
        /// empty state means "no rows", not "the question is not for you".</summary>
        [Export] private Control? _abilityPane;

        [Export] private AbilitySocketPanel? _sockets;
        [Export] private AugmentTray? _tray;
        [Export] private Label? _available;
        [Export] private Label? _spent;
        [Export] private Label? _total;

        /// <summary>One line, said synchronously where the cursor is: the price of a route, or why the
        /// last gesture was refused. The asynchronous half of an augment install is said by the panel
        /// on its own line, because that is the half this window never sees the answer to.</summary>
        [Export] private Label? _hint;

        [Export] private Label? _noPoints;
        [Export] private Label? _waiting;
        [Export] private Button? _respec;
        [Export] private Button? _frame;
        [Export] private Button? _close;
        [Export] private ConfirmationDialog? _respecConfirm;

        private IPassiveTreeService? _tree;
        private IAbilitySocketBoard? _board;
        private ModifierFormatter? _modifiers;
        private ContextModifierFormatter? _knobs;
        private ILocalizationProvider? _localization;

        public override void _Ready()
        {
            if (_close != null) _close.Pressed += Close;
            if (_frame != null) _frame.Pressed += () => _canvas?.FrameAll();
            if (_respec != null) _respec.Pressed += AskRespec;
            if (_respecConfirm != null) _respecConfirm.Confirmed += Respec;

            if (_card != null)
            {
                _card.TakePressed += () => _canvas?.TakeSelected();
                _card.RefundPressed += () => _canvas?.RefundSelected();
            }

            if (_canvas == null) return;

            _canvas.SelectionChanged += OnSelectionChanged;
            _canvas.HoveredChanged += OnHoveredChanged;
            _canvas.StatusChanged += ShowHint;
        }

        public override void _ExitTree()
        {
            if (_tree != null) _tree.AllocationChanged -= RefreshAllocation;
            if (_board != null) _board.Changed -= RefreshAllocation;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            ReportMissingParts();

            // Optional: the battle sandbox composes no passive tree, and a wheel that took the scene
            // down there would be worse than one showing an empty state.
            _tree = provider.Optional<IPassiveTreeService>();
            _board = provider.Optional<IAbilitySocketBoard>();
            _modifiers = provider.Optional<ModifierFormatter>();
            _knobs = provider.Optional<ContextModifierFormatter>();
            _localization = provider.Optional<ILocalizationProvider>();

            if (_tree != null) _tree.AllocationChanged += RefreshAllocation;
            if (_board != null) _board.Changed += RefreshAllocation;

            _canvas?.InjectServices(provider);
            _sockets?.InjectServices(provider);
            _tray?.InjectServices(provider);

            // The panel is already the one road from a cell to the install gate, and it is a public
            // host. The wheel borrows it rather than writing a second request of its own.
            _canvas?.UseSocketHost(_sockets);

            OnSelectionChanged(null);
            RefreshAllocation();
        }

        // Close = death (QueueFree), the IWindow contract: RemoveChild left a live, still-tracked node
        // parentless, so a following Esc called Close() again on it and GetParent() was null.
        public void Close() => QueueFree();

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>A missing part would simply make the wheel refuse things without a word — the
        /// sockets above all, since a null panel turns every drop into a silent refusal.</summary>
        private void ReportMissingParts()
        {
            if (_canvas == null) Tracker.TrackNotFound("Passive wheel window has no canvas", this);
            if (_sockets == null) Tracker.TrackNotFound("Passive wheel window has no augment socket panel", this);
            if (_tray == null) Tracker.TrackNotFound("Passive wheel window has no augment tray", this);
        }

        /// <summary>
        /// The selected node in words, and the socket sheet of the ability it names. The panel is never
        /// told anything after this: taking or giving back a node moves the board on its own and the
        /// panel redraws itself from it, whoever moved it.
        /// </summary>
        private void OnSelectionChanged(PassiveNode? node)
        {
            ShowCard(node);

            string abilityId = node?.AbilityId ?? string.Empty;
            bool showsAbility = abilityId.Length > 0 && node != null
                                                     && NodeKindRules.SocketTier(node.Kind) != NodeKindRules.NoSocket;

            if (_abilityPane != null) _abilityPane.Visible = showsAbility;
            if (showsAbility) _sockets?.ShowAbility(abilityId);
        }

        /// <summary>Hovering prices the route to the node under the cursor. Nothing is bought and
        /// nothing is selected — the wheel answers "what would reaching this cost me" before the
        /// click.</summary>
        private void OnHoveredChanged(PassiveNode? node)
        {
            if (_hint == null || _tree == null) return;
            if (node == null || _tree.IsTaken(node.Id))
            {
                ShowHint(string.Empty);
                return;
            }

            int cost = _tree.PathTo(node.Id).Count;
            if (cost == 0)
            {
                ShowHint(string.Empty);
                return;
            }

            ShowHint(Localization.Render(PassiveWheelText.PathCost,
                new Dictionary<string, object?> { [PassiveWheelText.PointsValue] = cost }));
        }

        private void ShowHint(string text)
        {
            if (_hint == null) return;

            _hint.Text = text;
            _hint.Visible = text.Length > 0;
        }

        private void ShowCard(PassiveNode? node)
        {
            if (_card == null) return;
            if (node == null || _tree == null)
            {
                _card.Show(null, null, AllocationResult.UnknownNode, AllocationResult.UnknownNode, 0, []);
                return;
            }

            List<string> lines = [];
            foreach (PassiveNodeLine line in PassiveNodeLines.Of(node, _modifiers, _knobs, _localization, TextFormat.Rich))
                lines.Add(line.Text);

            _card.Show(_tree.Tree, node, _tree.CheckTake(node.Id), _tree.CheckRefund(node.Id), _tree.PathTo(node.Id).Count, lines);
        }

        /// <summary>
        /// The numbers that move with the tree: the points still to spend, and how many augments are
        /// sitting in slots no node backs any more. The second one is the same count the mastery window
        /// shows, in the same words — without it the player never learns there is property of his in
        /// the dimmed cells waiting to be taken out.
        /// </summary>
        private void RefreshAllocation()
        {
            if (_available != null)
            {
                _available.Visible = _tree != null;
                if (_tree != null) _available.Text = _tree.AvailablePoints.ToString();
            }

            if (_spent != null && _tree != null) _spent.Text = _tree.SpentPoints.ToString();
            if (_total != null && _tree != null) _total.Text = _tree.TotalPoints.ToString();

            // Not the design budget beside it: the service measures every check against the total
            // mastery granted, and a design ceiling printed next to it would be a promise nobody keeps.
            if (_noPoints != null)
            {
                _noPoints.Visible = _tree is { TotalPoints: 0 };
                if (_noPoints.Visible) _noPoints.Text = Localization.Localize(PassiveWheelText.NoPoints);
            }

            int waiting = WaitingAugments();
            if (_waiting != null)
            {
                _waiting.Visible = waiting > 0;
                if (waiting > 0)
                    _waiting.Text = Localization.Render(PassiveWheelText.AugmentsWaiting,
                        new Dictionary<string, object?> { [PassiveWheelText.CountValue] = waiting });
            }

            if (_respec != null) _respec.Disabled = _tree == null || _tree.SpentPoints == 0;

            ShowCard(_canvas?.Selected);
        }

        private int WaitingAugments() =>
            _board?.Sockets.Count(socket => !socket.IsOpen && !socket.IsEmpty) ?? 0;

        /// <summary>The dialog says the other half of the truth: a respec closes every socket, and the
        /// augments in them stay behind in cells the player then has to empty by hand.</summary>
        private void AskRespec()
        {
            if (_respecConfirm == null)
            {
                Respec();
                return;
            }

            _respecConfirm.DialogText = Localization.Render(PassiveWheelText.RespecConfirm,
                new Dictionary<string, object?> { [PassiveWheelText.CountValue] = WaitingAugments() });
            _respecConfirm.PopupCentered();
        }

        private void Respec() => _tree?.Respec();
    }
}
