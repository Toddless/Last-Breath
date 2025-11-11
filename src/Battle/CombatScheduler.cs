namespace Battle
{
    using System;
    using System.Collections.Generic;

    public class CombatScheduler
    {
        private readonly LinkedList<AttackContext> _attackQueue = [];
        private bool _inProcess, _isCancelled;
        public event Action? AllContextsHandled;

        public void Schedule(AttackContext context)
        {
            if (_inProcess)
                _attackQueue.AddFirst(context);
            else
                _attackQueue.AddLast(context);
        }

        public void RunQueue()
        {
            _inProcess = true;
            while (_attackQueue.Count > 0 && !_isCancelled)
            {
                var context = _attackQueue.First?.Value;
                if (context == null) break;
                _attackQueue.RemoveFirst();
                context.Target.OnReceiveAttack(context);
            }
            _inProcess = false;
            CheckQueueLeft();
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

        private void CheckQueueLeft()
        {
            if (_attackQueue.Count == 0)
                AllContextsHandled?.Invoke();
        }
    }
}
