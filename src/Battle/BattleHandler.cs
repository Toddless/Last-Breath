namespace Battle
{
    using Godot;
    using System;
    using Stateless;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    public class BattleHandler
    {
        private enum State
        {
            AwaitingBattleToStart,
            TurnStartPhase,
            AttackingPhase,
            TurnEndPhase,
            TurnTransitionPhase,
            EndBattle
        }

        private enum Trigger
        {
            AwaitForBattle,
            NextPhase,
            EndBattle
        }

        private readonly StateMachine<State, Trigger> _machine = new(State.AwaitingBattleToStart);
        private StateMachine<State, Trigger>.TriggerWithParameters<BattleResults>? _battleEnds;
        private IEntity? _currentAttacking;
        private List<IEntity> _fighters = [];
        private Queue<IEntity> _attackQueue = [];
        private CombatScheduler _combatScheduler;

        public event Action<BattleResults>? BattleEnd;
        public event Action<IEntity>? StartTurn;
        public event Action? OnEnterStartPhase, OnExitStartPhase;

        public IReadOnlyList<IEntity> Fighters => _fighters;

        public BattleHandler()
        {
            _combatScheduler = new CombatScheduler();
            ConfigureStateMachine();
        }

        public void BattleStart()
        {
            //UIEventBus.NextTurnPhase += NextPhase;
            SetNextFighter();
            NextPhase();
        }

        public void PlayerEscapedBattle()
        {
            _machine.Fire(_battleEnds, BattleResults.PlayerEscaped);
        }

        public void PlayerFailToEscapeBattle()
        {
            // if (_currentAttacking is Player p && _machine.State == State.TurnStartPhase) NextPhase();
        }

        public Type? GetTypeOfCurrentAttacker() => _currentAttacking?.GetType();

        public void CleanBattleHandler()
        {
            foreach (var fighter in _fighters)
            {
                //if (fighter is Player p)
                //{
                //    p.Dead -= OnPlayerDead;
                //    continue;
                //}

                //fighter.Dead -= OnFighterDeath;
            }

            _attackQueue.Clear();
            _fighters.Clear();
            _currentAttacking = null;
        }

        private void NextPhase() => _machine.Fire(Trigger.NextPhase);

        private void PerformTurnTransition()
        {
            // Set current character at the end of a queue, if he still alive
            SetCurrentAttackerToEndOfQueue();
            SetNextFighter();
            NextPhase();
        }

        private void SetCurrentAttackerToEndOfQueue()
        {
            if (_currentAttacking is { IsAlive: true })
                _attackQueue.Enqueue(_currentAttacking);
        }

        private void SetNextFighter()
        {
            // looking for next fighter, until only one character left
            while (_attackQueue.Count > 1)
            {
                var nextFighter = _attackQueue.Dequeue();
                if (!nextFighter.IsAlive) continue;

                _currentAttacking = nextFighter;
                return;
            }

            // no characters alive except one => battle ends (but probably we should end the battle way earlier)
            CheckIfBattleShouldEnd();
        }


        private void DecideTurnsOrder()
        {
        }


        // I cant delete character from attack queue on death (Queue<T> has no methods for this), so I made this workaround
        private void OnFighterDeath(IEntity character)
        {
            CheckIfBattleShouldEnd();
        }

        private void OnPlayerDead(IEntity character)
        {
            // Handle player dead
            _combatScheduler.CancelQueue();
            _machine.Fire(_battleEnds, BattleResults.PlayerDead);
        }

        private void CheckIfBattleShouldEnd()
        {
            // <2 because it is still players turn, and enemies still in queue
            //if (_currentAttacking is Player && _attackQueue.Count < 2)
            //{
            //    GD.Print($"Attack queue:{_attackQueue.Count}");
            //    GD.Print("Check if battle should end is true");
            //    _combatScheduler.CancelQueue();
            //    _machine.Fire(_battleEnds, BattleResults.PlayerWon);
            //}
        }

        private void EndBattle(BattleResults results)
        {
            BattleEnd?.Invoke(results);
            _machine.Fire(Trigger.AwaitForBattle);
            // UIEventBus.NextTurnPhase -= NextPhase;
        }

        private void OnAllContextsHandled() => NextPhase();

        private void ConfigureStateMachine()
        {
            _battleEnds = _machine.SetTriggerParameters<BattleResults>(Trigger.EndBattle);

            _machine.Configure(State.AwaitingBattleToStart)
                .OnExit(() => GD.Print($"Leaving state: {_machine.State}"))
                .Permit(Trigger.NextPhase, State.TurnStartPhase);

            _machine.Configure(State.TurnStartPhase)
                .OnEntry(TurnStart)
                .OnExit(() =>
                {
                    GD.Print($"Leaving state: {_machine.State}");
                    OnExitStartPhase?.Invoke();
                })
                .Permit(Trigger.EndBattle, State.EndBattle)
                .Permit(Trigger.NextPhase, State.AttackingPhase);

            _machine.Configure(State.AttackingPhase)
                .OnEntry(() =>
                {
                    _combatScheduler.AllContextsHandled += OnAllContextsHandled;
                    _combatScheduler.RunQueue();
                })
                .OnExit(() =>
                {
                    GD.Print($"Leaving state: {_machine.State}");
                    _combatScheduler.AllContextsHandled -= OnAllContextsHandled;
                })
                .Permit(Trigger.EndBattle, State.EndBattle)
                .Permit(Trigger.NextPhase, State.TurnEndPhase);


            _machine.Configure(State.TurnEndPhase)
                .OnEntry(TurnEnds)
                .OnExit(() => GD.Print($"Leaving state: {_machine.State}"))
                .Permit(Trigger.EndBattle, State.EndBattle)
                .Permit(Trigger.NextPhase, State.TurnTransitionPhase);

            _machine.Configure(State.TurnTransitionPhase)
                .OnEntry(PerformTurnTransition)
                .OnExit(() => GD.Print($"Leaving state: {_machine.State}"))
                .Permit(Trigger.NextPhase, State.TurnStartPhase);

            _machine.Configure(State.EndBattle)
                .Ignore(Trigger.NextPhase)
                .OnEntryFrom(_battleEnds, EndBattle)
                .OnExit(() => GD.Print($"Leaving state: {_machine.State}"))
                .Permit(Trigger.AwaitForBattle, State.AwaitingBattleToStart);
        }

        private void TurnStart()
        {
            if (_currentAttacking != null)
                StartTurn?.Invoke(_currentAttacking);
            //if (_currentAttacking is Player p)
            //{
            //    p?.OnTurnStart();
            //}
            //OnEnterStartPhase?.Invoke();
        }

        private void TurnEnds()
        {
            _currentAttacking?.OnTurnEnd();
            NextPhase();
        }
    }
}
