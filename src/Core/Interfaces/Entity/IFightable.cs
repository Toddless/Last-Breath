namespace Core.Interfaces.Entity
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle;
    using Components;
    using Enums;
    using Events;

    /// <summary>
    /// An entity that can participate in combat: resources, parameters, effects,
    /// passives, modifier pipelines and the combat loop itself.
    /// </summary>
    public interface IFightable : IEntity
    {
        // Components
        IEffectsComponent Effects { get; }
        IParameterModifiersComponent ParameterModifiers { get; }
        IEntityParametersComponent Parameters { get; }
        IPassiveSkillsComponent PassiveSkills { get; }
        IModifierHandlerComponent ModifierHandler { get; }
        IAbilityBookComponent AbilityBook { get; }
        IEntityAttribute Dexterity { get; }
        IEntityAttribute Strength { get; }
        IEntityAttribute Intelligence { get; }
        IEntityGroup? Group { get; set; }

        // Combat wiring
        ICombatEventBus CombatEvents { get; }
        IStance CurrentStance { get; }
        ITargetChooser? TargetChooser { get; set; }
        bool IsFighting { get; set; }
        bool IsAlive { get; }
        StatusEffects StatusEffects { get; }

        // Resources
        float CurrentHealth { get; set; }
        float CurrentBarrier { get; set; }
        float CurrentMana { get; set; }

        event Action<float>? CurrentManaChanged;
        event Action<float>? CurrentBarrierChanged;
        event Action<float>? CurrentHealthChanged;

        float GetDamage();
        void ConsumeResource(Costs type, float amount);
        bool TryApplyStatusEffect(StatusEffects statusEffect);
        bool TryRemoveStatusEffect(StatusEffects statusEffect);

        // Combat loop
        IFightable ChoseTarget(List<IFightable> targets);
        void Kill();
        void SetupBattleEventBus(IBattleEventBus bus);
        Task ReceiveAttack(IAttackContext context);
        Task Attack(IAttackContext context);
        Task TakeDamage(IDamageContext context);
        void Heal(IHealContext context);
        void OnTurnStart();
        void OnTurnEnd();
    }
}
