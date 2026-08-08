namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Core.Localization;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The selected node in words: what it is, what it gives, what reaching it costs and why it cannot
    /// be bought or given back. The place the wheel SAYS things instead of showing them — a node whose
    /// socket does not exist yet is an empty space on the panel beside it and a sentence here.
    /// </summary>
    [GlobalClass]
    public partial class PassiveNodeCard : PanelContainer
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://b6ykr3jsvpq0m";

        [Export] private Label? _title;
        [Export] private Label? _kind;
        [Export] private RichTextLabel? _lines;
        [Export] private Label? _cost;
        [Export] private Label? _reason;
        [Export] private Label? _socketPromise;
        [Export] private Button? _take;
        [Export] private Button? _refund;

        public event Action? TakePressed;

        public event Action? RefundPressed;

        public override void _Ready()
        {
            if (_take != null) _take.Pressed += () => TakePressed?.Invoke();
            if (_refund != null) _refund.Pressed += () => RefundPressed?.Invoke();
            Show(null, null, AllocationResult.UnknownNode, AllocationResult.UnknownNode, 0, []);
        }

        /// <summary>
        /// Fills the card from answers somebody else produced: the two verdicts are the allocation
        /// service's own, and the lines have already been through the game's modifier templates. The
        /// card measures nothing and decides nothing — the document is here only so that a refusal can
        /// tell "nothing reaches it yet" from "it is wired to nothing at all".
        /// </summary>
        public void Show(PassiveTreeDocument? document, PassiveNode? node, AllocationResult take, AllocationResult refund, int pathCost, IReadOnlyList<string> lines)
        {
            Visible = node != null && document != null;
            if (node == null || document == null) return;

            if (_title != null) _title.Text = node.Title.Length > 0 ? node.Title : node.Id;
            if (_kind != null) _kind.Text = Localization.Localize($"{PassiveWheelText.KindPrefix}{node.Kind}");
            if (_lines != null) _lines.Text = Join(lines);

            ShowCost(take, refund, pathCost);
            ShowReason(document, node, take, refund);
            ShowSocketPromise(node, take, refund);

            if (_take != null) _take.Disabled = take != AllocationResult.Success;
            if (_refund != null) _refund.Disabled = refund != AllocationResult.Success;
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>A single node always costs one point, so the number worth showing is what the whole
        /// route to it costs — which is the only price the player can act on.</summary>
        private void ShowCost(AllocationResult take, AllocationResult refund, int pathCost)
        {
            if (_cost == null) return;

            _cost.Text = take switch
            {
                AllocationResult.AlreadyTaken => Localization.Localize(PassiveWheelText.Taken),
                AllocationResult.Granted => Localization.Localize(PassiveWheelText.Granted),
                _ when refund == AllocationResult.Granted => Localization.Localize(PassiveWheelText.Granted),
                _ => Localization.Render(PassiveWheelText.PathCost,
                    new Dictionary<string, object?> { [PassiveWheelText.PointsValue] = pathCost })
            };
        }

        /// <summary>Whichever half is refused right now: a node in hand answers "why can it not go
        /// back", a node out of hand answers "why can it not be bought".</summary>
        private void ShowReason(PassiveTreeDocument document, PassiveNode node, AllocationResult take, AllocationResult refund)
        {
            if (_reason == null) return;

            string key = take == AllocationResult.AlreadyTaken
                ? refund == AllocationResult.Success ? string.Empty : PassiveTreeRefusalText.RefundKey(refund)
                : take == AllocationResult.Success
                    ? string.Empty
                    : PassiveTreeRefusalText.TakeKey(document, node.Id, take);

            _reason.Text = key.Length == 0 ? string.Empty : Localization.Localize(key);
            _reason.Visible = key.Length > 0;
        }

        /// <summary>A socket node that is not taken opens no slot, so the panel beside the wheel has
        /// nothing to draw. That is the truth and not a bug — said in a sentence rather than left as an
        /// empty box.</summary>
        private void ShowSocketPromise(PassiveNode node, AllocationResult take, AllocationResult refund)
        {
            if (_socketPromise == null) return;

            int tier = NodeKindRules.SocketTier(node.Kind);
            bool held = take == AllocationResult.AlreadyTaken || refund == AllocationResult.Granted;
            bool promised = tier != NodeKindRules.NoSocket && node.AbilityId.Length > 0 && !held;

            _socketPromise.Visible = promised;
            if (promised)
                _socketPromise.Text = Localization.Render(PassiveWheelText.SocketPromise,
                    new Dictionary<string, object?> { [PassiveWheelText.TierValue] = tier });
        }

        private static string Join(IReadOnlyList<string> lines)
        {
            var text = new StringBuilder();
            foreach (string line in lines)
            {
                if (text.Length > 0) text.Append('\n');
                text.Append(line);
            }

            return text.ToString();
        }
    }
}
