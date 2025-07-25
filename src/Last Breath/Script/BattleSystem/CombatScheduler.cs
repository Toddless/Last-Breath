namespace Playground.Script.BattleSystem
{
    using System;
    using System.Collections.Generic;
    using Godot;

    public class CombatScheduler
    {
        private readonly LinkedList<AttackContext> _attackQueue = new();
        private bool _inProcess = false, _isCancelled = false;
        public event Action? AllContexHandled;

        public static CombatScheduler? Instance { get; private set; }

        public CombatScheduler()
        {
            Instance = this;
        }

        public void Schedule(AttackContext context)
        {
            if (_inProcess)
                _attackQueue.AddFirst(context);
            else
                _attackQueue.AddLast(context);
            GD.Print($"Added new context to Queue. Target: {context.Target.GetType().Name}, Attacker: {context.Attacker.GetType().Name}");
        }

        public void RunQueue()
        {
            _inProcess = true;
            while (_attackQueue.Count > 0 && !_isCancelled)
            {
                var context = _attackQueue.First?.Value;
                if (context == null) break;
                _attackQueue.RemoveFirst();
                GD.Print($"Processing context. Target: {context.Target.GetType().Name}, Attacker: {context.Attacker.GetType().Name}");
                context.Target.OnReceiveAttack(context);
                GD.Print($"Attack contextes left: {_attackQueue.Count}");
            }
            _inProcess = false;
            CheckQueueLeft();
        }

        private void CheckQueueLeft()
        {
            if (_attackQueue.Count == 0)
                AllContexHandled?.Invoke();
        }

        public void CancelQueue()
        {
            _isCancelled = true;
            while (_attackQueue.Count > 0)
            {
                var context = _attackQueue.First?.Value;
                if (context == null) break;
                _attackQueue.RemoveFirst();
                context.CancelAttack();
            }
            _isCancelled = false;
            CheckQueueLeft();
        }
    }
}
