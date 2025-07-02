namespace Playground.Script.BattleSystem
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Components;

    public class CombatScheduler
    {
        private Queue<AttackContext> _contextQueue = [];
        public event Action? AllContexHandled;

        public static CombatScheduler? Instance { get; private set; }

        public CombatScheduler()
        {
            Instance = this;
        }

        public void Schedule(AttackContext context)
        {
            _contextQueue.Enqueue(context);
        }

        public void RunQueue()
        {
            while (_contextQueue.Count > 0)
            {
                var context = _contextQueue.Dequeue();
                GD.Print($"Processing context. Target: {context.Target.GetType().Name}, Attacker: {context.Attacker.GetType().Name}");
                context.Target.OnReceiveAttack(context);
            }
            AllContexHandled?.Invoke();
        }
    }
}
