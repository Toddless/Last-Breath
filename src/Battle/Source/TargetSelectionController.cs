namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Events;
    using Core.Events.GameEvents;

    /// <summary>
    /// Owns the player's target-selection phase. Highlights valid spots per the ability's
    /// <see cref="ITargetingStrategy"/>, collects picks, and commits once — publishing
    /// <see cref="AbilityActivationEvent"/> (the single commit point) with the resolved targets stashed for
    /// <see cref="BattleArena"/> to consume. Selection is free and cancellable; commit pays cost/cooldown.
    /// While no ability is being selected, the player's turn highlights enemies as basic-attack targets.
    /// </summary>
    public sealed class TargetSelectionController
    {
        private enum Mode
        {
            Idle,
            BasicAttack,
            Ability
        }

        private readonly IBattleEventBus _bus;
        private readonly IBattleField _field;
        private readonly IReadOnlyList<EntitySpot> _spots;
        private readonly List<EntitySpot> _highlighted = [];
        private readonly List<IFightable> _chosen = [];
        private readonly Dictionary<string, IReadOnlyList<IFightable>> _committed = [];

        private Mode _mode = Mode.Idle;
        private IFightable? _caster;
        private IAbility? _ability;
        private string _selectionId = string.Empty;

        public TargetSelectionController(IBattleEventBus bus, IBattleField field, IReadOnlyList<EntitySpot> spots)
        {
            _bus = bus;
            _field = field;
            _spots = spots;
            _bus.Subscribe<PlayerSelectingTargetForAbilityEvent>(OnSelectionStarted);
            _bus.Subscribe<AbilityTargetPickedEvent>(OnTargetPicked);
            _bus.Subscribe<ConfirmSelectionEvent>(OnConfirm);
            _bus.Subscribe<CancelSelectionEvent>(OnCancel);
        }

        /// <summary>Begin the player's turn: enemies are highlighted as basic-attack targets.</summary>
        public void BeginBasicAttack(IFightable player)
        {
            _caster = player;
            StartBasicAttack();
        }

        /// <summary>End of the player's turn (or battle): drop all highlights and state.</summary>
        public void EndTargeting()
        {
            ClearHighlights();
            _chosen.Clear();
            _ability = null;
            _selectionId = string.Empty;
            _mode = Mode.Idle;
            _caster = null;
        }

        /// <summary>Targets resolved at the last commit for <paramref name="selectionId"/>. Consumed by the arena.</summary>
        public IReadOnlyList<IFightable> TakeCommittedTargets(string selectionId) =>
            _committed.Remove(selectionId, out var targets) ? targets : [];

        private void StartBasicAttack()
        {
            if (_caster == null) return;
            ClearHighlights();
            _chosen.Clear();
            _ability = null;
            _selectionId = string.Empty;
            _mode = Mode.BasicAttack;
            HighlightValid(_field.GetEnemies(_caster), forAttack: true);
        }

        private void OnSelectionStarted(PlayerSelectingTargetForAbilityEvent evt)
        {
            if (_caster == null) return; // ability activation only makes sense on the player's turn
            ClearHighlights();
            _chosen.Clear();
            _mode = Mode.Ability;
            _ability = evt.Ability;
            _selectionId = evt.SelectionId;

            if (!_ability.Targeting.RequiresManualSelection)
            {
                Commit(_ability.Targeting.ResolveAutomatic(_caster, _field));
                return;
            }

            HighlightValid(_ability.Targeting.GetValidTargets(_caster, _field), forAttack: false);
        }

        private void OnTargetPicked(AbilityTargetPickedEvent evt)
        {
            if (_mode != Mode.Ability || _ability == null || evt.SelectionId != _selectionId) return;

            if (_ability.Targeting.MaxTargets == 1)
            {
                Commit([evt.Target]);
                return;
            }

            ToggleChosen(evt.Target);
        }

        private void OnConfirm(ConfirmSelectionEvent evt)
        {
            if (_mode != Mode.Ability || evt.SelectionId != _selectionId) return;
            if (_chosen.Count == 0)
            {
                CancelBackToAttack(); // nothing picked yet: a confirm press means "never mind"
                return;
            }

            Commit(_chosen.ToList());
        }

        private void OnCancel(CancelSelectionEvent evt)
        {
            if (_mode != Mode.Ability || evt.SelectionId != _selectionId) return;
            CancelBackToAttack();
        }

        private void ToggleChosen(IFightable target)
        {
            var spot = SpotFor(target);
            if (spot == null) return;
            if (_chosen.Remove(target))
            {
                spot.SetChosen(false);
                return;
            }

            if (_chosen.Count >= _ability!.Targeting.MaxTargets) return;
            _chosen.Add(target);
            spot.SetChosen(true);
        }

        private void Commit(IReadOnlyList<IFightable> targets)
        {
            var selectionId = _selectionId;
            var ability = _ability!;
            _committed[selectionId] = targets;
            ClearHighlights();
            _bus.Publish<AbilityActivationEvent>(new(ability, selectionId));
            _bus.Publish<TargetSelectionResolvedEvent>(new(selectionId));
            StartBasicAttack(); // the cast does not end the turn; resume attack targeting
        }

        private void CancelBackToAttack()
        {
            var selectionId = _selectionId;
            StartBasicAttack();
            _bus.Publish<TargetSelectionResolvedEvent>(new(selectionId));
        }

        private void HighlightValid(IReadOnlyList<IFightable> targets, bool forAttack)
        {
            foreach (var target in targets)
            {
                var spot = SpotFor(target);
                if (spot == null) continue;
                spot.MarkSelectable(_selectionId, forAttack);
                _highlighted.Add(spot);
            }
        }

        private void ClearHighlights()
        {
            foreach (var spot in _highlighted) spot.ClearSelection();
            _highlighted.Clear();
        }

        private EntitySpot? SpotFor(IFightable entity) =>
            _spots.FirstOrDefault(spot => spot.Entity?.IsSame(entity.InstanceId) == true);
    }
}
