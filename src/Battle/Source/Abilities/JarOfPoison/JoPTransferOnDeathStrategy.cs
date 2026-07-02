namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Effects;

    /// <summary>
    /// L3 upgrade strategy: after applying poison, when the target dies, remaining
    /// poison stacks are transferred to a random living enemy.
    /// </summary>
    public class JoPTransferOnDeathStrategy : JoPDefaultExecutionStrategy
    {
        public override async Task Execute(JarOfPoison ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            foreach (IFightable target in targets)
            {
                if (!target.IsAlive) continue;
                await ApplyToTarget(ability, owner, target);
                SubscribeTransfer(target, owner, field);
            }
        }

        private static void SubscribeTransfer(IFightable target, IFightable owner, IBattleField field)
        {
            target.Dead += OnDead;
            return;

            void OnDead(IFightable dead)
            {
                dead.Dead -= OnDead;
                TransferPoison(dead, owner, field);
            }
        }

        private static void TransferPoison(IFightable dead, IFightable owner, IBattleField field)
        {
            var poisonStacks = dead.Effects.GetBy(e => e.Status == StatusEffects.Poison).OfType<DamageOverTurnEffect>().ToList();
            if (poisonStacks.Count == 0) return;

            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0) return;

            var rnd = new Godot.RandomNumberGenerator();
            rnd.Randomize();
            var newTarget = enemies[rnd.RandiRange(0, enemies.Count - 1)];

            foreach (var stack in poisonStacks)
            {
                var clone = stack.Copy();
                clone.Apply(new EffectApplyingContext
                {
                    Caster = owner,
                    Target = newTarget,
                    Source = dead.InstanceId,
                    Damage = stack.DamagePerTick
                });
            }
        }
    }
}
