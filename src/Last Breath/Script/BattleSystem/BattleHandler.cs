namespace Playground.Script.BattleSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground;
    using Playground.Script;
    using Playground.Script.Enums;
    using Playground.Script.ScenesHandlers;
    using Stateless;

    public class BattleHandler
    {
        private enum State { AwaitingBattleToStart, TurnStartPhase, AttackingPhase, TurnEndPhase, TurnTransitionPhase, EndBattle }
        private enum Trigger { AwaitForBattle, NextPhase, EndBattle }

        private readonly StateMachine<State, Trigger> _machine = new(State.AwaitingBattleToStart);
        private StateMachine<State, Trigger>.TriggerWithParameters<BattleResults>? _battleEnds;
        private ICharacter? _currentAttacking;
        private List<ICharacter> _fighters = [];
        private List<ICharacter> _alreadyDead = [];
        private Queue<ICharacter> _attackQueue = [];
        private CombatScheduler _combatScheduler;
        public event Action<BattleResults>? BattleEnd;

        public event Action? StartTurn, EndTurn;

        public IReadOnlyList<ICharacter> Fighters => _fighters;
        public static BattleHandler? Instance { get; private set; }

        public BattleHandler()
        {
            _combatScheduler = new CombatScheduler();
            Instance = this;
            ConfigureStateMachine();
        }

        public void Init(BattleContext context)
        {
            _fighters.AddRange(context.Fighters);
            // TODO: Rework context
            foreach (var fighter in _fighters)
            {
                if (fighter is Player player)
                {
                    // Handle player 
                    player.Dead += OnPlayerDead;
                    continue;
                }
                fighter.Dead += OnFighterDeath;
            }
            DecideTurnsOrder();
        }

        public void BattleStart()
        {
            SetNextFighter();
            _machine.Fire(Trigger.NextPhase);
        }

        public void PlayerEscapedBattle()
        {
            _machine.Fire(_battleEnds, BattleResults.PlayerEscaped);
        }

        public void PlayerFailToEscapeBattle()
        {
            if (_currentAttacking is Player p && _machine.State == State.TurnStartPhase) _machine.Fire(Trigger.NextPhase);
        }


        public Type? GetTypeOfCurrentAttacker() => _currentAttacking?.GetType();


        public void CleanBattleHandler()
        {
            foreach (var fighter in _fighters)
            {
                if (fighter is Player p)
                {
                    p.Dead -= OnPlayerDead;
                    continue;
                }

                fighter.Dead -= OnFighterDeath;
            }
            _attackQueue.Clear();
            _fighters.Clear();
            _alreadyDead.Clear();
            _currentAttacking = null;
            BattleEnd = null;
            StartTurn = null;
            EndTurn = null;
        }


        private void PerformTurnTransition()
        {
            // Set current character at the end of an queue, if he still alive
            SetCurrentAttackerToEndOfQueue();
            SetNextFighter();
            _machine.Fire(Trigger.NextPhase);
        }

        private void SetCurrentAttackerToEndOfQueue()
        {
            if (MakeSureFighterAlive(_currentAttacking!))
            {
                GD.Print($"Set to end of queue: {GetName()}");
                _attackQueue.Enqueue(_currentAttacking!);
            }
        }

        private string GetName() => _currentAttacking?.GetType().Name ?? string.Empty;

        private bool MakeSureFighterAlive(ICharacter fighter) => !_alreadyDead.Contains(fighter);

        private void SetNextFighter()
        {
            // looking for next fighter, until only one character left
            while (_attackQueue.Count > 1)
            {
                var nextFighter = _attackQueue.Dequeue();
                if (MakeSureFighterAlive(nextFighter))
                {
                    _currentAttacking = nextFighter;
                    GD.Print($"Set next Fighter: {GetName()}");
                    return;
                }
            }
            // no characters allive except one => battle ends (but probably we should end the battle way ealier)
            CheckIfBattleShouldEnd();
        }

        private void NextPhase() => _machine.Fire(Trigger.NextPhase);

        private void DecideTurnsOrder()
        {
            foreach (var fighter in _fighters.OrderByDescending(x => x.Initiative))
            {
                _attackQueue.Enqueue(fighter);
            }
        }


        // i cant delete character from attack queue on death (Queue<T> has no methods for this), so i made this workaround
        private void OnFighterDeath(ICharacter character)
        {
            CheckIfBattleShouldEnd();
            _alreadyDead.Add(character);
        }

        private void OnPlayerDead(ICharacter character)
        {
            // Handle player dead
            _alreadyDead.Add(character);

            _machine.Fire(_battleEnds, BattleResults.PlayerDead);
        }

        private void CheckIfBattleShouldEnd()
        {
            GD.Print($"End Battle Check. Current Queue: {_attackQueue.Count}");
            if (_attackQueue.Count < 2)
            {
                var character = _attackQueue.Peek();
                if (character is Player)
                {
                    _machine.Fire(_battleEnds, BattleResults.PlayerWon);
                }
                else
                {
                    _machine.Fire(_battleEnds, BattleResults.PlayerDead);
                }
            }
        }

        private void EndBattle(BattleResults results)
        {
            BattleEnd?.Invoke(results);
        }

        private void ConfigureStateMachine()
        {
            _battleEnds = _machine.SetTriggerParameters<BattleResults>(Trigger.EndBattle);

            _machine.Configure(State.AwaitingBattleToStart)
                .Permit(Trigger.NextPhase, State.TurnStartPhase);

            // Что происходит в начале хода
            _machine.Configure(State.TurnStartPhase)
                .OnEntry(() =>
                {
                    StartTurn?.Invoke();
                    // TODO: Make sure moving to next phase fast enough
                    // (is it possible, that current attacking character perform his attack way to fast and battle handler subscribe to event to late?)
                    GD.Print($"Start phase: {_currentAttacking.GetType().Name}");
                    _currentAttacking?.OnTurnStart(NextPhase);
                })
                .OnExit(() =>
                {
                    GD.Print("Exit start phase");

                })
                .Permit(Trigger.EndBattle, State.EndBattle)
                .Permit(Trigger.NextPhase, State.AttackingPhase);

            _machine.Configure(State.AttackingPhase)
                .OnEntry(() =>
                {
                    GD.Print("Enter attacking phase");
                    _combatScheduler.AllContexHandled += OnAllAttacksFinished;
                    _combatScheduler.RunQueue();
                })
                .OnExit(() =>
                {
                    GD.Print("Exit attacking phase");
                    _combatScheduler.AllContexHandled -= OnAllAttacksFinished;
                })
                .Permit(Trigger.EndBattle, State.EndBattle)
                .Permit(Trigger.NextPhase, State.TurnEndPhase);


            _machine.Configure(State.TurnEndPhase)
                 .OnEntry(() =>
                 {
                     EndTurn?.Invoke();
                     _currentAttacking?.OnTurnEnd();
                     _machine.Fire(Trigger.NextPhase);
                     GD.Print($"End phase: {_currentAttacking.GetType().Name}");
                 })
                 .Permit(Trigger.EndBattle, State.EndBattle)
                 .Permit(Trigger.NextPhase, State.TurnTransitionPhase);


            // TODO: Make sure current attacker is alive
            _machine.Configure(State.TurnTransitionPhase)
                .OnEntry(() =>
                {
                    PerformTurnTransition();
                    GD.Print($"Transition phase");
                })
                .Permit(Trigger.NextPhase, State.TurnStartPhase);

            _machine.Configure(State.EndBattle)
                .OnEntryFrom(_battleEnds, EndBattle)
                .Permit(Trigger.AwaitForBattle, State.AwaitingBattleToStart);
        }

        private void OnAllAttacksFinished() => _machine.Fire(Trigger.NextPhase);
    }
}
