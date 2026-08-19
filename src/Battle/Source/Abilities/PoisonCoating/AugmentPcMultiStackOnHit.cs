namespace Battle.Source.Abilities.PoisonCoating
{
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// L3 upgrade: the number of poison stacks applied per attack equals the number of
    /// living enemies on the battlefield (instead of a fixed 1 stack).
    /// </summary>
    public class AugmentPcMultiStackOnHit(string id, string[] tags, int tier)
        : Augment<PoisonCoating>(id, tags, tier)
    {
        private PoisonCoating? _ability;
        private IFightable? _owner;

        public override void ApplyUpgrade(PoisonCoating ability)
        {
            _ability = ability;
            _owner?.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }


        public override void RemoveUpgrade(PoisonCoating ability)
        {
            _owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            _ability = null;
            _owner = null;
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (_owner == null || _ability == null) return;
            if (evt.Context.Result != AttackResults.Succeed) return;

            // Extra stacks = (enemy count - 1), since PoisonCoatingEffect already applied 1 stack.
            // The target's group is the enemy side from the owner's perspective.
            int enemyCount = evt.Context.Target.Group?
                .GetEntitiesInGroup<IFightable>()
                .Count(e => e.IsAlive) ?? 1;
            int extraStacks = System.Math.Max(0, enemyCount - 1);

            for (int i = 0; i < extraStacks; i++)
            {
                var poison = new Effects.DamageOverTurnEffect(
                    _ability.PoisonDuration, StatusEffects.Poison, 999, _ability.PoisonDamagePercent);
                poison.Apply(new EffectApplyingContext
                {
                    Caster = _owner,
                    Target = evt.Context.Target,
                    Source = _ability.InstanceId,
                    Damage = evt.Context.FinalDamage,
                    IsCritical = evt.Context.IsCritical,
                    Trace = _ability.Trace
                });
            }
        }

        public override IAugment Copy() => new AugmentPcMultiStackOnHit(Id, Tags, Tier);
    }
}
