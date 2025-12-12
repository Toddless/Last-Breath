namespace LastBreathTest.ComponentTests.TestData
{
    using Battle;
    using Godot;
    using Core.Enums;
    using Battle.Attribute;
    using Battle.Components;
    using Core.Interfaces.Items;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using Battle.Source.CombatEvents;
    using Battle.TestData;
    using Core.Interfaces;
    using Core.Interfaces.Components;

    public class EntityTest : IEntity
    {
        private IRandomNumberGenerator _rnd;
        public string Id { get; } = string.Empty;
        public string[] Tags { get; } = [];
        public Texture2D? Icon { get; } = null;
        public string Description { get; } = string.Empty;
        public string DisplayName { get; } = string.Empty;
        public IEntityParametersComponent Parameters { get; }
        public IPassiveSkillsComponent PassiveSkills { get; }
        public IEntityAttribute Dexterity { get; }
        public IEntityAttribute Strength { get; }
        public IEntityAttribute Intelligence { get; }
        public ICombatEventBus CombatEvents { get; }
        public IStance CurrentStance { get; }
        public ITargetChooser? TargetChooser { get; set; }
        public ICombatScheduler? CombatScheduler { get; set; }
        public bool IsFighting { get; set; }
        public bool IsAlive { get; set; }
        public IEffectsComponent Effects { get; }
        public IModifiersComponent Modifiers { get; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; }

        public float CurrentHealth
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxHealth);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                CurrentHealthChanged?.Invoke(field);
            }
        }

        public float CurrentBarrier
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxHealth);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                CurrentBarrierChanged?.Invoke(field);
            }
        }

        public float CurrentMana
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.Mana);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                CurrentManaChanged?.Invoke(field);
            }
        }

        public event Action<float>? CurrentManaChanged;
        public event Action<float>? CurrentBarrierChanged;
        public event Action<float>? CurrentHealthChanged;
        public event Action<IFightable>? Dead;
        public event Action<IAttackContext>? AttackPerformed;

        public EntityTest()
        {
            Parameters = new EntityParametersComponent();
            Modifiers = new ModifiersComponent();
            Parameters.Initialize(Modifiers.GetModifiers);
            Effects = new EffectsComponent(this);
            PassiveSkills = new PassiveSkillsComponent(this);
            Dexterity = new Dexterity(Modifiers);
            Strength = new Strength(Modifiers);
            Intelligence = new Intelligence(Modifiers);
            Modifiers.ModifiersChanged += Parameters.OnModifiersChange;
            Parameters.ParameterChanged += OnParameterChanged;
            Parameters.ParameterChanged += Dexterity.OnParameterChanges;
            Parameters.ParameterChanged += Strength.OnParameterChanges;
            Parameters.ParameterChanged += Intelligence.OnParameterChanges;
            CombatEvents = new CombatEventBus();
            SetBaseValuesForParameters();
        }

        private void SetBaseValuesForParameters()
        {
            foreach (EntityParameter entityParameter in Enum.GetValues<EntityParameter>())
            {
                float value = 0;

                switch (entityParameter)
                {
                    case EntityParameter.Health:
                    case EntityParameter.Barrier:
                        value = 600;
                        break;
                    case EntityParameter.Mana:
                        value = 50;
                        break;
                    case EntityParameter.Intelligence:
                    case EntityParameter.Strength:
                    case EntityParameter.Dexterity:
                        value = 1f;
                        break;
                    case EntityParameter.Evade:
                    case EntityParameter.Armor:
                        value = 200;
                        break;
                    case EntityParameter.CriticalChance:
                    case EntityParameter.AdditionalHitChance:
                        value = 0.05f;
                        break;
                    case EntityParameter.CriticalDamage:
                        value = 1.5f;
                        break;
                    case EntityParameter.Damage:
                    case EntityParameter.SpellDamage:
                        value = 50f;
                        break;
                }

                Parameters.SetBaseValueForParameter(entityParameter, value);
            }
        }

        private void OnParameterChanged(EntityParameter parameter, float value)
        {
            switch (parameter)
            {
                case EntityParameter.Health:
                    CurrentHealth = value;
                    break;
                case EntityParameter.Barrier:
                    CurrentBarrier = value;
                    break;
                case EntityParameter.Mana:
                    CurrentMana = value;
                    break;
            }
        }

        public bool TryApplyStatusEffect(StatusEffects statusEffect)
        {
            if ((StatusEffects & statusEffect) != 0) return false;
            StatusEffects |= statusEffect;
            CombatEvents.Publish(new StatusEffectAppliedEvent(this, statusEffect));
            return true;
        }

        public bool TryRemoveStatusEffect(StatusEffects statusEffect)
        {
            if ((StatusEffects & statusEffect) == 0) return false;
            StatusEffects &= ~statusEffect;
            CombatEvents.Publish(new StatusEffectRemovedEvent(this, statusEffect));
            return true;
        }

        public void AddItemToInventory(IItem item) => throw new NotImplementedException();
        public float GetDamage() => throw new NotImplementedException();
        public void SetupEventBus(IGameEventBus bus) => throw new NotImplementedException();

        public void Heal(float amount) => CurrentHealth += amount;

        public void ConsumeResource(Costs type, float amount)
        {
            switch (type)
            {
                case Costs.Barrier:
                    CurrentBarrier -= amount;
                    break;
                case Costs.Health:
                    CurrentHealth -= amount;
                    break;
                case Costs.Mana:
                    CurrentMana -= amount;
                    break;
            }
        }

        public Task Attack(IAttackContext target)
        {
            //var context = new AttackContext(this, target, this.Parameters.Damage, new RndGodot());

            //    AttackPerformed?.Invoke(context);
            return Task.CompletedTask;
        }


        public void OnTurnEnd()
        {
            Effects.TriggerTurnEnd();
            CombatEvents.Publish(new TurnEndEvent(this));
        }

        public void OnTurnStart()
        {
            Effects.TriggerTurnStart();
            CombatEvents.Publish(new TurnStartEvent(this));
        }

        public Task ReceiveAttack(IAttackContext context)
        {
            return Task.CompletedTask;
        }

        public void TakeDamage(float damage, DamageType type, DamageSource source, bool isCrit = false)
        {
            CurrentHealth -= damage;
        }

        public IEntity ChoseTarget(List<IEntity> targets) => throw new NotImplementedException();

        public void AllAttacks() => throw new NotImplementedException();
        public void OnEvadeAttack() => throw new NotImplementedException();
        public void OnBlockAttack() => throw new NotImplementedException();
        public void Kill() => Dead?.Invoke(this);
    }
}
