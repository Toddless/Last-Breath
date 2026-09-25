namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using Core.Events;
    using Godot;

    /// <summary>
    /// The battle-log panel: a ring buffer of ready BBCode lines with per-category filter toggles and a
    /// collapse button. Dumb by design — all formatting lives in <see cref="BattleLogPresenter"/>.
    /// </summary>
    public partial class BattleLog : MarginContainer
    {
        private const int MaxEntries = 150;

        private readonly List<BattleLogEntry> _entries = [];
        private readonly HashSet<BattleLogCategory> _hidden = [];
        private BattleLogPresenter? _presenter;
        [Export] private RichTextLabel? _text;
        [Export] private Button? _collapseButton;
        [Export] private Button? _damageFilter, _attackFilter, _healFilter, _abilityFilter, _effectFilter;

        public override void _Ready()
        {
            _collapseButton?.Toggled += OnCollapseToggled;
            WireFilter(_damageFilter, BattleLogCategory.Damage);
            WireFilter(_attackFilter, BattleLogCategory.AttackResult);
            WireFilter(_healFilter, BattleLogCategory.Heal);
            WireFilter(_abilityFilter, BattleLogCategory.Ability);
            WireFilter(_effectFilter, BattleLogCategory.Effect);
        }

        public override void _ExitTree() => DetachPresenter();

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            DetachPresenter(); // the next battle's presenter replaces the previous one entirely
            _entries.Clear();
            _text?.Clear();
            _presenter = new BattleLogPresenter(battleEventBus);
            _presenter.EntryAdded += OnEntryAdded;
        }

        private void DetachPresenter()
        {
            if (_presenter == null) return;
            _presenter.EntryAdded -= OnEntryAdded;
            _presenter.Detach();
            _presenter = null;
        }

        private void OnEntryAdded(BattleLogEntry entry)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries) _entries.RemoveAt(0);
            if (IsVisibleCategory(entry.Category)) _text?.AppendText(entry.Text + "\n");
        }

        private void WireFilter(Button? button, BattleLogCategory category)
        {
            button?.Toggled += pressed =>
            {
                if (pressed) _hidden.Remove(category);
                else _hidden.Add(category);
                Rebuild();
            };
        }

        /// <summary>Collapsed = header only, the feed is hidden.</summary>
        private void OnCollapseToggled(bool collapsed) => _text?.Visible = !collapsed;

        private bool IsVisibleCategory(BattleLogCategory category) =>
            category == BattleLogCategory.System || !_hidden.Contains(category);

        private void Rebuild()
        {
            if (_text == null) return;
            _text.Clear();
            foreach (var entry in _entries)
                if (IsVisibleCategory(entry.Category))
                    _text.AppendText(entry.Text + "\n");
        }
    }
}
