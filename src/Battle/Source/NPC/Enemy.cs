namespace Battle.Source.NPC
{
    using System;
    using System.Threading.Tasks;
    using Attribute;
    using Components;
    using Abilities.PassiveSkills;
    using CombatEvents;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Data;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.UI;
    using Godot;
    using Utilities;

    internal partial class Enemy : CharacterBody2D, IInitializable, IRequireServices, IEntity
    {
        private const string UID = "uid://bssmtdwwycbpt";
        [Export] private AnimatedSprite2D? _animatedSprite;
        [Export] private Area2D? _interactionArea;
        private IMediator? _mediator;


        public string Id { get; } = string.Empty;
        public string[] Tags { get; } = [];
        public Texture2D? Icon { get; } = null;
        public string Description { get; } = string.Empty;
        public string DisplayName { get; } = string.Empty;
        public IEntityParametersComponent Parameters { get; private set; }
        public IPassiveSkillsComponent PassiveSkills { get; private set; }
        public IEntityAttribute Dexterity { get; private set; }
        public IEntityAttribute Strength { get; private set; }
        public IEntityAttribute Intelligence { get; private set; }
        public ICombatEventDispatcher CombatEvents { get; private set; }
        public IStance CurrentStance { get; private set; }
        public bool IsFighting { get; set; }

        public bool IsAlive => CurrentHealth > 0;
        public IEffectsComponent Effects { get; private set; }
        public IModifiersComponent Modifiers { get; private set; }
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
                if (field <= 0) Dead?.Invoke(this);
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


        public override void _Ready()
        {
            _animatedSprite?.Play("Idle");
            if (_interactionArea != null)
                _interactionArea.BodyEntered += OnBodyEnter;

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
            CombatEvents = new CombatEventDispatcher();

            var passive = new ChainAttackPassiveSkill("Chain");
            passive.Attach(this);
            var passiveTwo = new CounterAttackPassiveSkill("Counter");
            passiveTwo.Attach(this);

            _animatedSprite?.Play("Idle");

            SetBaseValuesForParameters();
        }

        public void AddItemToInventory(IItem item) => throw new NotImplementedException();

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


        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IMediator>();
        }


        public void AllAttacks()
        {
        }

        public void OnBlockAttack()
        {
        }

        public void Kill() => Dead?.Invoke(this);

        public void OnEvadeAttack()
        {
        }

        public async Task ReceiveAttack(IAttackContext context)
        {
            _animatedSprite?.Play("Hurt");
            TakeDamage(context.FinalDamage, DamageType.Normal, DamageSource.Hit, context.IsCritical);
            CombatEvents.Publish(new DamageTakenEvent(this, context));
            context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context.Attacker, context));
            await ToSignal(_animatedSprite, "animation_finished");
            _animatedSprite?.Play("Idle");
        }

        public async Task Attack(IAttackContext context)
        {
            context.IsCritical = context.Rnd.RandFloat() <= Parameters.CriticalChance;
            if (context.IsCritical)
                context.FinalDamage = Parameters.Damage * Parameters.CriticalDamage;
            CombatEvents.Publish(new BeforeAttackEvent(this, context));
            _animatedSprite?.Play("Attack");
            await ToSignal(_animatedSprite, "animation_finished");
            _animatedSprite?.Play("Idle");
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

        public void TakeDamage(float damage, DamageType type, DamageSource source, bool isCrit = false)
        {
            CurrentHealth -= damage;
            GD.Print($"NPC get damage: {damage}");
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnBodyEnter(Node2D body)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mediator);
                switch (body)
                {
                    case Player player:
                    // {
                    //     var fighters = Group?.GetEntitiesInGroup<IFightable>() ?? [this];
                    //     if (!fighters.Contains(player))
                    //         fighters.Add(player);
                    //     _mediator.PublishAsync(new InitializeFightEvent<IFightable>(fighters));
                    //     break;
                    // }
                    case IFightable fighter:
                        // TODO: Decide to begin fight or not
                        break;
                }
                // TODO: Change state to "Fighting"
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to handle body enter", e, this);
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

        private void SetBaseValuesForParameters()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            foreach (EntityParameter entityParameter in Enum.GetValues<EntityParameter>())
            {
                float value = 0;

                switch (entityParameter)
                {
                    case EntityParameter.Health:
                    case EntityParameter.Barrier:
                        value = 1000;
                        break;
                    case EntityParameter.Mana:
                        value = 50;
                        break;
                    case EntityParameter.Intelligence:
                    case EntityParameter.Strength:
                    case EntityParameter.Dexterity:
                        value = rnd.RandfRange(1, 10);
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
    }
}
