namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    /// <summary>
    /// L3 upgrade strategy: after applying poison, when the target dies, remaining
    /// poison stacks are transferred to a random living enemy.
    /// </summary>
    public class JoPTransferOnDeathStrategy : JoPDefaultExecutionStrategy
    {
        public override async Task Execute(JarOfPoison ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            foreach (IEntity target in targets)
            {
                if (!target.IsAlive) continue;
                await ApplyToTarget(ability, owner, target);
                SubscribeTransfer(target, owner, field);
            }
        }

        private static void SubscribeTransfer(IEntity target, IEntity owner, IBattleField field)
        {
            void OnDead(IEntity dead)
            {
                dead.Dead -= OnDead;
                TransferPoison(dead, owner, field);
            }

            target.Dead += OnDead;
        }

        private static void TransferPoison(IEntity dead, IEntity owner, IBattleField field)
        {
            var poisonStacks = dead.Effects.GetBy(e => e.Status == StatusEffects.Poison).ToList();
            if (poisonStacks.Count == 0) return;

            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0) return;

            // Pick a random living enemy (different from the dead one)
            var rnd = new Godot.RandomNumberGenerator();
            rnd.Randomize();
            var newTarget = enemies[rnd.RandiRange(0, enemies.Count - 1)];

            foreach (IEffect stack in poisonStacks)
            {
                var clone = stack.Clone();
                clone.Apply(new EffectApplyingContext
                {
                    Caster = owner,
                    Target = newTarget,
                    Source = dead.InstanceId,
                    Damage = (stack is Battle.Source.Abilities.Effects.DamageOverTurnEffect dot) ? dot.DamagePerTick : 0
                });
            }
        }
    }
}
