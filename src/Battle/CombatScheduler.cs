namespace Battle
{
    using Godot;
    using System;
    using Core.Interfaces.Battle;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public class CombatScheduler : ICombatScheduler
    {
        private readonly Queue<IAttackContext> _attackQueue = [];
        private bool _isCancelled, _isProcessing;

        public void Schedule(IAttackContext context)
        {
            _attackQueue.Enqueue(context);
        }

        public async Task RunQueue()
        {
            try
            {
                if (_isProcessing) return;
                _isProcessing = true;
                while (_attackQueue.Count > 0 && !_isCancelled)
                {
                    var context = _attackQueue.Dequeue();
                    await context.Attacker.Attack(context);
                    await context.Target.ReceiveAttack(context);
                }
            }
            catch (Exception ex)
            {
                GD.Print("Exception: " + ex.Message);
            }
            finally
            {
                _attackQueue.Clear();
                _isProcessing = false;
                _isCancelled = false;
            }
        }


        public void CancelQueue()
        {
            _isCancelled = true;
            _attackQueue.Clear();
        }
    }
}
