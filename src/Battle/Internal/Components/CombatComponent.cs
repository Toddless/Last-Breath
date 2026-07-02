namespace Battle.Internal.Components
{
    using System;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Godot;
    using Source;
    using Utilities;

    /// <summary>
    /// Shared combat pipeline for all entities (player and NPCs): attack resolution,
    /// incoming damage, healing and turn transitions. Entity-specific concerns
    /// (death notifications, resource-change events, state machines) stay in the entity.
    /// </summary>
    public class CombatComponent(IEntity owner) : ICombatComponent
    {
        public IBattleEventBus? BattleEventBus { get; set; }
        public IGameEventBus? GameEventBus { get; set; }

        public async Task ReceiveAttack(IAttackContext context)
        {
            try
            {
                Calculations.CalculateSucceeded(context);
                switch (context.Result)
                {
                    case AttackResults.Succeed:
                        Calculations.CalculateFinalDamage(context);
                        var damageContext = new DamageContext
                        {
                            Source = context.Attacker,
                            Damage = context.FinalDamage,
                            Type = DamageType.Normal,
                            Cause = DamageCause.Attack,
                            IsCrit = context.ForceCriticalAttack || context.IsCritical
                        };
                        await owner.TakeDamage(damageContext);
                        context.FinalDamage = damageContext.Damage; // actual damage dealt to target (barrier-absorbed included)
                        break;
                    case AttackResults.Blocked:
                        owner.CombatEvents.Publish<AttackBlockedEvent>(new(context));
                        break;
                    case AttackResults.Evaded:
                        owner.CombatEvents.Publish<AttackEvadedEvent>(new(context));
                        break;
                }

                // Single post-attack channel: all reactions (effects, passives, upgrades) subscribe to this event
                context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));
            }
            catch (Exception e)
            {
                GD.Print($"{e.Message}, {e.StackTrace}");
            }
        }

        public async Task Attack(IAttackContext context)
        {
            // BeforeAttack reactions may mutate RawCriticalChance, so the crit roll happens after them
            owner.CombatEvents.Publish(new BeforeAttackEvent(context));
            context.IsCritical = context.Rnd.Randf() <= context.RawCriticalChance;
            await owner.Animations.PlayAnimationAsync("Fight_Attack");
        }

        public async Task TakeDamage(IDamageContext context)
        {
            owner.ModifierHandler.Apply(context);
            owner.CombatEvents.Publish(new BeforeDamageTakenEvent(context));

            float remaining = context.Damage;
            if (owner.CurrentBarrier > 0)
            {
                float absorbed = Mathf.Min(owner.CurrentBarrier, remaining);
                context.AbsorbedByBarrier = absorbed;
                owner.CurrentBarrier -= absorbed;
                remaining -= absorbed;
            }

            if (remaining > 0) owner.CurrentHealth -= remaining;

            owner.CombatEvents.Publish(new DamageTakenEvent(context, owner));
            BattleEventBus?.Publish(new DamageTakenEvent(context, owner));
            await owner.Animations.PlayAnimationAsync("Fight_Hurt");
        }

        public void Heal(IHealContext context)
        {
            owner.ModifierHandler.Apply(context);
            if (context.Amount <= 0) return;
            if (context.ConvertToDamage)
            {
                _ = owner.TakeDamage(new DamageContext
                {
                    Source = context.Source,
                    Damage = context.Amount,
                    Type = DamageType.Normal,
                    Cause = DamageCause.Passive
                });
                return;
            }

            float amount = context.Amount;
            owner.CombatEvents.Publish<EntityHealedEvent>(new(owner, amount));
            BattleEventBus?.Publish(new EntityHealedEvent(owner, amount));
            owner.CurrentHealth += amount;
        }

        public void OnTurnStart()
        {
            owner.Effects.TriggerTurnStart();
            owner.CombatEvents.Publish(new TurnStartEvent(owner));
            BattleEventBus?.Publish(new TurnStartEvent(owner));
            GameEventBus?.Publish(new TurnStartEvent(owner));
        }

        public void OnTurnEnd()
        {
            owner.Effects.TriggerTurnEnd();
            owner.CombatEvents.Publish(new TurnEndEvent());
            BattleEventBus?.Publish(new TurnEndEvent());
            GameEventBus?.Publish(new TurnEndEvent());
        }
    }
}
