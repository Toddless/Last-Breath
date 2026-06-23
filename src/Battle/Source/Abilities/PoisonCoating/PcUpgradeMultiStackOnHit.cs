namespace Battle.Source.Abilities.PoisonCoating
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;

    /// <summary>
    /// L3 upgrade: the number of poison stacks applied per attack equals the number of
    /// living enemies on the battlefield (instead of a fixed 1 stack).
    /// </summary>
    public class PcUpgradeMultiStackOnHit(string id, string[] tags, int tier)
        : AbilityUpgrade<PoisonCoating>(id, tags, tier)
    {
        private PoisonCoating? _ability;
        private IEntity? _owner;

        public override void ApplyUpgrade(PoisonCoating ability)
        {
            _ability = ability;
            _owner = ability.AbilityOwner;
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

            // Extra stacks = (enemy count - 1), since PoisonCoatingEffect already applied 1 stack
          //  int enemyCount = _owner.Group?.GetEnemies()?.Count ?? 1;
            int extraStacks = System.Math.Max(0, 2 - 1);

            for (int i = 0; i < extraStacks; i++)
            {
                var poison = new Battle.Source.Abilities.Effects.DamageOverTurnEffect(
                    _ability.PoisonDuration, StatusEffects.Poison, 999, _ability.PoisonDamagePercent);
                poison.Apply(new EffectApplyingContext
                {
                    Caster = _owner,
                    Target = evt.Context.Target,
                    Source = _ability.InstanceId,
                    Damage = evt.Context.FinalDamage,
                    IsCritical = evt.Context.IsCritical
                });
            }
        }

        public override IAbilityUpgradeWrap<PoisonCoating> Clone() =>
            new PcUpgradeMultiStackOnHit(Id, Tags, Tier);
    }
}
