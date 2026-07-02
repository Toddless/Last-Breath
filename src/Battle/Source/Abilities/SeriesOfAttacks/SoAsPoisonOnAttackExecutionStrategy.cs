namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Effects;
    using Godot;
    using Utilities;

    public class SoAsPoisonOnAttackExecutionStrategy(int duration) : SoAsDefaultExecutionStrategy
    {
        private SeriesOfAttacks? _ability;
        private IFightable? _owner;

        public override async Task Execute(SeriesOfAttacks ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            Subscribe(ability, owner);
            await base.Execute(ability, owner, targets, field);
            Unsubscribe(owner);
        }

        private async void OnAfterAttack(AfterAttackEvent obj)
        {
            try
            {
                if (_ability == null || _owner == null) return;
                if (obj.Context.Result is not AttackResults.Succeed) return;
                var context = obj.Context;
                var poison = new DamageOverTurnEffect(duration, StatusEffects.Poison);
                var applyContext = new EffectApplyingContext
                {
                    Caster = _owner,
                    Target = context.Target,
                    Source = _ability.InstanceId,
                    Damage = context.FinalDamage,
                    IsCritical = context.IsCritical
                };
                await poison.Apply(applyContext);
            }
            catch (Exception ex)
            {
                GD.Print($"Failed to apply poison: {ex.Message}, {ex.StackTrace}");
                Tracker.TrackException("Failed to apply poison", ex, this);
            }
        }

        private void Subscribe(SeriesOfAttacks ability, IFightable owner)
        {
            _ability = ability;
            _owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void Unsubscribe(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            _ability = null;
            _owner = null;
        }
    }
}
